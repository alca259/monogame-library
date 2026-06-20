using System.Drawing;

namespace MonoGame.Editor.Winforms.Theme;

/// <summary>Fuentes del editor. Se crean una sola vez y viven durante toda la app.</summary>
internal static class EditorFonts
{
    public static readonly Font Primary     = new("Segoe UI",  9f, FontStyle.Regular, GraphicsUnit.Point);
    public static readonly Font PrimaryBold = new("Segoe UI",  9f, FontStyle.Bold,    GraphicsUnit.Point);
    public static readonly Font Small       = new("Segoe UI",  8f, FontStyle.Regular, GraphicsUnit.Point);
    public static readonly Font SmallBold   = new("Segoe UI",  8f, FontStyle.Bold,    GraphicsUnit.Point);
    public static readonly Font Mono        = new("Consolas",  9f, FontStyle.Regular, GraphicsUnit.Point);
    public static readonly Font MonoSmall   = new("Consolas",  8f, FontStyle.Regular, GraphicsUnit.Point);
    public static readonly Font Tiny        = new("Segoe UI",  6f, FontStyle.Regular, GraphicsUnit.Point);
}
