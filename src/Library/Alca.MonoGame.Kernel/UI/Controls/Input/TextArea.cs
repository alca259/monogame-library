using Alca.MonoGame.Kernel.Graphics;
using Alca.MonoGame.Kernel.UI.Controls.Base;

namespace Alca.MonoGame.Kernel.UI.Controls.Input;

/// <summary>Multi-line text area with Enter-key line breaks, word wrap, and vertical scrolling.</summary>
public sealed class TextArea : TextBoxBase
{
    #region Fields

    private const int ScrollBarWidth = 8;

    private readonly List<string> _lines = new(8);
    private int _scrollOffsetLines;
    private bool _linesDirty = true;
    private int _lastWheelValue;

    // Cached cursor line prefix — rebuilt on cursor change to avoid alloc in Draw.
    private string _cachedCursorLinePrefix = string.Empty;

    #endregion

    #region Properties

    /// <summary>Maximum number of total lines (-1 = unlimited).</summary>
    public int MaxLines { get; set; } = -1;

    /// <summary>When true, lines wider than Bounds.Width are soft-wrapped by character.</summary>
    public bool WordWrap { get; set; }

    #endregion

    #region Constructor

    /// <summary>Creates a new TextArea.</summary>
    public TextArea(SpriteFont? font, Texture2D? pixel, GameWindow? window)
        : base(font, pixel, window) { }

    #endregion

    #region TextBoxBase overrides

    /// <inheritdoc/>
    protected override void OnEnterPressed()
    {
        if (IsReadOnly) return;

        int currentLines = CountHardLines();
        if (MaxLines >= 0 && currentLines >= MaxLines) return;

        _text.Insert(_cursorIndex, '\n');
        _cursorIndex++;
        _selectionStart = _cursorIndex;
        _linesDirty = true;
        MarkDirty();
    }

    /// <inheritdoc/>
    protected override void OnTextCacheRebuilt()
    {
        _linesDirty = true;
    }

    /// <inheritdoc/>
    protected override void OnCursorCacheRebuilt()
    {
        // Rebuild cursor line prefix to avoid string alloc in Draw.
        RebuildLines();
        var (line, col) = IndexToLineCol(_cursorIndex);
        if (line < _lines.Count)
        {
            int safeCol = Math.Min(col, _lines[line].Length);
            _cachedCursorLinePrefix = safeCol > 0 ? _lines[line].Substring(0, safeCol) : string.Empty;
        }
        else
        {
            _cachedCursorLinePrefix = string.Empty;
        }

        ScrollToCursor(line);
    }

    private void ScrollToCursor(int cursorLine)
    {
        int visible = VisibleLineCount();
        if (cursorLine < _scrollOffsetLines)
            _scrollOffsetLines = cursorLine;
        else if (cursorLine >= _scrollOffsetLines + visible)
            _scrollOffsetLines = cursorLine - visible + 1;
    }

    /// <inheritdoc/>
    protected override void ProcessKeyboardInput(KeyboardState current, KeyboardState prev)
    {
        bool shift = current.IsKeyDown(Keys.LeftShift) || current.IsKeyDown(Keys.RightShift);

        if (WasJustPressed(current, prev, Keys.Up))
        {
            MoveCursorVertical(-1, shift);
            return;
        }

        if (WasJustPressed(current, prev, Keys.Down))
        {
            MoveCursorVertical(1, shift);
            return;
        }

        base.ProcessKeyboardInput(current, prev);
    }

    #endregion

    #region Line management

    private int CountHardLines()
    {
        int count = 1;
        for (int i = 0; i < _text.Length; i++)
            if (_text[i] == '\n') count++;
        return count;
    }

    private void RebuildLines()
    {
        if (!_linesDirty) return;

        _lines.Clear();
        int wrapWidth = (WordWrap && Bounds.Width > 8) ? Bounds.Width - 8 - ScrollBarWidth : int.MaxValue;

        int start = 0;
        for (int i = 0; i <= _text.Length; i++)
        {
            if (i == _text.Length || _text[i] == '\n')
            {
                AddWrapped(_text.ToString(start, i - start), wrapWidth);
                start = i + 1;
            }
        }

        _linesDirty = false;
        ClampScrollOffset();
    }

    private void AddWrapped(string segment, int maxWidth)
    {
        if (_font is null || maxWidth == int.MaxValue)
        {
            _lines.Add(segment);
            return;
        }

        if (segment.Length == 0) { _lines.Add(string.Empty); return; }

        int lineStart = 0;
        for (int i = 1; i <= segment.Length; i++)
        {
            string candidate = segment.Substring(lineStart, i - lineStart);
            if (_font.MeasureString(candidate).X > maxWidth)
            {
                int breakAt = i - 1 > lineStart ? i - 1 : i;
                _lines.Add(segment.Substring(lineStart, breakAt - lineStart));
                lineStart = breakAt;
            }
        }

        if (lineStart < segment.Length)
            _lines.Add(segment.Substring(lineStart));
    }

    private void ClampScrollOffset()
    {
        int maxScroll = Math.Max(0, _lines.Count - VisibleLineCount());
        _scrollOffsetLines = Math.Clamp(_scrollOffsetLines, 0, maxScroll);
    }

    private int VisibleLineCount()
    {
        int lineH = _font?.LineSpacing ?? 16;
        return lineH > 0 ? Math.Max(1, (Bounds.Height - 8) / lineH) : 1;
    }

    private (int Line, int Col) IndexToLineCol(int idx)
    {
        RebuildLines();
        int remaining = idx;
        for (int l = 0; l < _lines.Count; l++)
        {
            int lineLen = _lines[l].Length;
            bool isLastLine = l == _lines.Count - 1;
            if (remaining <= lineLen || isLastLine)
                return (l, Math.Min(remaining, lineLen));
            remaining -= lineLen + 1; // +1 for \n
        }

        return (_lines.Count - 1, _lines.Count > 0 ? _lines[^1].Length : 0);
    }

    private int LineColToIndex(int line, int col)
    {
        RebuildLines();
        line = Math.Clamp(line, 0, _lines.Count - 1);
        int idx = 0;
        for (int l = 0; l < line; l++)
            idx += _lines[l].Length + 1; // +1 for \n
        col = Math.Clamp(col, 0, _lines[line].Length);
        return idx + col;
    }

    private void MoveCursorVertical(int delta, bool shift)
    {
        var (line, col) = IndexToLineCol(_cursorIndex);
        int newLine = Math.Clamp(line + delta, 0, _lines.Count - 1);
        if (newLine == line) return;

        int newIdx = LineColToIndex(newLine, col);
        _cursorIndex = newIdx;
        if (!shift) _selectionStart = newIdx;
        MarkCursorDirty();

        if (newLine < _scrollOffsetLines)
            _scrollOffsetLines = newLine;
        else if (newLine >= _scrollOffsetLines + VisibleLineCount())
            _scrollOffsetLines = newLine - VisibleLineCount() + 1;
    }

    #endregion

    #region Update

    /// <inheritdoc/>
    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (!IsEnabled) return;

        MouseState ms = Mouse.GetState();
        if (Bounds.Contains(ms.Position))
        {
            int wheelDelta = ms.ScrollWheelValue - _lastWheelValue;
            if (wheelDelta != 0)
            {
                int lineDelta = wheelDelta > 0 ? -1 : 1;
                _scrollOffsetLines = Math.Clamp(
                    _scrollOffsetLines + lineDelta,
                    0,
                    Math.Max(0, _lines.Count - VisibleLineCount()));
            }
        }

        _lastWheelValue = ms.ScrollWheelValue;
    }

    #endregion

    #region Layout / Draw

    /// <inheritdoc/>
    public override void Measure(Vector2 availableSize)
    {
        int lineH = _font?.LineSpacing ?? 16;
        DesiredSize = new Vector2(availableSize.X, lineH * 4 + 10);
    }

    /// <inheritdoc/>
    public override void Arrange(Rectangle finalBounds)
    {
        base.Arrange(finalBounds);
        _linesDirty = true;
    }

    /// <inheritdoc/>
    public override void Draw(SpriteBatch spriteBatch)
    {
        if (!IsVisible || _pixel is null || _font is null) return;

        RebuildCacheIfNeeded();
        RebuildLines();

        float opacity = EffectiveOpacity;
        Color border = _isFocused ? FocusBorderColor : BorderColor;
        int lineH = _font.LineSpacing;

        spriteBatch.Draw(_pixel, Bounds, BackColor * opacity);
        DrawHelper.DrawBorder(_pixel, spriteBatch, Bounds, border * opacity, 1);

        int visibleCount = VisibleLineCount();
        int endLine = Math.Min(_scrollOffsetLines + visibleCount, _lines.Count);

        var (cursorLine, _) = IndexToLineCol(_cursorIndex);

        for (int l = _scrollOffsetLines; l < endLine; l++)
        {
            float y = Bounds.Y + 4 + (l - _scrollOffsetLines) * lineH;
            spriteBatch.DrawString(_font, _lines[l],
                new Vector2(Bounds.X + 4, y),
                TextColor * opacity);

            if (_isFocused && _cursorVisible && !IsReadOnly && l == cursorLine)
            {
                // Use pre-cached prefix string — no alloc in Draw.
                float cx = Bounds.X + 4 + _font.MeasureString(_cachedCursorLinePrefix).X;
                spriteBatch.Draw(_pixel,
                    new Rectangle((int)cx, (int)y, 1, lineH),
                    TextColor * opacity);
            }
        }

        if (_lines.Count > visibleCount)
            DrawScrollBar(spriteBatch, opacity, _lines.Count, visibleCount);
    }

    private void DrawScrollBar(SpriteBatch spriteBatch, float opacity, int totalLines, int visibleLines)
    {
        int trackX = Bounds.Right - ScrollBarWidth - 1;
        int trackY = Bounds.Y + 2;
        int trackH = Bounds.Height - 4;

        spriteBatch.Draw(_pixel!, new Rectangle(trackX, trackY, ScrollBarWidth, trackH),
            new Color(50, 50, 50) * opacity);

        float thumbRatio = Math.Min(1f, (float)visibleLines / totalLines);
        int thumbH = Math.Max(10, (int)(trackH * thumbRatio));
        int maxScroll = totalLines - visibleLines;
        float scrollRatio = maxScroll > 0 ? (float)_scrollOffsetLines / maxScroll : 0f;
        int thumbY = trackY + (int)((trackH - thumbH) * scrollRatio);

        spriteBatch.Draw(_pixel!, new Rectangle(trackX + 1, thumbY, ScrollBarWidth - 2, thumbH),
            BorderColor * opacity);
    }

    #endregion
}
