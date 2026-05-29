using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGame.Editor.WinForms.Rendering;

/// <summary>
/// Renders a lightweight preview of scene objects (via SpriteRendererBehaviour sprite paths)
/// directly in the editor viewport without requiring Play mode to be active.
/// Textures are loaded from raw image files and cached until the project changes.
/// </summary>
public sealed class EditModeRenderer : IDisposable
{
    private static readonly string SpriteRendererTypeSuffix = "SpriteRendererBehaviour";

    private readonly EditorContext _context;
    private SpriteBatch? _spriteBatch;
    private Texture2D? _placeholder;

    private readonly Dictionary<string, Texture2D> _textureCache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Returns <c>true</c> once <see cref="Initialize"/> has been called from the render thread.</summary>
    public bool IsInitialized => _spriteBatch is not null && _placeholder is not null;

    /// <summary>Creates the renderer bound to the given editor context.</summary>
    public EditModeRenderer(EditorContext context)
    {
        _context = context;
        _context.EventBus.Subscribe<ProjectOpenedEvent>(OnProjectOpened);
    }

    /// <summary>
    /// Allocates GPU resources. Must be called from the render thread (MonoGameControl render loop)
    /// before <see cref="DrawScene"/>.
    /// </summary>
    public void Initialize(GraphicsDevice gd)
    {
        _spriteBatch?.Dispose();
        _placeholder?.Dispose();

        _spriteBatch = new SpriteBatch(gd);

        // 16×16 magenta placeholder so objects are visible even without a sprite assigned.
        _placeholder = new Texture2D(gd, 16, 16);
        XnaColor[] magenta = new XnaColor[16 * 16];
        for (int i = 0; i < magenta.Length; i++) magenta[i] = XnaColor.Magenta;
        _placeholder.SetData(magenta);
    }

    /// <summary>
    /// Draws all objects in <paramref name="scene"/> that carry a SpriteRendererBehaviour
    /// using the camera transform for world→screen mapping.
    /// Must be called from the render thread outside any active SpriteBatch pass.
    /// </summary>
    public void DrawScene(EditorScene scene, Matrix cameraTransform)
    {
        if (_spriteBatch is null || _placeholder is null) return;

        GraphicsDevice? gd = _spriteBatch.GraphicsDevice;
        if (gd is null) return;

        bool began = false;

        try
        {
            _spriteBatch.Begin(
                transformMatrix: cameraTransform,
                samplerState: SamplerState.PointClamp,
                blendState: BlendState.AlphaBlend);
            began = true;
            DrawObjectList(scene.RootGameObjects, gd);
        }
        catch { /* ignore per-object draw errors */ }
        finally
        {
            if (began)
            {
                try { _spriteBatch.End(); }
                catch { /* ignore end-pass errors */ }
            }
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _context.EventBus.Unsubscribe<ProjectOpenedEvent>(OnProjectOpened);
        ClearCache();
        _placeholder?.Dispose();
        _spriteBatch?.Dispose();
        _placeholder = null;
        _spriteBatch = null;
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private void DrawObjectList(List<EditorGameObject> objects, GraphicsDevice gd)
    {
        for (int i = 0; i < objects.Count; i++)
        {
            DrawObject(objects[i], gd);
            if (objects[i].Children.Count > 0)
                DrawObjectList(objects[i].Children, gd);
        }
    }

    private void DrawObject(EditorGameObject obj, GraphicsDevice gd)
    {
        if (!obj.Active) return;

        for (int i = 0; i < obj.Behaviours.Count; i++)
        {
            EditorBehaviour b = obj.Behaviours[i];
            if (!b.Enabled) continue;
            if (!b.TypeName.EndsWith(SpriteRendererTypeSuffix, StringComparison.Ordinal)) continue;

            Texture2D tex = ResolveTexture(b, gd);
            if (tex is null) continue;

            Vector2 origin = new Vector2(tex.Width * 0.5f, tex.Height * 0.5f);
            Vector2 pos = new Vector2(obj.Position.X, obj.Position.Y);
            Vector2 scale = new Vector2(obj.Scale.X, obj.Scale.Y);
            float rot = obj.Rotation * (MathF.PI / 180f);

            _spriteBatch!.Draw(tex, pos, null, XnaColor.White, rot, origin, scale, SpriteEffects.None, 0f);
        }
    }

    private Texture2D ResolveTexture(EditorBehaviour behaviour, GraphicsDevice gd)
    {
        string spritePath = string.Empty;
        if (behaviour.Properties.TryGetValue("SpritePath", out JsonElement el))
            spritePath = el.GetString() ?? string.Empty;

        if (string.IsNullOrEmpty(spritePath))
            return _placeholder!;

        if (_textureCache.TryGetValue(spritePath, out Texture2D? cached))
            return cached;

        string contentRoot = _context.ActiveProject?.ContentPath ?? string.Empty;
        if (string.IsNullOrEmpty(contentRoot))
            return _placeholder!;

        // Try the path as-is, then with common image extensions.
        string[] candidates =
        [
            Path.IsPathRooted(spritePath) ? spritePath : Path.Combine(contentRoot, spritePath),
            Path.Combine(contentRoot, spritePath + ".png"),
            Path.Combine(contentRoot, spritePath + ".jpg"),
            Path.Combine(contentRoot, spritePath + ".bmp"),
        ];

        for (int i = 0; i < candidates.Length; i++)
        {
            if (!File.Exists(candidates[i])) continue;
            try
            {
                using FileStream fs = File.OpenRead(candidates[i]);
                Texture2D tex = Texture2D.FromStream(gd, fs);
                _textureCache[spritePath] = tex;
                return tex;
            }
            catch { }
        }

        _textureCache[spritePath] = _placeholder!;   // cache miss → avoid repeated file IO
        return _placeholder!;
    }

    private void OnProjectOpened(ProjectOpenedEvent _) => ClearCache();

    private void ClearCache()
    {
        foreach (KeyValuePair<string, Texture2D> kv in _textureCache)
        {
            if (!ReferenceEquals(kv.Value, _placeholder))
                kv.Value.Dispose();
        }
        _textureCache.Clear();
    }
}
