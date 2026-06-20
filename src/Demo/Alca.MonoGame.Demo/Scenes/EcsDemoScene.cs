using Alca.MonoGame.Kernel.UI.Core;

namespace Alca.MonoGame.Demo.Scenes;

/// <summary>
/// Demo scene showcasing the ECS hierarchy system.
/// Creates a parent entity and child entities to demonstrate Transform hierarchy.
/// </summary>
public sealed class EcsDemoScene : Scene
{
    private readonly UIRoot _uiRoot = new();
    private readonly UIInteractionManager _interactionManager = new();

    private SpriteFont? _font;
    private readonly GameWorld _world = new();
    private GameEntity _parentEntity = null!;
    private GameEntity _childEntity = null!;
    private float _angle;

    private Texture2D _parentTexture = null!;
    private Texture2D _childTexture = null!;

    public override void LoadContent()
    {
        try { _font = Content.Load<SpriteFont>("DefaultFont"); }
        catch { /* No font available */ }

        _parentTexture = CreateCircleTexture(Core.GraphicsDevice, 24);
        _childTexture = CreateCircleTexture(Core.GraphicsDevice, 14);

        if (_font is not null)
            BuildUI();
    }

    private void BuildUI()
    {
        var root = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };
        var backBtn = new Button(_font!, "← Menú");
        backBtn.Clicked += () => Core.SceneManager.RequestChange(Core.GetService<UIScene_Menu>());
        root.Add(backBtn);
        _uiRoot.Add(root);
    }

    protected override void PostInitialize()
    {
        base.PostInitialize();

        _parentEntity = _world.CreateEntity("Parent", new Vector2(640, 360));
        _parentEntity.Add(new SpriteRendererBehaviour(_parentTexture) { Color = Color.Yellow });

        _childEntity = _world.CreateEntity("Child", new Vector2(100, 0));
        _childEntity.Add(new SpriteRendererBehaviour(_childTexture) { Color = Color.Cyan });
        _childEntity.SetParent(_parentEntity);
    }

    public override void Update(GameTime gameTime)
    {
        _angle += (float)gameTime.ElapsedGameTime.TotalSeconds;
        _parentEntity.Transform.Rotation2d = _angle;

        _world.Update(gameTime);

        _uiRoot.Update(gameTime);
        Rectangle screen = new(0, 0, Core.GraphicsDevice.Viewport.Width, Core.GraphicsDevice.Viewport.Height);
        _uiRoot.Measure(new Vector2(screen.Width, screen.Height));
        _uiRoot.Arrange(screen);
        _interactionManager.Update(_uiRoot, Core.Input.Mouse);
    }

    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(new Color(20, 20, 40));

        Core.SpriteBatch.Begin();

        _world.Draw(gameTime, Core.SpriteBatch);

        if (_font is not null)
        {
            var parentPos = _parentEntity.Transform.Position;
            var childPos = _childEntity.Transform.Position;

            Core.SpriteBatch.DrawString(_font, "ECS Hierarchy Demo", new Vector2(20, 20), Color.White);
            Core.SpriteBatch.DrawString(_font, $"Parent world pos: {parentPos.X:F0}, {parentPos.Y:F0}", new Vector2(20, 50), Color.LightGreen);
            Core.SpriteBatch.DrawString(_font, $"Child world pos:  {childPos.X:F0}, {childPos.Y:F0}", new Vector2(20, 75), Color.LightBlue);
            Core.SpriteBatch.DrawString(_font, $"Child local pos:  {_childEntity.Transform.LocalPosition.X:F0}, {_childEntity.Transform.LocalPosition.Y:F0}", new Vector2(20, 100), Color.LightYellow);
        }

        Core.SpriteBatch.End();

        _uiRoot.DrawAll(Core.SpriteBatch);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _parentTexture?.Dispose();
            _childTexture?.Dispose();
        }
        base.Dispose(disposing);
    }

    private static Texture2D CreateCircleTexture(GraphicsDevice device, int radius)
    {
        int diameter = radius * 2;
        var texture = new Texture2D(device, diameter, diameter);
        var data = new Color[diameter * diameter];
        float radiusSq = (radius - 1f) * (radius - 1f);
        float cx = radius - 0.5f;
        float cy = radius - 0.5f;

        for (int y = 0; y < diameter; y++)
        {
            for (int x = 0; x < diameter; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                data[y * diameter + x] = dx * dx + dy * dy <= radiusSq ? Color.White : Color.Transparent;
            }
        }

        texture.SetData(data);
        return texture;
    }
}
