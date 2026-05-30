using System.Text.Json;
using Microsoft.Xna.Framework;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGame.Editor.Maui.Rendering;

/// <summary>
/// Renderiza una previsualización de los objetos de la escena (via SpriteRendererBehaviour)
/// en el viewport del editor sin activar el modo Play.
/// Texturas cargadas desde disco sin Content Pipeline y cacheadas por proyecto.
/// </summary>
public sealed class EditModeRenderer : IDisposable
{
    private static readonly string SpriteRendererTypeSuffix = "SpriteRendererBehaviour";

    private readonly EditorContext _context;
    private SpriteBatch? _spriteBatch;
    private Texture2D?   _placeholder;

    private readonly Dictionary<string, Texture2D> _textureCache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>True una vez que <see cref="Initialize"/> ha sido llamado desde el hilo de renderizado.</summary>
    public bool IsInitialized => _spriteBatch is not null && _placeholder is not null;

    public EditModeRenderer(EditorContext context)
    {
        _context = context;
        _context.EventBus.Subscribe<ProjectOpenedEvent>(OnProjectOpened);
    }

    /// <summary>Reserva recursos de GPU. Llamar desde el hilo de renderizado antes de <see cref="DrawScene"/>.</summary>
    public void Initialize(GraphicsDevice gd)
    {
        _spriteBatch?.Dispose();
        _placeholder?.Dispose();

        _spriteBatch = new SpriteBatch(gd);

        _placeholder = new Texture2D(gd, 16, 16);
        XnaColor[] magenta = new XnaColor[16 * 16];
        for (int i = 0; i < magenta.Length; i++) magenta[i] = XnaColor.Magenta;
        _placeholder.SetData(magenta);
    }

    /// <summary>
    /// Dibuja los objetos de <paramref name="scene"/> que tienen SpriteRendererBehaviour.
    /// Llamar desde el hilo de renderizado fuera de cualquier pasada activa de SpriteBatch.
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
        catch { }
        finally
        {
            if (began)
            {
                try { _spriteBatch.End(); }
                catch { }
            }
        }
    }

    public void Dispose()
    {
        _context.EventBus.Unsubscribe<ProjectOpenedEvent>(OnProjectOpened);
        ClearCache();
        _placeholder?.Dispose();
        _spriteBatch?.Dispose();
        _placeholder = null;
        _spriteBatch = null;
    }

    // ── Private ──────────────────────────────────────────────────────────────

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

            Vector2 origin = new(tex.Width * 0.5f, tex.Height * 0.5f);
            Vector2 pos    = new(obj.Position.X, obj.Position.Y);
            Vector2 scale  = new(obj.Scale.X, obj.Scale.Y);
            float   rot    = obj.Rotation * (MathF.PI / 180f);

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

        _textureCache[spritePath] = _placeholder!;
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
