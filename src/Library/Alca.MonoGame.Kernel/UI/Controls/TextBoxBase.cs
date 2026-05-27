using System.Text;
using Alca.MonoGame.Kernel.Graphics;
using Alca.MonoGame.Kernel.UI.Focus;
using Alca.MonoGame.Kernel.UI.Interaction;

namespace Alca.MonoGame.Kernel.UI.Controls;

/// <summary>Abstract base for all text-input controls. Manages the text buffer, cursor, selection, blink timer, and keyboard navigation.</summary>
public abstract class TextBoxBase : UIElement, IUIInteractable, IFocusable
{
    #region Fields

    protected readonly SpriteFont? _font;
    protected readonly Texture2D? _pixel;
    private readonly GameWindow? _window;

    protected readonly StringBuilder _text = new(64);

    protected int _cursorIndex;
    protected int _selectionStart;
    protected int _scrollOffset;

    protected float _blinkTimer;
    protected bool _cursorVisible = true;
    protected bool _isFocused;
    protected bool _isHovered;

    private string _cachedText = string.Empty;
    protected string _cachedBeforeCursor = string.Empty;
    private string _cachedSelectionBefore = string.Empty;
    private string _cachedSelectionText = string.Empty;
    private bool _cursorDirty;

    #endregion

    #region Properties

    /// <summary>Current text content (read-only; use SetText or keyboard input to modify).</summary>
    public string Text => _cachedText;

    /// <summary>Current cursor insertion index within Text.</summary>
    public int CursorIndex => _cursorIndex;

    /// <summary>Anchor index of the active selection. Equals CursorIndex when no selection is active.</summary>
    public int SelectionStartIndex => _selectionStart;

    /// <summary>Placeholder text displayed when the control is empty and unfocused.</summary>
    public string Placeholder { get; set; } = string.Empty;

    /// <summary>Maximum number of characters. -1 = unlimited.</summary>
    public int MaxLength { get; set; } = -1;

    /// <summary>When true the user cannot type or delete text.</summary>
    public bool IsReadOnly { get; set; }

    /// <summary>Background fill color.</summary>
    public Color BackColor { get; set; } = new Color(30, 30, 30);

    /// <summary>Text and cursor color.</summary>
    public Color TextColor { get; set; } = Color.White;

    /// <summary>Color used for placeholder text.</summary>
    public Color PlaceholderColor { get; set; } = new Color(120, 120, 120);

    /// <summary>Border color when unfocused.</summary>
    public Color BorderColor { get; set; } = Color.Gray;

    /// <summary>Border color when focused.</summary>
    public Color FocusBorderColor { get; set; } = new Color(100, 149, 237);

    /// <summary>Background tint for the selected text range.</summary>
    public Color SelectionColor { get; set; } = new Color(100, 149, 237, 128);

    /// <summary>Fired whenever the text changes.</summary>
    public event Action<string>? TextChanged;

    /// <summary>Fired when the user presses Enter (single-line variants).</summary>
    public event Action? Submitted;

    #endregion

    #region Constructor

    /// <summary>Creates a new TextBoxBase.</summary>
    /// <param name="font">Font used for text rendering and cursor measurement. May be null.</param>
    /// <param name="pixel">1×1 white pixel texture for background, border, cursor and selection. May be null.</param>
    /// <param name="window">GameWindow for TextInput event subscription. Null disables IME input (useful in tests).</param>
    protected TextBoxBase(SpriteFont? font, Texture2D? pixel, GameWindow? window)
    {
        _font = font;
        _pixel = pixel;
        _window = window;
    }

    #endregion

    #region Public API

    /// <summary>Replaces the entire text content programmatically and resets cursor to end.</summary>
    public void SetText(string text)
    {
        _text.Clear();
        _text.Append(text);
        _cursorIndex = _text.Length;
        _selectionStart = _cursorIndex;
        MarkDirty();
    }

    /// <summary>Processes a single character from the TextInput event. Safe to call from tests.</summary>
    public virtual void HandleTextInput(char c)
    {
        if (IsReadOnly) return;

        if (c == '\b')
        {
            if (HasSelection) DeleteSelection();
            else DeleteBeforeCursor();
            return;
        }

        if (c == '\r' || c == '\n') { OnEnterPressed(); return; }
        if (char.IsControl(c)) return;
        if (MaxLength >= 0 && _text.Length >= MaxLength && !HasSelection) return;
        if (!AcceptChar(c)) return;

        if (HasSelection) DeleteSelection();

        if (MaxLength >= 0 && _text.Length >= MaxLength) return;

        _text.Insert(_cursorIndex, c);
        _cursorIndex++;
        _selectionStart = _cursorIndex;
        MarkDirty();
    }

    #endregion

    #region Protected helpers

    /// <summary>Override to filter which characters are accepted. Return false to silently reject the character.</summary>
    protected virtual bool AcceptChar(char c) => true;

    /// <summary>Called when the user presses Enter. Default behaviour fires the Submitted event.</summary>
    protected virtual void OnEnterPressed() => Submitted?.Invoke();

    /// <summary>Called when the control loses focus. Override for validation / reformatting.</summary>
    protected virtual void OnBlur() { }

    /// <summary>Called after the text cache is rebuilt (i.e. the text changed). Override to rebuild derived caches.</summary>
    protected virtual void OnTextCacheRebuilt() { }

    /// <summary>Called after the cursor/selection cache is rebuilt. Override to rebuild derived caches.</summary>
    protected virtual void OnCursorCacheRebuilt() { }

    protected bool HasSelection => _selectionStart != _cursorIndex;

    protected (int Min, int Max) GetSelectionRange()
    {
        return _selectionStart < _cursorIndex
            ? (_selectionStart, _cursorIndex)
            : (_cursorIndex, _selectionStart);
    }

    protected void DeleteBeforeCursor()
    {
        if (_cursorIndex <= 0) return;
        _text.Remove(_cursorIndex - 1, 1);
        _cursorIndex--;
        _selectionStart = _cursorIndex;
        MarkDirty();
    }

    protected void DeleteAfterCursor()
    {
        if (_cursorIndex >= _text.Length) return;
        _text.Remove(_cursorIndex, 1);
        MarkDirty();
    }

    protected void DeleteSelection()
    {
        var (min, max) = GetSelectionRange();
        _text.Remove(min, max - min);
        _cursorIndex = min;
        _selectionStart = min;
        MarkDirty();
    }

    protected void MarkDirty()
    {
        _cachedText = _text.ToString();
        _cursorDirty = true;
        _blinkTimer = 0f;
        _cursorVisible = true;
        TextChanged?.Invoke(_cachedText);
        OnTextCacheRebuilt();
    }

    protected void MarkCursorDirty()
    {
        _cursorDirty = true;
        _blinkTimer = 0f;
        _cursorVisible = true;
    }

    protected void RebuildCacheIfNeeded()
    {
        if (_cursorDirty)
        {
            int safeIdx = Math.Min(_cursorIndex, _text.Length);
            _cachedBeforeCursor = _text.ToString(0, safeIdx);

            if (HasSelection)
            {
                var (selMin, selMax) = GetSelectionRange();
                _cachedSelectionBefore = _text.ToString(0, selMin);
                _cachedSelectionText = _text.ToString(selMin, selMax - selMin);
            }
            else
            {
                _cachedSelectionBefore = string.Empty;
                _cachedSelectionText = string.Empty;
            }

            UpdateScrollOffset();
            _cursorDirty = false;
            OnCursorCacheRebuilt();
        }
    }

    private void UpdateScrollOffset()
    {
        if (_font is null || Bounds.Width <= 0) return;

        int innerWidth = Bounds.Width - 8;
        if (innerWidth <= 0) return;

        float cursorX = _font.MeasureString(_cachedBeforeCursor).X;

        if (cursorX - _scrollOffset > innerWidth)
            _scrollOffset = (int)(cursorX - innerWidth);
        else if (cursorX - _scrollOffset < 0)
            _scrollOffset = (int)cursorX;

        if (_scrollOffset < 0) _scrollOffset = 0;
    }

    /// <summary>Renders background, border, text with horizontal scroll, selection highlight, and blinking cursor.</summary>
    /// <param name="spriteBatch">The SpriteBatch used for rendering.</param>
    /// <param name="displayText">The string to render (may be a masked version for PasswordBox).</param>
    /// <param name="beforeCursorMeasure">String measured to find cursor X. Defaults to <see cref="_cachedBeforeCursor"/> when null.</param>
    protected void DrawSingleLine(SpriteBatch spriteBatch, string displayText, string? beforeCursorMeasure = null)
    {
        RebuildCacheIfNeeded();
        if (_pixel is null || _font is null) return;

        float opacity = EffectiveOpacity;
        Color border = _isFocused ? FocusBorderColor : BorderColor;

        spriteBatch.Draw(_pixel, Bounds, BackColor * opacity);
        DrawHelper.DrawBorder(_pixel, spriteBatch, Bounds, border * opacity, 1);

        var textOrigin = new Vector2(Bounds.X + 4, Bounds.Y + (Bounds.Height - _font.LineSpacing) / 2f);
        int textX = (int)textOrigin.X;
        int textY = (int)textOrigin.Y;

        if (displayText.Length == 0 && Placeholder.Length > 0)
        {
            spriteBatch.DrawString(_font, Placeholder, textOrigin, PlaceholderColor * opacity);
        }
        else if (displayText.Length > 0)
        {
            spriteBatch.DrawString(_font, displayText,
                new Vector2(textX - _scrollOffset, textOrigin.Y),
                TextColor * opacity);
        }

        // Selection highlight
        if (HasSelection && _cachedSelectionText.Length > 0)
        {
            float selBeforeW = _font.MeasureString(_cachedSelectionBefore).X;
            float selW = _font.MeasureString(_cachedSelectionText).X;
            int sx = Math.Clamp(textX + (int)selBeforeW - _scrollOffset, Bounds.X + 2, Bounds.Right - 2);
            int ex = Math.Clamp(textX + (int)(selBeforeW + selW) - _scrollOffset, Bounds.X + 2, Bounds.Right - 2);
            if (ex > sx)
                spriteBatch.Draw(_pixel, new Rectangle(sx, textY, ex - sx, _font.LineSpacing), SelectionColor * opacity);
        }

        // Cursor
        if (_isFocused && _cursorVisible && !IsReadOnly)
        {
            string measure = beforeCursorMeasure ?? _cachedBeforeCursor;
            float beforeW = _font.MeasureString(measure).X;
            int cx = textX + (int)beforeW - _scrollOffset;
            if (cx >= Bounds.X + 2 && cx < Bounds.Right - 2)
                spriteBatch.Draw(_pixel, new Rectangle(cx, textY, 1, _font.LineSpacing), TextColor * opacity);
        }
    }

    #endregion

    #region Update

    /// <inheritdoc/>
    public override void Update(GameTime gameTime)
    {
        if (!IsEnabled) return;

        _blinkTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (_blinkTimer >= 0.5f) { _blinkTimer -= 0.5f; _cursorVisible = !_cursorVisible; }

        if (_isFocused) HandleKeyboardUpdate();
    }

    private void HandleKeyboardUpdate()
    {
        ProcessKeyboardInput(Core.Input.Keyboard.CurrentState, Core.Input.Keyboard.PreviousState);
    }

    /// <summary>Processes keyboard navigation keys. Override to add variant-specific key handling.</summary>
    protected virtual void ProcessKeyboardInput(KeyboardState current, KeyboardState prev)
    {
        if (IsReadOnly) return;

        bool shift = current.IsKeyDown(Keys.LeftShift) || current.IsKeyDown(Keys.RightShift);

        if (WasJustPressed(current, prev, Keys.Left))
        {
            if (HasSelection && !shift)
            {
                _cursorIndex = GetSelectionRange().Min;
                _selectionStart = _cursorIndex;
            }
            else if (_cursorIndex > 0)
            {
                _cursorIndex--;
                if (!shift) _selectionStart = _cursorIndex;
            }
            MarkCursorDirty();
        }
        else if (WasJustPressed(current, prev, Keys.Right))
        {
            if (HasSelection && !shift)
            {
                _cursorIndex = GetSelectionRange().Max;
                _selectionStart = _cursorIndex;
            }
            else if (_cursorIndex < _text.Length)
            {
                _cursorIndex++;
                if (!shift) _selectionStart = _cursorIndex;
            }
            MarkCursorDirty();
        }
        else if (WasJustPressed(current, prev, Keys.Home))
        {
            _cursorIndex = 0;
            if (!shift) _selectionStart = 0;
            MarkCursorDirty();
        }
        else if (WasJustPressed(current, prev, Keys.End))
        {
            _cursorIndex = _text.Length;
            if (!shift) _selectionStart = _cursorIndex;
            MarkCursorDirty();
        }
        else if (WasJustPressed(current, prev, Keys.Delete))
        {
            if (HasSelection) DeleteSelection();
            else DeleteAfterCursor();
        }
    }

    protected static bool WasJustPressed(KeyboardState current, KeyboardState prev, Keys key)
        => current.IsKeyDown(key) && prev.IsKeyUp(key);

    #endregion

    #region Click-to-position

    private void PositionCursorAtPoint(Point clickPos)
    {
        if (_font is null) return;

        RebuildCacheIfNeeded();
        string txt = _cachedText;
        float textLeft = Bounds.X + 4 - _scrollOffset;

        int best = 0;
        float bestDist = float.MaxValue;

        // Walk character by character — O(n) with one Substring alloc per iteration
        // Acceptable: this runs only on pointer-down, not every frame.
        for (int i = 0; i <= txt.Length; i++)
        {
            float x = textLeft + _font.MeasureString(txt.Substring(0, i)).X;
            float dist = MathF.Abs(x - clickPos.X);
            if (dist < bestDist) { bestDist = dist; best = i; }
        }

        _cursorIndex = best;
        _selectionStart = best;
        MarkCursorDirty();
    }

    #endregion

    #region IUIInteractable

    /// <inheritdoc/>
    public bool IsHovered => _isHovered;

    /// <inheritdoc/>
    public void OnPointerEnter() => _isHovered = true;

    /// <inheritdoc/>
    public void OnPointerLeave() => _isHovered = false;

    /// <inheritdoc/>
    public void OnPointerDown(ref UIPointerEventArgs args)
    {
        if (!IsEnabled) return;
        PositionCursorAtPoint(args.Position);
        args.Handled = true;
    }

    /// <inheritdoc/>
    public void OnPointerUp(ref UIPointerEventArgs args) { }

    #endregion

    #region IFocusable

    /// <inheritdoc/>
    public int TabIndex { get; set; }

    /// <inheritdoc/>
    public int? FocusNeighborUp { get; set; }

    /// <inheritdoc/>
    public int? FocusNeighborDown { get; set; }

    /// <inheritdoc/>
    public int? FocusNeighborLeft { get; set; }

    /// <inheritdoc/>
    public int? FocusNeighborRight { get; set; }

    /// <inheritdoc/>
    public bool IsFocused => _isFocused;

    /// <inheritdoc/>
    public void OnFocusGained()
    {
        _isFocused = true;
        _cursorVisible = true;
        _blinkTimer = 0f;
        _window?.TextInput += OnWindowTextInput;
    }

    /// <inheritdoc/>
    public void OnFocusLost()
    {
        _isFocused = false;
        _cursorVisible = false;
        _window?.TextInput -= OnWindowTextInput;
        OnBlur();
    }

    private void OnWindowTextInput(object? sender, TextInputEventArgs e) => HandleTextInput(e.Character);

    #endregion
}
