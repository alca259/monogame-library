using Alca.MonoGame.Kernel.Graphics;
using Alca.MonoGame.Kernel.Graphics.Camera;

namespace Alca.MonoGame.Kernel.DebugHelpers;

/// <summary>
/// Static debug rendering utility. Accumulates draw commands from any system and renders them all
/// at the end of the frame. Commands with a positive <c>duration</c> persist across frames.
/// </summary>
public static class DebugDraw
{
    private const int BufferCapacity = 512;

    private static readonly DebugCommand[] _commands = new DebugCommand[BufferCapacity];
    private static int _count;

    /// <summary>Gets or sets a value indicating whether debug draw calls are processed.
    /// When <c>false</c>, all Draw* methods are no-ops; <see cref="Update"/> still expires commands.</summary>
    public static bool IsEnabled { get; set; } = true;

    /// <summary>Gets the number of commands currently queued.</summary>
    internal static int CommandCount => _count;

    #region Write API
    /// <summary>Draws a line from <paramref name="from"/> to <paramref name="to"/>.</summary>
    public static void DrawLine(Vector2 from, Vector2 to, Color color, float duration = 0f)
    {
        if (!IsEnabled || _count >= BufferCapacity) return;
        _commands[_count++] = new DebugCommand
        {
            Type = DebugCommandType.Line,
            A = from,
            B = to,
            Color = color,
            Lifetime = duration,
        };
    }

    /// <summary>Draws an axis-aligned rectangle outline.</summary>
    public static void DrawRect(Rectangle rect, Color color, float duration = 0f)
    {
        if (!IsEnabled || _count >= BufferCapacity) return;
        _commands[_count++] = new DebugCommand
        {
            Type = DebugCommandType.Rect,
            A = new Vector2(rect.X, rect.Y),
            B = new Vector2(rect.Width, rect.Height),
            Color = color,
            Lifetime = duration,
        };
    }

    /// <summary>Draws a circle outline approximated with <paramref name="segments"/> line segments.</summary>
    public static void DrawCircle(Vector2 center, float radius, Color color, int segments = 16, float duration = 0f)
    {
        if (!IsEnabled || _count >= BufferCapacity) return;
        _commands[_count++] = new DebugCommand
        {
            Type = DebugCommandType.Circle,
            A = center,
            B = new Vector2(radius),
            Color = color,
            Lifetime = duration,
            Size = segments,
        };
    }

    /// <summary>Draws a small cross marker at <paramref name="pos"/>.</summary>
    public static void DrawPoint(Vector2 pos, Color color, float size = 4f, float duration = 0f)
    {
        if (!IsEnabled || _count >= BufferCapacity) return;
        _commands[_count++] = new DebugCommand
        {
            Type = DebugCommandType.Point,
            A = pos,
            Color = color,
            Lifetime = duration,
            Size = size,
        };
    }

    /// <summary>Draws a text label at <paramref name="pos"/>. Requires a <see cref="SpriteFont"/> passed to <c>Draw</c>.</summary>
    public static void DrawText(Vector2 pos, string text, Color color, float duration = 0f)
    {
        if (!IsEnabled || _count >= BufferCapacity) return;
        _commands[_count++] = new DebugCommand
        {
            Type = DebugCommandType.Text,
            A = pos,
            Color = color,
            Lifetime = duration,
            Text = text,
        };
    }

    /// <summary>Removes all queued commands immediately.</summary>
    public static void Clear() => _count = 0;
    #endregion

    #region Update / Draw
    /// <summary>
    /// Decrements <c>Lifetime</c> for all commands and removes those that have expired.
    /// Call once per frame in <c>Update()</c>.
    /// </summary>
    public static void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        int i = 0;

        while (i < _count)
        {
            if (_commands[i].Lifetime <= 0f)
            {
                _commands[i] = _commands[_count - 1];
                _count--;
            }
            else
            {
                _commands[i].Lifetime -= dt;
                i++;
            }
        }
    }

    /// <summary>
    /// Renders all queued debug commands.
    /// Call at the end of <c>Draw()</c> after the main <see cref="SpriteBatch"/> has ended.
    /// </summary>
    /// <param name="spriteBatch">SpriteBatch used for rendering. Must not be active (Begin/End managed internally).</param>
    /// <param name="camera">Optional camera; when provided, applies the camera's transform matrix.</param>
    /// <param name="font">Optional font for text commands; text commands are silently skipped when null.</param>
    public static void Draw(SpriteBatch spriteBatch, Camera2D? camera = null, SpriteFont? font = null)
    {
        var transform = camera?.GetTransformMatrix(spriteBatch.GraphicsDevice.Viewport);
        Draw(spriteBatch, transform, font);
    }

    /// <summary>
    /// Renders all queued debug commands using an explicit transform matrix.
    /// Allows editor code to pass a custom camera matrix without depending on <see cref="Camera2D"/>.
    /// Call at the end of <c>Draw()</c> after the main <see cref="SpriteBatch"/> has ended.
    /// </summary>
    /// <param name="spriteBatch">SpriteBatch used for rendering. Must not be active (Begin/End managed internally).</param>
    /// <param name="transformMatrix">Optional transform matrix; when null, uses identity (screen space).</param>
    /// <param name="font">Optional font for text commands; text commands are silently skipped when null.</param>
    public static void Draw(SpriteBatch spriteBatch, Matrix? transformMatrix, SpriteFont? font = null)
    {
        if (_count == 0) return;

        spriteBatch.Begin(transformMatrix: transformMatrix);

        var pixel = DrawHelper.DefaultPixelTexture;

        for (int i = 0; i < _count; i++)
        {
            ref readonly var cmd = ref _commands[i];
            switch (cmd.Type)
            {
                case DebugCommandType.Line:
                    DrawHelper.DrawLine(pixel, spriteBatch, cmd.A, cmd.B, cmd.Color);
                    break;

                case DebugCommandType.Rect:
                    var rect = new Rectangle((int)cmd.A.X, (int)cmd.A.Y, (int)cmd.B.X, (int)cmd.B.Y);
                    DrawHelper.DrawBorder(pixel, spriteBatch, rect, cmd.Color);
                    break;

                case DebugCommandType.Circle:
                    DrawCircleInternal(spriteBatch, pixel, cmd.A, cmd.B.X, (int)cmd.Size, cmd.Color);
                    break;

                case DebugCommandType.Point:
                    float half = cmd.Size * 0.5f;
                    DrawHelper.DrawLine(pixel, spriteBatch, cmd.A - new Vector2(half, 0), cmd.A + new Vector2(half, 0), cmd.Color);
                    DrawHelper.DrawLine(pixel, spriteBatch, cmd.A - new Vector2(0, half), cmd.A + new Vector2(0, half), cmd.Color);
                    break;

                case DebugCommandType.Text:
                    if (font is not null && cmd.Text is not null)
                        spriteBatch.DrawString(font, cmd.Text, cmd.A, cmd.Color);
                    break;
            }
        }

        spriteBatch.End();
    }
    #endregion

    #region Internal helpers
    private static void DrawCircleInternal(SpriteBatch sb, Texture2D pixel, Vector2 center, float radius, int segments, Color color)
    {
        float step = MathF.PI * 2f / segments;
        for (int i = 0; i < segments; i++)
        {
            float angle0 = step * i;
            float angle1 = step * (i + 1);
            var from = center + new Vector2(MathF.Cos(angle0) * radius, MathF.Sin(angle0) * radius);
            var to = center + new Vector2(MathF.Cos(angle1) * radius, MathF.Sin(angle1) * radius);
            DrawHelper.DrawLine(pixel, sb, from, to, color);
        }
    }
    #endregion
}
