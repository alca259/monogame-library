using Alca.MonoGame.Kernel.ECS;

namespace Alca.MonoGame.Kernel.UnitTests.ECS;

public sealed class GameEntityTests
{
    private static GameTime AnyGameTime() =>
        new(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(0.016));

    // ── Identity ───────────────────────────────────────────────────────────────

    [Fact]
    public void Name_IsPreserved_FromCreateEntity()
    {
        var world = new GameWorld();
        var entity = world.CreateEntity("Hero");

        Assert.Equal("Hero", entity.Name);
    }

    [Fact]
    public void Id_IsUnique_AcrossEntities()
    {
        var world = new GameWorld();
        var a = world.CreateEntity("A");
        var b = world.CreateEntity("B");

        Assert.NotEqual(a.Id, b.Id);
    }

    // ── Transform auto-attach ──────────────────────────────────────────────────

    [Fact]
    public void Transform_IsNotNull_AfterCreateEntity()
    {
        var world = new GameWorld();
        var entity = world.CreateEntity("E", new Vector2(10, 20));

        Assert.NotNull(entity.Transform);
    }

    [Fact]
    public void Transform_Position_MatchesCreateEntity2dPosition()
    {
        var world = new GameWorld();
        var entity = world.CreateEntity("E", new Vector2(42, 99));

        Assert.Equal(new Vector3(42, 99, 0), entity.Transform.Position);
    }

    [Fact]
    public void Transform_Position_MatchesCreateEntity3dPosition()
    {
        var world = new GameWorld();
        var entity = world.CreateEntity("E", new Vector3(1, 2, 3));

        Assert.Equal(new Vector3(1, 2, 3), entity.Transform.Position);
    }

    [Fact]
    public void Transform_Position2d_ReturnsXY()
    {
        var world = new GameWorld();
        var entity = world.CreateEntity("E", new Vector3(7, 8, 9));

        Assert.Equal(new Vector2(7, 8), entity.Transform.Position2d);
    }

    [Fact]
    public void Transform_Position2d_Setter_PreservesZ()
    {
        var world = new GameWorld();
        var entity = world.CreateEntity("E", new Vector3(0, 0, 5));

        entity.Transform.Position2d = new Vector2(3, 4);

        Assert.Equal(new Vector3(3, 4, 5), entity.Transform.Position);
    }

    [Fact]
    public void Transform_Rotation2d_MapsToRotationZ()
    {
        var world = new GameWorld();
        var entity = world.CreateEntity("E");

        entity.Transform.Rotation2d = 1.57f;

        Assert.Equal(1.57f, entity.Transform.Rotation.Z);
        Assert.Equal(1.57f, entity.Transform.Rotation2d);
    }

    [Fact]
    public void Transform_IsAccessible_WithoutGetComponent()
    {
        var world = new GameWorld();
        var entity = world.CreateEntity("E");

        entity.Transform.Position = new Vector3(5, 5, 0);

        Assert.Equal(new Vector3(5, 5, 0), entity.Transform.Position);
    }

    // ── GetComponent / HasComponent / TryGetComponent ─────────────────────────

    [Fact]
    public void GetComponent_ReturnsBehaviour_ByConcreteType()
    {
        var world = new GameWorld();
        var entity = world.CreateEntity("E");
        var b = new CounterBehaviour();
        entity.Add(b);

        var result = entity.GetComponent<CounterBehaviour>();

        Assert.Same(b, result);
    }

    [Fact]
    public void GetComponent_ReturnsBehaviour_ByInterface()
    {
        var world = new GameWorld();
        var entity = world.CreateEntity("E");
        var b = new CounterBehaviour();
        entity.Add(b);

        var result = entity.GetComponent<ICounter>();

        Assert.Same(b, result);
    }

    [Fact]
    public void GetComponent_ReturnsNull_WhenNotPresent()
    {
        var world = new GameWorld();
        var entity = world.CreateEntity("E");

        var result = entity.GetComponent<CounterBehaviour>();

        Assert.Null(result);
    }

    [Fact]
    public void HasComponent_ReturnsTrue_WhenPresent()
    {
        var world = new GameWorld();
        var entity = world.CreateEntity("E");
        entity.Add(new CounterBehaviour());

        Assert.True(entity.HasComponent<CounterBehaviour>());
        Assert.True(entity.HasComponent<ICounter>());
    }

    [Fact]
    public void HasComponent_ReturnsFalse_WhenAbsent()
    {
        var world = new GameWorld();
        var entity = world.CreateEntity("E");

        Assert.False(entity.HasComponent<CounterBehaviour>());
    }

    [Fact]
    public void TryGetComponent_ReturnsTrue_AndSetsOut_WhenPresent()
    {
        var world = new GameWorld();
        var entity = world.CreateEntity("E");
        var b = new CounterBehaviour();
        entity.Add(b);

        bool found = entity.TryGetComponent<CounterBehaviour>(out var result);

        Assert.True(found);
        Assert.Same(b, result);
    }

    [Fact]
    public void TryGetComponent_ReturnsFalse_WhenAbsent()
    {
        var world = new GameWorld();
        var entity = world.CreateEntity("E");

        bool found = entity.TryGetComponent<CounterBehaviour>(out var result);

        Assert.False(found);
        Assert.Null(result);
    }

    [Fact]
    public void GetAllComponents_IncludesAllAddedBehaviours()
    {
        var world = new GameWorld();
        var entity = world.CreateEntity("E");
        var b1 = new CounterBehaviour();
        var b2 = new AnotherBehaviour();
        entity.Add(b1);
        entity.Add(b2);

        var all = entity.GetAllComponents().ToList();

        // TransformBehaviour is pre-attached, plus b1 and b2
        Assert.Contains(b1, all);
        Assert.Contains(b2, all);
    }

    // ── Active flag ────────────────────────────────────────────────────────────

    [Fact]
    public void Update_IsSkipped_WhenEntityIsInactive()
    {
        var world = new GameWorld();
        var spy = new CounterBehaviour();
        var entity = world.CreateEntity("E");
        entity.Add(spy);
        entity.Active = false;

        world.Update(AnyGameTime());
        world.Update(AnyGameTime());

        Assert.Equal(0, spy.UpdateCount);
    }

    [Fact]
    public void Update_Resumes_WhenEntityIsReActivated()
    {
        var world = new GameWorld();
        var spy = new CounterBehaviour();
        var entity = world.CreateEntity("E");
        entity.Add(spy);
        entity.Active = false;

        world.Update(AnyGameTime());
        entity.Active = true;
        world.Update(AnyGameTime());

        Assert.Equal(1, spy.UpdateCount);
    }

    // ── Reflection hot-path lists ──────────────────────────────────────────────

    [Fact]
    public void TransformBehaviour_IsNotInUpdateList_ZeroCostPerFrame()
    {
        // TransformBehaviour does not override Update or Draw, so it
        // must NOT appear in the entity's updatable/drawable hot-path lists.
        // We verify indirectly: the TransformBehaviour's Update method's
        // DeclaringType should be GameBehaviour (not overridden).
        var method = typeof(TransformBehaviour).GetMethod(
            "Update",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public,
            null, [typeof(GameTime)], null);

        Assert.Equal(typeof(GameBehaviour), method?.DeclaringType);
    }

    private interface ICounter
    {
        int UpdateCount { get; }
    }

    private sealed class CounterBehaviour : GameBehaviour, ICounter
    {
        public int UpdateCount { get; private set; }
        public override void Update(GameTime gameTime) => UpdateCount++;
    }

    private sealed class AnotherBehaviour : GameBehaviour { }

    // ── Hierarchy ──────────────────────────────────────────────────────────────

    [Fact]
    public void SetParent_SetsParentAndAddsToChildrenList()
    {
        var world = new GameWorld();
        var parent = world.CreateEntity("Parent");
        var child = world.CreateEntity("Child");

        child.SetParent(parent);

        Assert.Same(parent, child.Parent);
        Assert.Contains(child, parent.Children);
    }

    [Fact]
    public void SetParent_ClearsParent_WhenNullPassed()
    {
        var world = new GameWorld();
        var parent = world.CreateEntity("Parent");
        var child = world.CreateEntity("Child");
        child.SetParent(parent);

        child.SetParent(null);

        Assert.Null(child.Parent);
        Assert.DoesNotContain(child, parent.Children);
    }

    [Fact]
    public void IsChildOf_ReturnsTrue_ForDirectParent()
    {
        var world = new GameWorld();
        var parent = world.CreateEntity("Parent");
        var child = world.CreateEntity("Child");
        child.SetParent(parent);

        Assert.True(child.IsChildOf(parent));
    }

    [Fact]
    public void IsChildOf_ReturnsTrue_ForIndirectParent()
    {
        var world = new GameWorld();
        var root = world.CreateEntity("Root");
        var mid = world.CreateEntity("Mid");
        var leaf = world.CreateEntity("Leaf");
        mid.SetParent(root);
        leaf.SetParent(mid);

        Assert.True(leaf.IsChildOf(root));
    }

    [Fact]
    public void IsChildOf_ReturnsFalse_WhenNotInHierarchy()
    {
        var world = new GameWorld();
        var a = world.CreateEntity("A");
        var b = world.CreateEntity("B");

        Assert.False(a.IsChildOf(b));
        Assert.False(b.IsChildOf(a));
    }

    [Fact]
    public void ChildCount_IncrementsWhenChildAdded()
    {
        var world = new GameWorld();
        var parent = world.CreateEntity("Parent");

        world.CreateEntity("C1").SetParent(parent);
        world.CreateEntity("C2").SetParent(parent);

        Assert.Equal(2, parent.ChildCount);
    }

    [Fact]
    public void GetSiblingIndex_ReturnsCorrectIndex()
    {
        var world = new GameWorld();
        var parent = world.CreateEntity("Parent");
        var c0 = world.CreateEntity("C0");
        var c1 = world.CreateEntity("C1");
        c0.SetParent(parent);
        c1.SetParent(parent);

        Assert.Equal(0, c0.GetSiblingIndex());
        Assert.Equal(1, c1.GetSiblingIndex());
    }

    [Fact]
    public void SetAsFirstSibling_MovesToFront()
    {
        var world = new GameWorld();
        var parent = world.CreateEntity("Parent");
        var c0 = world.CreateEntity("C0");
        var c1 = world.CreateEntity("C1");
        c0.SetParent(parent);
        c1.SetParent(parent);

        c1.SetAsFirstSibling();

        Assert.Equal(0, c1.GetSiblingIndex());
    }

    [Fact]
    public void Root_ReturnsSelf_WhenNoParent()
    {
        var world = new GameWorld();
        var entity = world.CreateEntity("E");

        Assert.Same(entity, entity.Root);
    }

    [Fact]
    public void Root_ReturnsTopLevelEntity_WhenNested()
    {
        var world = new GameWorld();
        var root = world.CreateEntity("Root");
        var mid = world.CreateEntity("Mid");
        var leaf = world.CreateEntity("Leaf");
        mid.SetParent(root);
        leaf.SetParent(mid);

        Assert.Same(root, leaf.Root);
    }

    [Fact]
    public void Find_FindsChildByName_InNestedHierarchy()
    {
        var world = new GameWorld();
        var root = world.CreateEntity("Root");
        var mid = world.CreateEntity("Mid");
        var leaf = world.CreateEntity("Target");
        mid.SetParent(root);
        leaf.SetParent(mid);

        var found = root.Find("Target");

        Assert.Same(leaf, found);
    }

    [Fact]
    public void Find_ReturnsNull_WhenNameNotFound()
    {
        var world = new GameWorld();
        var root = world.CreateEntity("Root");

        Assert.Null(root.Find("Nobody"));
    }

    [Fact]
    public void DetachChildren_RemovesAllChildren()
    {
        var world = new GameWorld();
        var parent = world.CreateEntity("Parent");
        world.CreateEntity("C1").SetParent(parent);
        world.CreateEntity("C2").SetParent(parent);

        parent.DetachChildren();

        Assert.Equal(0, parent.ChildCount);
    }

    [Fact]
    public void TraverseDown_VisitsThisAndAllDescendants()
    {
        var world = new GameWorld();
        var root = world.CreateEntity("Root");
        var mid = world.CreateEntity("Mid");
        var leaf = world.CreateEntity("Leaf");
        mid.SetParent(root);
        leaf.SetParent(mid);
        var visited = new List<string>();

        root.TraverseDown(e => visited.Add(e.Name));

        Assert.Equal(new[] { "Root", "Mid", "Leaf" }, visited);
    }

    [Fact]
    public void TraverseUp_VisitsThisAndAllAncestors()
    {
        var world = new GameWorld();
        var root = world.CreateEntity("Root");
        var mid = world.CreateEntity("Mid");
        var leaf = world.CreateEntity("Leaf");
        mid.SetParent(root);
        leaf.SetParent(mid);
        var visited = new List<string>();

        leaf.TraverseUp(e => visited.Add(e.Name));

        Assert.Equal(new[] { "Leaf", "Mid", "Root" }, visited);
    }

    // ── Tags ───────────────────────────────────────────────────────────────────

    [Fact]
    public void AddTag_And_HasTag_ReturnTrue()
    {
        var world = new GameWorld();
        var entity = world.CreateEntity("E");
        entity.AddTag("enemy");

        Assert.True(entity.HasTag("enemy"));
        Assert.False(entity.HasTag("player"));
    }

    [Fact]
    public void RemoveTag_RemovesExistingTag()
    {
        var world = new GameWorld();
        var entity = world.CreateEntity("E");
        entity.AddTag("enemy");
        entity.RemoveTag("enemy");

        Assert.False(entity.HasTag("enemy"));
    }

    [Fact]
    public void CompareTag_DelegatesToHasTag()
    {
        var world = new GameWorld();
        var entity = world.CreateEntity("E");
        entity.AddTag("player");

        Assert.True(entity.CompareTag("player"));
        Assert.False(entity.CompareTag("enemy"));
    }

    // ── New component API ──────────────────────────────────────────────────────

    [Fact]
    public void AddComponent_ReturnsNewBehaviour_WithEntitySet()
    {
        var world = new GameWorld();
        var entity = world.CreateEntity("E");

        var b = entity.AddComponent<CounterBehaviour>();

        Assert.NotNull(b);
        Assert.Same(entity, b.Entity);
    }

    [Fact]
    public void SetActive_SetsActiveFlag()
    {
        var world = new GameWorld();
        var entity = world.CreateEntity("E");
        entity.SetActive(false);

        Assert.False(entity.Active);
    }

    [Fact]
    public void GetComponentCount_IncludesTransformAndAllAdded()
    {
        var world = new GameWorld();
        var entity = world.CreateEntity("E");
        entity.Add(new CounterBehaviour());
        entity.Add(new AnotherBehaviour());

        // TransformBehaviour + CounterBehaviour + AnotherBehaviour = 3
        Assert.Equal(3, entity.GetComponentCount());
    }

    [Fact]
    public void GetComponentAtIndex_ReturnsCorrectBehaviour()
    {
        var world = new GameWorld();
        var entity = world.CreateEntity("E");
        var b = new CounterBehaviour();
        entity.Add(b);

        // Index 0 = TransformBehaviour, index 1 = CounterBehaviour
        Assert.Same(b, entity.GetComponentAtIndex(1));
    }

    [Fact]
    public void GetComponentIndex_ReturnsCorrectIndex()
    {
        var world = new GameWorld();
        var entity = world.CreateEntity("E");
        var b = new CounterBehaviour();
        entity.Add(b);

        Assert.Equal(1, entity.GetComponentIndex(b));
    }

    [Fact]
    public void GetComponents_FillsResultsWithAllMatchingBehaviours()
    {
        var world = new GameWorld();
        var entity = world.CreateEntity("E");
        var b = new CounterBehaviour();
        entity.Add(b);
        var results = new List<ICounter>();

        entity.GetComponents<ICounter>(results);

        Assert.Single(results);
        Assert.Same(b, results[0]);
    }

    [Fact]
    public void GetComponentInChildren_FindsComponentInDirectChild()
    {
        var world = new GameWorld();
        var parent = world.CreateEntity("Parent");
        var child = world.CreateEntity("Child");
        child.SetParent(parent);
        var b = new CounterBehaviour();
        child.Add(b);

        var result = parent.GetComponentInChildren<CounterBehaviour>();

        Assert.Same(b, result);
    }

    [Fact]
    public void GetComponentInChildren_SkipsInactiveChild_WhenIncludeInactiveFalse()
    {
        var world = new GameWorld();
        var parent = world.CreateEntity("Parent");
        var child = world.CreateEntity("Child");
        child.SetParent(parent);
        child.Active = false;
        child.Add(new CounterBehaviour());

        var result = parent.GetComponentInChildren<CounterBehaviour>(includeInactive: false);

        Assert.Null(result);
    }

    [Fact]
    public void GetComponentInParent_FindsComponentInDirectParent()
    {
        var world = new GameWorld();
        var parent = world.CreateEntity("Parent");
        var child = world.CreateEntity("Child");
        child.SetParent(parent);
        var b = new CounterBehaviour();
        parent.Add(b);

        var result = child.GetComponentInParent<CounterBehaviour>();

        Assert.Same(b, result);
    }

    [Fact]
    public void GetComponentsInChildren_FillsResultsFromAllDescendants()
    {
        var world = new GameWorld();
        var root = world.CreateEntity("Root");
        var child1 = world.CreateEntity("C1");
        var child2 = world.CreateEntity("C2");
        child1.SetParent(root);
        child2.SetParent(root);
        child1.Add(new CounterBehaviour());
        child2.Add(new CounterBehaviour());
        var results = new List<CounterBehaviour>();

        root.GetComponentsInChildren<CounterBehaviour>(results);

        Assert.Equal(2, results.Count);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────
}
