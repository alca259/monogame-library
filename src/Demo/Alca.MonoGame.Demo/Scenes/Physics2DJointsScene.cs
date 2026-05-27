using Alca.MonoGame.Kernel.Physics;

namespace Alca.MonoGame.Demo.Scenes;

/// <summary>Scene 28 — Physics2D joints: DistanceJoint2D, HingeJoint2D, SpringJoint2D, PolygonCollider2D, CollisionMatrix.</summary>
public sealed class Physics2DJointsScene : Scene
{
    private readonly UIRoot _uiRoot = new();
    private readonly UIInteractionManager _interactionManager = new();
    private Texture2D _pixel = null!;
    private SpriteFont _font = null!;

    private readonly GameWorld _world = new();
    private const float Gravity = 500f;
    private bool _layerFilterActive;

    private Label _bodiesLabel = null!;
    private Label _contactsLabel = null!;
    private readonly System.Text.StringBuilder _sb = new(64);

    protected override void PostInitialize()
    {
        base.PostInitialize();
        _world.AudioController = Core.Audio;
        BuildPhysicsWorld();
    }

    public override void LoadContent()
    {
        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _font = Content.Load<SpriteFont>("DefaultFont");
        BuildUI();
    }

    private void BuildPhysicsWorld()
    {
        float hw = Core.GraphicsDevice.Viewport.Width / 2f;
        float hh = Core.GraphicsDevice.Viewport.Height;

        _world.PhysicsWorld = new Physics2DWorld(new Vector2(0, Gravity));

        // Static floor and walls
        var floor = _world.CreateEntity("Floor", new Vector2(hw, hh - 20));
        var floorBody = new RigidBody2D { IsStatic = true };
        var floorColl = new BoxCollider2D { Size = new Vector2(hw * 2, 30) };
        floor.Add(floorBody);
        floor.Add(floorColl);

        var wallL = _world.CreateEntity("WallL", new Vector2(10, hh / 2f));
        var wallLBody = new RigidBody2D { IsStatic = true };
        var wallLColl = new BoxCollider2D { Size = new Vector2(20, hh) };
        wallL.Add(wallLBody);
        wallL.Add(wallLColl);

        var wallR = _world.CreateEntity("WallR", new Vector2(hw * 2 - 10, hh / 2f));
        var wallRBody = new RigidBody2D { IsStatic = true };
        var wallRColl = new BoxCollider2D { Size = new Vector2(20, hh) };
        wallR.Add(wallRBody);
        wallR.Add(wallRColl);

        // Distance joint chain (6 bodies)
        GameEntity? prev = null;
        for (int i = 0; i < 6; i++)
        {
            var node = _world.CreateEntity($"Chain{i}", new Vector2(hw - 100 + i * 30, 100 + i * 20));
            var body = new RigidBody2D { IsStatic = i == 0 };
            var coll = new BoxCollider2D { Size = new Vector2(14, 14) };
            node.Add(body);
            node.Add(coll);

            if (prev != null && prev.GetComponent<RigidBody2D>() is RigidBody2D prevBody)
            {
                var joint = new DistanceJoint2D { Distance = 0.4f, Frequency = 6f, DampingRatio = 0.5f };
                joint.ConnectedBody = prevBody;
                node.Add(joint);
            }
            prev = node;
        }

        // Hinge joint (revolving door)
        var anchor = _world.CreateEntity("HingeAnchor", new Vector2(hw + 80, hh - 120));
        var anchorBody = new RigidBody2D { IsStatic = true };
        anchor.Add(anchorBody);
        anchor.Add(new BoxCollider2D { Size = new Vector2(8, 8) });

        var door = _world.CreateEntity("Door", new Vector2(hw + 120, hh - 120));
        var doorBody = new RigidBody2D { IsStatic = false };
        door.Add(doorBody);
        door.Add(new BoxCollider2D { Size = new Vector2(80, 12) });
        var hinge = new HingeJoint2D { UseMotor = true, MotorSpeed = 2f, MaxMotorTorque = 50f };
        hinge.ConnectedBody = anchorBody;
        door.Add(hinge);

        // Spring joint
        var boxA = _world.CreateEntity("SpringA", new Vector2(hw - 80, hh - 200));
        var boxABody = new RigidBody2D { IsStatic = false };
        boxA.Add(boxABody);
        boxA.Add(new BoxCollider2D { Size = new Vector2(28, 28) });

        var boxB = _world.CreateEntity("SpringB", new Vector2(hw - 40, hh - 200));
        var boxBBody = new RigidBody2D { IsStatic = false };
        boxB.Add(boxBBody);
        boxB.Add(new BoxCollider2D { Size = new Vector2(28, 28) });
        var spring = new SpringJoint2D { Distance = 1.5f, Frequency = 4f, DampingRatio = 0.4f };
        spring.ConnectedBody = boxABody;
        boxB.Add(spring);
    }

    private void BuildUI()
    {
        var controls = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        var backBtn = new Button(_font, "← Menú") { BackgroundPixel = _pixel };
        backBtn.Clicked += () => Core.SceneManager.RequestChange(Core.GetService<UIScene_Menu>());
        controls.Add(backBtn);

        controls.Add(new Label { Font = _font, Text = "Physics2D Joints Demo", Color = Color.Yellow });

        var spawnBtn = new Button(_font, "Spawn Polygon") { BackgroundPixel = _pixel };
        spawnBtn.Clicked += SpawnPolygon;
        controls.Add(spawnBtn);

        var resetBtn = new Button(_font, "Reset scene") { BackgroundPixel = _pixel };
        resetBtn.Clicked += () =>
        {
            _world.Destroy();
            BuildPhysicsWorld();
        };
        controls.Add(resetBtn);

        var layerBtn = new Button(_font, "Toggle layers") { BackgroundPixel = _pixel };
        layerBtn.Clicked += () => _layerFilterActive = !_layerFilterActive;
        controls.Add(layerBtn);

        _bodiesLabel = new Label { Font = _font, Text = "Cuerpos: 0", Color = Color.LightGreen };
        _contactsLabel = new Label { Font = _font, Text = "Contactos: 0", Color = Color.LightGreen };
        controls.Add(_bodiesLabel);
        controls.Add(_contactsLabel);

        var anchor = new AnchorLayout();
        anchor.SetAnchor(controls, Anchor.TopRight, new Vector2(10, 10));
        _uiRoot.Add(anchor);
    }

    private void SpawnPolygon()
    {
        float x = 100 + Random.Shared.Next(Core.GraphicsDevice.Viewport.Width - 200);
        float y = 50 + Random.Shared.Next(100);
        var poly = _world.CreateEntity("Polygon", new Vector2(x, y));
        var body = new RigidBody2D { IsStatic = false };
        poly.Add(body);

        var polyCollider = new PolygonCollider2D();
        ReadOnlySpan<Vector2> verts = stackalloc Vector2[]
        {
            new(-20, -10), new(0, -20), new(20, -10),
            new(15, 15), new(-15, 15)
        };
        polyCollider.SetPath(verts);
        poly.Add(polyCollider);
    }

    public override void Update(GameTime gameTime)
    {
        _world.Update(gameTime);

        _sb.Clear();
        _sb.Append("Cuerpos: ");
        _sb.Append(_world.EntityCount);
        _bodiesLabel.Text = _sb.ToString();
        _contactsLabel.Text = "Contactos: —";

        _uiRoot.Update(gameTime);
        Rectangle screen = new(0, 0, Core.GraphicsDevice.Viewport.Width, Core.GraphicsDevice.Viewport.Height);
        _uiRoot.Measure(new Vector2(screen.Width, screen.Height));
        _uiRoot.Arrange(screen);
        _interactionManager.Update(_uiRoot, Core.Input.Mouse);
    }

    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(new Color(15, 15, 25));

        Core.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
        _world.Draw(gameTime, Core.SpriteBatch);
        Core.SpriteBatch.End();

        _uiRoot.DrawAll(Core.SpriteBatch);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _pixel?.Dispose();
        base.Dispose(disposing);
    }
}
