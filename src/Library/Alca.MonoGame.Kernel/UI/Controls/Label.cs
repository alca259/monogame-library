namespace Alca.MonoGame.Kernel.UI.Controls;

/// <summary>Displays text with optional horizontal/vertical alignment and word-wrap.</summary>
public sealed class Label : UIElement
{
    private const int DefaultLineBufferCapacity = 8;

    private SpriteFont? _font;
    private string _text = string.Empty;
    private Vector2 _measuredSize;
    private bool _textDirty = true;

    /// <summary>Pre-allocated line buffer; rebuilt in Measure, read-only in Draw.</summary>
    private readonly List<string> _lines = new(DefaultLineBufferCapacity);

    /// <summary>Pre-allocated line-width cache matching _lines indices.</summary>
    private readonly List<float> _lineWidths = new(DefaultLineBufferCapacity);

    /// <summary>The text to display.</summary>
    public string Text
    {
        get => _text;
        set
        {
            if (_text == value) return;
            _text = value;
            _textDirty = true;
            Invalidate();
        }
    }

    /// <summary>The SpriteFont used to render text. Setting to null hides the label.</summary>
    public SpriteFont? Font
    {
        get => _font;
        set
        {
            if (ReferenceEquals(_font, value)) return;
            _font = value;
            _textDirty = true;
            Invalidate();
        }
    }

    /// <summary>Text draw color.</summary>
    public Color Color { get; set; } = Color.White;

    /// <summary>Horizontal alignment of text within Bounds.</summary>
    public HAlign HAlign { get; set; } = HAlign.Left;

    /// <summary>Vertical alignment of text within Bounds.</summary>
    public VAlign VAlign { get; set; } = VAlign.Top;

    /// <summary>When true, text wraps at word boundaries using the available width from the last Measure call.</summary>
    public bool WrapText { get; set; }

    #region Measure

    /// <inheritdoc/>
    public override void Measure(Vector2 availableSize)
    {
        if (_font is null)
        {
            DesiredSize = Vector2.Zero;
            return;
        }

        if (_textDirty)
            RebuildLines(availableSize.X);

        DesiredSize = _measuredSize;
    }

    private void RebuildLines(float maxWidth)
    {
        _textDirty = false;
        _lines.Clear();
        _lineWidths.Clear();

        if (_text.Length == 0 || _font is null)
        {
            _measuredSize = Vector2.Zero;
            return;
        }

        if (!WrapText)
        {
            _measuredSize = _font.MeasureString(_text);
            _lines.Add(_text);
            _lineWidths.Add(_measuredSize.X);
            return;
        }

        BuildWrappedLines(maxWidth);
    }

    private void BuildWrappedLines(float maxWidth)
    {
        float lineHeight = _font!.LineSpacing;
        float maxLineWidth = 0f;

        // Process newline-separated segments first
        int segStart = 0;
        int len = _text.Length;

        while (segStart <= len)
        {
            // Find next newline or end of string
            int newlineIdx = _text.IndexOf('\n', segStart);
            int segEnd = newlineIdx >= 0 ? newlineIdx : len;
            string segment = _text.Substring(segStart, segEnd - segStart);

            // Wrap this segment
            WrapSegment(segment, maxWidth, ref maxLineWidth);

            segStart = segEnd + 1;
        }

        if (_lines.Count == 0)
        {
            _lines.Add(string.Empty);
            _lineWidths.Add(0f);
        }

        _measuredSize = new Vector2(maxLineWidth, lineHeight * _lines.Count);
    }

    private void WrapSegment(string segment, float maxWidth, ref float maxLineWidth)
    {
        if (segment.Length == 0)
        {
            _lines.Add(string.Empty);
            _lineWidths.Add(0f);
            return;
        }

        float segWidth = _font!.MeasureString(segment).X;
        if (segWidth <= maxWidth)
        {
            _lines.Add(segment);
            _lineWidths.Add(segWidth);
            if (segWidth > maxLineWidth) maxLineWidth = segWidth;
            return;
        }

        // Split by spaces and accumulate words
        int wordStart = 0;
        int lineStart = 0;
        string currentLine = string.Empty;
        float currentWidth = 0f;

        while (wordStart <= segment.Length)
        {
            int spaceIdx = segment.IndexOf(' ', wordStart);
            int wordEnd = spaceIdx >= 0 ? spaceIdx : segment.Length;
            string word = segment.Substring(wordStart, wordEnd - wordStart);

            string testLine = currentLine.Length == 0 ? word : currentLine + " " + word;
            float testWidth = _font.MeasureString(testLine).X;

            if (testWidth > maxWidth && currentLine.Length > 0)
            {
                // Commit current line
                _lines.Add(currentLine);
                _lineWidths.Add(currentWidth);
                if (currentWidth > maxLineWidth) maxLineWidth = currentWidth;
                currentLine = word;
                currentWidth = _font.MeasureString(word).X;
            }
            else
            {
                currentLine = testLine;
                currentWidth = testWidth;
            }

            wordStart = wordEnd + 1;
            _ = lineStart; // suppress unused warning
        }

        if (currentLine.Length > 0)
        {
            _lines.Add(currentLine);
            _lineWidths.Add(currentWidth);
            if (currentWidth > maxLineWidth) maxLineWidth = currentWidth;
        }
    }

    #endregion

    #region Draw

    /// <inheritdoc/>
    public override void Draw(SpriteBatch spriteBatch)
    {
        if (!IsVisible || _font is null || _text.Length == 0) return;

        Color drawColor = Color * EffectiveOpacity;

        if (_lines.Count <= 1)
        {
            DrawSingleLine(spriteBatch, _text, _measuredSize, drawColor);
            return;
        }

        DrawMultiLine(spriteBatch, drawColor);
    }

    private void DrawSingleLine(SpriteBatch spriteBatch, string text, Vector2 size, Color drawColor)
    {
        float x = ComputeLineX(size.X);
        float y = VAlign switch
        {
            VAlign.Middle => Bounds.Y + (Bounds.Height - size.Y) / 2f,
            VAlign.Bottom => Bounds.Bottom - size.Y,
            _ => Bounds.Y,
        };

        spriteBatch.DrawString(_font!, text, new Vector2(x, y), drawColor);
    }

    private void DrawMultiLine(SpriteBatch spriteBatch, Color drawColor)
    {
        float lineHeight = _font!.LineSpacing;
        float totalHeight = lineHeight * _lines.Count;

        float startY = VAlign switch
        {
            VAlign.Middle => Bounds.Y + (Bounds.Height - totalHeight) / 2f,
            VAlign.Bottom => Bounds.Bottom - totalHeight,
            _ => Bounds.Y,
        };

        for (int i = 0; i < _lines.Count; i++)
        {
            float x = i < _lineWidths.Count
                ? ComputeLineX(_lineWidths[i])
                : ComputeLineX(0f);

            spriteBatch.DrawString(_font, _lines[i], new Vector2(x, startY + i * lineHeight), drawColor);
        }
    }

    private float ComputeLineX(float lineWidth) => HAlign switch
    {
        HAlign.Center => Bounds.X + (Bounds.Width - lineWidth) / 2f,
        HAlign.Right => Bounds.Right - lineWidth,
        _ => Bounds.X,
    };

    #endregion
}
