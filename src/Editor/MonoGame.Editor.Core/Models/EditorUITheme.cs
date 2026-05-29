namespace MonoGame.Editor.Core.Models;

/// <summary>
/// NineSlice configuration for a single control type.
/// Serialized inside <see cref="EditorUITheme"/>.
/// </summary>
public sealed class EditorUIThemeEntry
{
    /// <summary>Content-relative texture path (no extension). Null or empty = no NineSlice texture.</summary>
    public string TexturePath { get; set; } = string.Empty;

    /// <summary>Pixels from the left edge treated as a fixed border.</summary>
    public int BorderLeft { get; set; } = 8;

    /// <summary>Pixels from the right edge treated as a fixed border.</summary>
    public int BorderRight { get; set; } = 8;

    /// <summary>Pixels from the top edge treated as a fixed border.</summary>
    public int BorderTop { get; set; } = 8;

    /// <summary>Pixels from the bottom edge treated as a fixed border.</summary>
    public int BorderBottom { get; set; } = 8;

    /// <summary>When true, edge regions are tiled instead of stretched.</summary>
    public bool TileEdges { get; set; }

    /// <summary>When true, the center region is tiled instead of stretched.</summary>
    public bool TileCenter { get; set; }
}

/// <summary>
/// Editor-side model for a UI theme asset (.uitheme.json).
/// Stores NineSlice texture paths and border insets for each supported control type.
/// </summary>
public sealed class EditorUITheme
{
    /// <summary>Display name for this theme.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>NineSlice settings for <c>Panel</c>.</summary>
    public EditorUIThemeEntry Panel { get; set; } = new();

    /// <summary>NineSlice settings for <c>Button</c>.</summary>
    public EditorUIThemeEntry Button { get; set; } = new();

    /// <summary>NineSlice settings for <c>Dropdown</c> header.</summary>
    public EditorUIThemeEntry Dropdown { get; set; } = new();

    /// <summary>NineSlice settings for <c>ProgressBar</c> border.</summary>
    public EditorUIThemeEntry ProgressBar { get; set; } = new() { BorderLeft = 4, BorderRight = 4, BorderTop = 4, BorderBottom = 4 };

    /// <summary>NineSlice settings for <c>TextBox</c> / <c>TextArea</c>.</summary>
    public EditorUIThemeEntry TextBox { get; set; } = new() { BorderLeft = 4, BorderRight = 4, BorderTop = 4, BorderBottom = 4 };

    /// <summary>Returns a new, empty theme ready for editing.</summary>
    public static EditorUITheme CreateEmpty(string name = "New UI Theme") => new() { Name = name };
}
