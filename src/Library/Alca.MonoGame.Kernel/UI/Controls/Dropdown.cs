using Alca.MonoGame.Kernel.Graphics;
using Alca.MonoGame.Kernel.UI.Focus;
using Alca.MonoGame.Kernel.UI.Interaction;

namespace Alca.MonoGame.Kernel.UI.Controls;

/// <summary>A collapsed header that expands an overlay item list when clicked or activated via keyboard.</summary>
public sealed class Dropdown : UIElement, IUIInteractable, IFocusable
{
    #region Constants

    private const int DefaultItemHeight = 28;
    private const int ArrowWidth = 20;
    private const int TextPadding = 6;

    #endregion

    #region Fields

    private readonly List<string> _options = new(8);
    private readonly UIOverlayManager _overlayManager;
    private readonly Panel _overlayPanel;
    private readonly List<Label> _itemLabels = new(8);

    private bool _isExpanded;
    private bool _isHovered;
    private bool _isFocused;
    private int _selectedIndex = -1;
    private int _highlightedIndex = -1;
    private Rectangle _listBounds;
    private int _screenHeight;

    private KeyboardState _prevKeyState;
    private GamePadState _prevGamePadState;

    #endregion

    #region Properties

    /// <summary>1×1 white pixel texture for background and border rendering.</summary>
    public Texture2D? Pixel { get; set; }

    /// <summary>Font used to render the header text and item labels.</summary>
    public SpriteFont? Font { get; set; }

    /// <summary>Height of each item row in the expanded list.</summary>
    public int ItemHeight { get; set; } = DefaultItemHeight;

    /// <summary>Background color of the collapsed header.</summary>
    public Color HeaderColor { get; set; } = new Color(50, 50, 50);

    /// <summary>Background color of the expanded list panel.</summary>
    public Color ListBackgroundColor { get; set; } = new Color(40, 40, 40);

    /// <summary>Background color of the currently highlighted item.</summary>
    public Color HighlightColor { get; set; } = new Color(100, 149, 237);

    /// <summary>Background color of the currently selected item.</summary>
    public Color SelectedColor { get; set; } = new Color(70, 100, 180);

    /// <summary>Text color for header and items.</summary>
    public Color TextColor { get; set; } = Color.White;

    /// <summary>Border color for header and list panel.</summary>
    public Color BorderColor { get; set; } = new Color(120, 120, 120);

    /// <summary>Border color when focused.</summary>
    public Color FocusBorderColor { get; set; } = new Color(100, 149, 237);

    /// <summary>Index of the currently selected item. -1 means no selection.</summary>
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            int clamped = (_options.Count == 0) ? -1 : Math.Clamp(value, -1, _options.Count - 1);
            if (clamped == _selectedIndex) return;
            _selectedIndex = clamped;
            SelectionChanged?.Invoke(_selectedIndex);
        }
    }

    /// <summary>Text of the currently selected item, or an empty string if nothing is selected.</summary>
    public string SelectedText => (_selectedIndex >= 0 && _selectedIndex < _options.Count)
        ? _options[_selectedIndex]
        : string.Empty;

    /// <summary>Whether the item list is currently visible.</summary>
    public bool IsExpanded => _isExpanded;

    /// <summary>Screen height used for flip detection. Set this before use (e.g. from GraphicsDevice.Viewport.Height).</summary>
    public int ScreenHeight
    {
        get => _screenHeight;
        set => _screenHeight = value;
    }

    /// <summary>Fired when <see cref="SelectedIndex"/> changes. Passes the new index.</summary>
    public event Action<int>? SelectionChanged;

    #endregion

    #region Constructor

    /// <summary>Creates a Dropdown attached to the given overlay manager.</summary>
    public Dropdown(UIOverlayManager overlayManager)
    {
        _overlayManager = overlayManager;
        _overlayPanel = new Panel
        {
            IsVisible = false,
            IsEnabled = false,
        };
    }

    #endregion

    #region Items

    /// <summary>Appends a string option to the list.</summary>
    public void AddItem(string text)
    {
        _options.Add(text);
        var label = new Label { Text = text, Font = Font, Color = TextColor };
        _itemLabels.Add(label);
        _overlayPanel.Add(label);
    }

    /// <summary>Removes all options and resets selection.</summary>
    public void ClearItems()
    {
        _options.Clear();
        _itemLabels.Clear();
        while (_overlayPanel.ChildrenReadOnly.Count > 0)
            _overlayPanel.Remove(_overlayPanel.ChildrenReadOnly[0]);
        _selectedIndex = -1;
        _highlightedIndex = -1;
    }

    #endregion

    #region Open / Close

    /// <summary>Expands the item list, registering the overlay panel with the UIOverlayManager.</summary>
    public void Open()
    {
        if (_isExpanded || _options.Count == 0) return;
        _isExpanded = true;
        _highlightedIndex = _selectedIndex >= 0 ? _selectedIndex : 0;
        RebuildListBounds();
        ArrangeItemLabels();
        _overlayPanel.IsVisible = true;
        _overlayPanel.IsEnabled = true;
        _overlayManager.Show(_overlayPanel);
    }

    /// <summary>Collapses the item list, removing the overlay from the UIOverlayManager.</summary>
    public void Close()
    {
        if (!_isExpanded) return;
        _isExpanded = false;
        _overlayPanel.IsVisible = false;
        _overlayPanel.IsEnabled = false;
        _overlayManager.Hide(_overlayPanel);
    }

    private void RebuildListBounds()
    {
        int listHeight = _options.Count * ItemHeight;
        bool flipUp = _screenHeight > 0 && (Bounds.Bottom + listHeight) > _screenHeight;

        _listBounds = flipUp
            ? new Rectangle(Bounds.X, Bounds.Y - listHeight, Bounds.Width, listHeight)
            : new Rectangle(Bounds.X, Bounds.Bottom, Bounds.Width, listHeight);

        _overlayPanel.Arrange(_listBounds);
        _overlayPanel.BackgroundColor = ListBackgroundColor;
        _overlayPanel.BorderColor = BorderColor;
        _overlayPanel.BorderThickness = 1;
    }

    private void ArrangeItemLabels()
    {
        for (int i = 0; i < _itemLabels.Count; i++)
        {
            _itemLabels[i].Font = Font;
            _itemLabels[i].Color = TextColor;
            _itemLabels[i].Arrange(new Rectangle(
                _listBounds.X,
                _listBounds.Y + i * ItemHeight,
                _listBounds.Width,
                ItemHeight));
        }
    }

    #endregion

    #region Layout

    /// <inheritdoc/>
    public override void Measure(Vector2 availableSize)
    {
        int height = Font is not null ? (int)Font.MeasureString("Ag").Y + TextPadding * 2 : DefaultItemHeight;
        DesiredSize = new Vector2(availableSize.X, height);
    }

    /// <inheritdoc/>
    public override void Arrange(Rectangle finalBounds)
    {
        base.Arrange(finalBounds);
        if (_isExpanded) RebuildListBounds();
    }

    #endregion

    #region Update / Draw

    /// <inheritdoc/>
    public override void Update(GameTime gameTime)
    {
        if (!IsEnabled) return;

        // Handle clicks on overlay items
        if (_isExpanded)
        {
            MouseState ms = Mouse.GetState();
            Point mousePos = ms.Position;

            // Update highlighted item based on hover
            _highlightedIndex = -1;
            for (int i = 0; i < _itemLabels.Count; i++)
            {
                if (_itemLabels[i].Bounds.Contains(mousePos))
                {
                    _highlightedIndex = i;
                    break;
                }
            }
        }

        if (_isFocused)
        {
            KeyboardState ks = Keyboard.GetState();
            GamePadState gp = GamePad.GetState(PlayerIndex.One);

            if (_isExpanded)
            {
                if (WasKeyJustPressed(ks, Keys.Up) || WasPadJustPressed(gp, Buttons.DPadUp))
                {
                    if (_highlightedIndex > 0) _highlightedIndex--;
                    else _highlightedIndex = _options.Count - 1;
                }
                else if (WasKeyJustPressed(ks, Keys.Down) || WasPadJustPressed(gp, Buttons.DPadDown))
                {
                    if (_highlightedIndex < _options.Count - 1) _highlightedIndex++;
                    else _highlightedIndex = 0;
                }
                else if (WasKeyJustPressed(ks, Keys.Enter) || WasPadJustPressed(gp, Buttons.A))
                {
                    if (_highlightedIndex >= 0)
                        SelectedIndex = _highlightedIndex;
                    Close();
                }
                else if (WasKeyJustPressed(ks, Keys.Escape) || WasPadJustPressed(gp, Buttons.B))
                {
                    Close();
                }
            }
            else
            {
                if (WasKeyJustPressed(ks, Keys.Enter) || WasKeyJustPressed(ks, Keys.Space) || WasPadJustPressed(gp, Buttons.A))
                    Open();
                else if (WasKeyJustPressed(ks, Keys.Up) || WasPadJustPressed(gp, Buttons.DPadUp))
                {
                    if (_selectedIndex > 0) SelectedIndex = _selectedIndex - 1;
                }
                else if (WasKeyJustPressed(ks, Keys.Down) || WasPadJustPressed(gp, Buttons.DPadDown))
                {
                    if (_selectedIndex < _options.Count - 1) SelectedIndex = _selectedIndex + 1;
                    else if (_selectedIndex == -1 && _options.Count > 0) SelectedIndex = 0;
                }
            }

            _prevKeyState = ks;
            _prevGamePadState = gp;
        }
    }

    private bool WasKeyJustPressed(KeyboardState current, Keys key)
        => current.IsKeyDown(key) && _prevKeyState.IsKeyUp(key);

    private bool WasPadJustPressed(GamePadState current, Buttons button)
        => current.IsButtonDown(button) && _prevGamePadState.IsButtonUp(button);

    /// <inheritdoc/>
    public override void Draw(SpriteBatch spriteBatch)
    {
        if (!IsVisible || Pixel is null) return;

        float opacity = EffectiveOpacity;
        Color border = _isFocused ? FocusBorderColor : BorderColor;

        // Header background
        spriteBatch.Draw(Pixel, Bounds, HeaderColor * opacity);
        DrawHelper.DrawBorder(Pixel, spriteBatch, Bounds, border * opacity, 1);

        // Selected text
        if (Font is not null && SelectedText.Length > 0)
        {
            var textPos = new Vector2(Bounds.X + TextPadding, Bounds.Y + (Bounds.Height - Font.LineSpacing) / 2f);
            spriteBatch.DrawString(Font, SelectedText, textPos, TextColor * opacity);
        }

        // Arrow indicator
        if (Font is not null)
        {
            string arrow = _isExpanded ? "▲" : "▼";
            Vector2 arrowSize = Font.MeasureString(arrow);
            var arrowPos = new Vector2(Bounds.Right - ArrowWidth, Bounds.Y + (Bounds.Height - arrowSize.Y) / 2f);
            spriteBatch.DrawString(Font, arrow, arrowPos, TextColor * opacity);
        }

        // Overlay panel draws itself (managed by UIOverlayManager),
        // but we still need to draw item highlights here for when it's visible
        if (_isExpanded)
        {
            DrawExpandedList(spriteBatch, opacity);
        }
    }

    private void DrawExpandedList(SpriteBatch spriteBatch, float opacity)
    {
        // Background + border already drawn by Panel (UIOverlayManager calls _overlayPanel.Draw)
        for (int i = 0; i < _itemLabels.Count; i++)
        {
            Rectangle itemBounds = _itemLabels[i].Bounds;

            Color bg;
            if (i == _highlightedIndex)
                bg = HighlightColor;
            else if (i == _selectedIndex)
                bg = SelectedColor;
            else
                bg = Color.Transparent;

            if (bg != Color.Transparent)
                spriteBatch.Draw(Pixel!, itemBounds, bg * opacity);
        }
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

        // Click on the header
        if (Bounds.Contains(args.Position))
        {
            if (_isExpanded) Close(); else Open();
            args.Handled = true;
            return;
        }

        // Click on an overlay item
        if (_isExpanded)
        {
            for (int i = 0; i < _itemLabels.Count; i++)
            {
                if (_itemLabels[i].Bounds.Contains(args.Position))
                {
                    SelectedIndex = i;
                    Close();
                    args.Handled = true;
                    return;
                }
            }

            // Click outside — close without selection change
            Close();
        }
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
        _prevKeyState = Keyboard.GetState();
        _prevGamePadState = GamePad.GetState(PlayerIndex.One);
    }

    /// <inheritdoc/>
    public void OnFocusLost()
    {
        _isFocused = false;
        Close();
    }

    #endregion
}
