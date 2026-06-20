using Alca.MonoGame.Kernel.UI.Controls.Base;

namespace Alca.MonoGame.Kernel.UI.Controls.Input;

/// <summary>Single-line password field that renders all characters as a mask character.</summary>
public sealed class PasswordBox : TextBoxBase
{
    #region Fields

    private char _maskChar = '•';
    private string _cachedMasked = string.Empty;
    private string _cachedMaskedBeforeCursor = string.Empty;

    #endregion

    #region Properties

    /// <summary>Character displayed in place of each real character. Default is '•'.</summary>
    public char MaskChar
    {
        get => _maskChar;
        set
        {
            _maskChar = value;
            OnTextCacheRebuilt();
        }
    }

    /// <summary>Returns the real (unmasked) text. Handle with care — avoid logging.</summary>
    public string Password => Text;

    #endregion

    #region Constructor

    /// <summary>Creates a new PasswordBox.</summary>
    public PasswordBox(SpriteFont? font, Texture2D? pixel, GameWindow? window)
        : base(font, pixel, window)
    {
    }

    #endregion

    #region Mask helpers

    /// <inheritdoc/>
    protected override void OnTextCacheRebuilt()
    {
        // Rebuild masked strings when text changes.
        int len = _text.Length;
        _cachedMasked = len > 0 ? new string(_maskChar, len) : string.Empty;
        int safeIdx = Math.Min(_cursorIndex, len);
        _cachedMaskedBeforeCursor = safeIdx > 0 ? new string(_maskChar, safeIdx) : string.Empty;
    }

    /// <inheritdoc/>
    protected override void OnCursorCacheRebuilt()
    {
        // Rebuild masked before-cursor string when cursor moves without text change.
        int safeIdx = Math.Min(_cursorIndex, _text.Length);
        _cachedMaskedBeforeCursor = safeIdx > 0 ? new string(_maskChar, safeIdx) : string.Empty;
    }

    #endregion

    #region Layout / Draw

    /// <inheritdoc/>
    public override void Measure(Vector2 availableSize)
    {
        int height = (_font?.LineSpacing ?? 16) + 10;
        DesiredSize = new Vector2(availableSize.X, height);
    }

    /// <inheritdoc/>
    public override void Draw(SpriteBatch spriteBatch)
    {
        if (!IsVisible) return;
        // Pass masked strings so cursor and selection positions use mask-char width.
        DrawSingleLine(spriteBatch, _cachedMasked, _cachedMaskedBeforeCursor);
    }

    #endregion
}
