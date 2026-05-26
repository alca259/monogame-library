using Alca.MonoGame.Kernel.ECS;

namespace Alca.MonoGame.Kernel.UnitTests.ECS;

public sealed class TransformBehaviourTests
{
    // ── Without parent: local == world ────────────────────────────────────────

    [Fact]
    public void Position_EqualsLocalPosition_WhenNoParent()
    {
        var world = new GameWorld();
        var entity = world.CreateEntity("E", new Vector3(5, 10, 0));

        Assert.Equal(entity.Transform.LocalPosition, entity.Transform.Position);
    }

    [Fact]
    public void Rotation_EqualsLocalRotation_WhenNoParent()
    {
        var world = new GameWorld();
        var entity = world.CreateEntity("E");
        entity.Transform.LocalRotation2d = 1.0f;

        Assert.Equal(entity.Transform.LocalRotation, entity.Transform.Rotation);
    }

    [Fact]
    public void Scale_EqualsLocalScale_AndLossyScale_WhenNoParent()
    {
        var world = new GameWorld();
        var entity = world.CreateEntity("E");
        entity.Transform.LocalScale = new Vector3(2, 3, 1);

        Assert.Equal(entity.Transform.LocalScale, entity.Transform.Scale);
        Assert.Equal(entity.Transform.LocalScale, entity.Transform.LossyScale);
    }

    // ── Backward compatibility ─────────────────────────────────────────────────

    [Fact]
    public void Scale_Setter_UpdatesLocalScale()
    {
        var world = new GameWorld();
        var entity = world.CreateEntity("E");
        entity.Transform.Scale = new Vector3(2, 2, 2);

        Assert.Equal(new Vector3(2, 2, 2), entity.Transform.LocalScale);
    }

    // ── With parent: world != local ───────────────────────────────────────────

    [Fact]
    public void Position_ComputesWorldPosition_WhenChildOfParent()
    {
        var world = new GameWorld();
        var parent = world.CreateEntity("Parent", new Vector2(100, 0));
        var child = world.CreateEntity("Child", new Vector2(0, 0));
        child.SetParent(parent);
        child.Transform.LocalPosition = new Vector3(50, 0, 0);

        var worldPos = child.Transform.Position;

        Assert.Equal(150f, worldPos.X, 0.01f);
        Assert.Equal(0f, worldPos.Y, 0.01f);
    }

    [Fact]
    public void Position_Setter_UpdatesLocalPosition_WhenChildOfParent()
    {
        var world = new GameWorld();
        var parent = world.CreateEntity("Parent", new Vector2(100, 0));
        var child = world.CreateEntity("Child");
        child.SetParent(parent);

        child.Transform.Position = new Vector3(150, 0, 0);

        Assert.Equal(50f, child.Transform.LocalPosition.X, 0.01f);
    }

    [Fact]
    public void Rotation_SumsParentAndLocalRotation()
    {
        var world = new GameWorld();
        var parent = world.CreateEntity("Parent");
        var child = world.CreateEntity("Child");
        child.SetParent(parent);

        parent.Transform.LocalRotation2d = 1.0f;
        child.Transform.LocalRotation2d = 0.5f;

        Assert.Equal(1.5f, child.Transform.Rotation2d, 0.001f);
    }

    [Fact]
    public void LossyScale_IsProductOfScalesInHierarchy()
    {
        var world = new GameWorld();
        var parent = world.CreateEntity("Parent");
        var child = world.CreateEntity("Child");
        child.SetParent(parent);

        parent.Transform.LocalScale = new Vector3(2, 2, 2);
        child.Transform.LocalScale = new Vector3(3, 3, 3);

        Assert.Equal(new Vector3(6, 6, 6), child.Transform.LossyScale);
    }

    // ── Matrices ──────────────────────────────────────────────────────────────

    [Fact]
    public void LocalToWorldMatrix_IsIdentityForDefaultTransform_WhenNoParent()
    {
        var world = new GameWorld();
        var entity = world.CreateEntity("E");

        // Default: position=(0,0,0), rotation=(0,0,0), scale=(1,1,1)
        var m = entity.Transform.LocalToWorldMatrix;

        Assert.Equal(Matrix.Identity, m);
    }

    [Fact]
    public void WorldToLocalMatrix_IsInverseOfLocalToWorld()
    {
        var world = new GameWorld();
        var entity = world.CreateEntity("E", new Vector3(5, 3, 0));

        var m = entity.Transform.LocalToWorldMatrix;
        var inv = entity.Transform.WorldToLocalMatrix;
        var product = m * inv;

        Assert.Equal(Matrix.Identity.M11, product.M11, 3);
        Assert.Equal(Matrix.Identity.M22, product.M22, 3);
        Assert.Equal(Matrix.Identity.M44, product.M44, 3);
    }

    // ── Coordinate transforms ─────────────────────────────────────────────────

    [Fact]
    public void TransformPoint_AppliesTranslation_WhenNoParent()
    {
        var world = new GameWorld();
        var entity = world.CreateEntity("E", new Vector3(10, 20, 0));

        var result = entity.Transform.TransformPoint(new Vector3(5, 0, 0));

        Assert.Equal(15f, result.X, 0.01f);
        Assert.Equal(20f, result.Y, 0.01f);
    }

    [Fact]
    public void InverseTransformPoint_RevertsTransformPoint()
    {
        var world = new GameWorld();
        var entity = world.CreateEntity("E", new Vector3(10, 20, 0));
        var original = new Vector3(5, 8, 0);

        var world3 = entity.Transform.TransformPoint(original);
        var back = entity.Transform.InverseTransformPoint(world3);

        Assert.Equal(original.X, back.X, 3);
        Assert.Equal(original.Y, back.Y, 3);
    }

    // ── Transform operations ──────────────────────────────────────────────────

    [Fact]
    public void Translate_LocalSpace_AddsToLocalPosition()
    {
        var world = new GameWorld();
        var entity = world.CreateEntity("E", new Vector2(10, 10));

        entity.Transform.Translate(new Vector3(5, 0, 0), worldSpace: false);

        Assert.Equal(new Vector3(15, 10, 0), entity.Transform.LocalPosition);
    }

    [Fact]
    public void Translate_WorldSpace_UpdatesWorldPosition()
    {
        var world = new GameWorld();
        var entity = world.CreateEntity("E", new Vector2(10, 10));

        entity.Transform.Translate(new Vector3(5, 0, 0), worldSpace: true);

        Assert.Equal(15f, entity.Transform.Position.X, 0.01f);
    }

    [Fact]
    public void SetLocalPositionAndRotation_SetsBackingFields()
    {
        var world = new GameWorld();
        var entity = world.CreateEntity("E");

        entity.Transform.SetLocalPositionAndRotation(new Vector3(1, 2, 3), new Vector3(0.1f, 0.2f, 0.3f));

        Assert.Equal(new Vector3(1, 2, 3), entity.Transform.LocalPosition);
        Assert.Equal(new Vector3(0.1f, 0.2f, 0.3f), entity.Transform.LocalRotation);
    }

    // ── Hierarchy navigation ──────────────────────────────────────────────────

    [Fact]
    public void ParentTransform_IsNull_ForRootEntity()
    {
        var world = new GameWorld();
        var entity = world.CreateEntity("E");

        Assert.Null(entity.Transform.ParentTransform);
    }

    [Fact]
    public void ParentTransform_ReturnsParentTransform_WhenChildSet()
    {
        var world = new GameWorld();
        var parent = world.CreateEntity("Parent");
        var child = world.CreateEntity("Child");
        child.SetParent(parent);

        Assert.Same(parent.Transform, child.Transform.ParentTransform);
    }

    [Fact]
    public void GetChild_ReturnsChildTransform_ByIndex()
    {
        var world = new GameWorld();
        var parent = world.CreateEntity("Parent");
        var child = world.CreateEntity("Child");
        child.SetParent(parent);

        Assert.Same(child.Transform, parent.Transform.GetChild(0));
    }

    [Fact]
    public void IsChildOf_DelegatesToEntity()
    {
        var world = new GameWorld();
        var parent = world.CreateEntity("Parent");
        var child = world.CreateEntity("Child");
        child.SetParent(parent);

        Assert.True(child.Transform.IsChildOf(parent.Transform));
        Assert.False(parent.Transform.IsChildOf(child.Transform));
    }

    [Fact]
    public void Root_ReturnsTopLevelTransform_ForNestedHierarchy()
    {
        var world = new GameWorld();
        var root = world.CreateEntity("Root");
        var mid = world.CreateEntity("Mid");
        var leaf = world.CreateEntity("Leaf");
        mid.SetParent(root);
        leaf.SetParent(mid);

        Assert.Same(root.Transform, leaf.Transform.Root);
    }
}
