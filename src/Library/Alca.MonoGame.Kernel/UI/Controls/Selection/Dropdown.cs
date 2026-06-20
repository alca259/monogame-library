using Alca.MonoGame.Kernel.Graphics;
using Alca.MonoGame.Kernel.UI.Core;
using Alca.MonoGame.Kernel.UI.Focus;
using Alca.MonoGame.Kernel.UI.Input;
using Alca.MonoGame.Kernel.UI.Interaction;
using Alca.MonoGame.Kernel.UI.Overlays;

namespace Alca.MonoGame.Kernel.UI.Controls.Selection;

/// <summary>A collapsed header that expands an overlay item list when clicked or activated via keyboard.</summary>
public sealed class Dropdown : UIElement, IUIInteractable, IFocusable
{
    #region Constants

    private const int DefaultItemHeight = 28;
    private const int ArrowWidth = 20;
    private const int TextPadding = 6;

    #endregion

    #region Inner overlay element

    private sealed class DropdownOverlay : UIElement
    {
        private readonly Dropdown _owner;

        internal DropdownOverlay(Dropdown owner) => _owner = owner;

        public override void Measure(Vector2 availableSize) { }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!IsVisible || _owner.Pixel is null || _owner.Font is null) return;

            float opacity = _owner.EffectiveOpacity;

            // Background
            spriteBatch.Draw(_owner.Pixel, Bounds, _owner.ListBackgroundColor * opacity);

            // Items: highlight background then text, drawn in correct Z-order
            for (int i = 0; i < _owner._options.Count; i++)
            {
                Rectangle itemBounds = new(
                    Bounds.X,
                    Bounds.Y + i * _owner.ItemHeight,
                    Bounds.Width,
                    _owner.ItemHeight);

                if (i == _owner._highlightedIndex)
                    spriteBatch.Draw(_owner.Pixel, itemBounds, _owner.HighlightColor * opacity);
                else if (i == _owner._selectedIndex)
                    spriteBatch.Draw(_owner.Pixel, itemBounds, _owner.SelectedColor * opacity);

                var textPos = new Vector2(
                    itemBounds.X + TextPadding,
                    itemBounds.Y + (itemBounds.Height - _owner.Font.LineSpacing) / 2f);
                spriteBatch.DrawString(_owner.Font, _owner._options[i], textPos, _owner.TextColor * opacity);
            }

            // Border drawn last so it is not overwritten by item backgrounds
            DrawHelper.DrawBorder(_owner.Pixel, spriteBatch, Bounds, _owner.BorderColor * opacity, 1);
        }
    }

    #endregion

    #region Fields

    private readonly List<string> _options = new(8);
    private readonly UIOverlayManager _overlayManager;
    private readonly DropdownOverlay _overlay;

    private bool _isExpanded;
    private bool _isHovered;
    private bool _isFocused;
    private int _selectedIndex = -1;
    private int _highlightedIndex = -1;
    private Rectangle _listBounds;
    private int _screenHeight;

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

    /// <summary>Optional nine-slice texture drawn as the collapsed header background. When set, replaces the solid <see cref="HeaderColor"/> fill.</summary>
    public Texture2D? NineSliceTexture { get; set; }

    /// <summary>Border insets used to divide <see cref="NineSliceTexture"/> into a 3×3 grid.</summary>
    public NineSliceBorderData NineSliceBorder { get; set; } = NineSliceBorderData.Uniform(8);

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
        _overlay = new DropdownOverlay(this) { IsVisible = false, IsEnabled = false };
    }

    #endregion

    #region Items

    /// <summary>Appends a string option to the list.</summary>
    public void AddItem(string text) => _options.Add(text);

    /// <summary>Removes all options and resets selection.</summary>
    public void ClearItems()
    {
        _options.Clear();
        _selectedIndex = -1;
        _highlightedIndex = -1;
    }

    #endregion

    #region Open / Close

    /// <summary>Expands the item list, registering the overlay with the UIOverlayManager.</summary>
    public void Open()
    {
        if (_isExpanded || _options.Count == 0) return;
        _isExpanded = true;
        _highlightedIndex = _selectedIndex >= 0 ? _selectedIndex : 0;
        RebuildListBounds();
        _overlay.IsVisible = true;
        _overlay.IsEnabled = true;
        _overlayManager.Show(_overlay);
    }

    /// <summary>Collapses the item list, removing the overlay from the UIOverlayManager.</summary>
    public void Close()
    {
        if (!_isExpanded) return;
        _isExpanded = false;
        _overlay.IsVisible = false;
        _overlay.IsEnabled = false;
        _overlayManager.Hide(_overlay);
    }

    private void RebuildListBounds()
    {
        int listHeight = _options.Count * ItemHeight;
        bool flipUp = _screenHeight > 0 && (Bounds.Bottom + listHeight) > _screenHeight;

        _listBounds = flipUp
            ? new Rectangle(Bounds.X, Bounds.Y - listHeight, Bounds.Width, listHeight)
            : new Rectangle(Bounds.X, Bounds.Bottom, Bounds.Width, listHeight);

        _overlay.Arrange(_listBounds);
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

        var input = UIInputContext.Current!;

        if (_isExpanded)
        {
            if (input.PointerPosition is not null)
            {
                Point mousePos = input.PointerPosition.Value;
                bool justClicked = input.WasPointerButtonJustPressed;

                if (justClicked)
                {
                    bool hitItem = false;
                    for (int i = 0; i < _options.Count; i++)
                    {
                        Rectangle itemBounds = new(
                            _listBounds.X,
                            _listBounds.Y + i * ItemHeight,
                            _listBounds.Width,
                            ItemHeight);

                        if (itemBounds.Contains(mousePos))
                        {
                            SelectedIndex = i;
                            Close();
                            hitItem = true;
                            break;
                        }
                    }

                    // Click outside both header and list closes the dropdown
                    if (!hitItem && !Bounds.Contains(mousePos))
                        Close();
                }
                else
                {
                    _highlightedIndex = -1;
                    for (int i = 0; i < _options.Count; i++)
                    {
                        Rectangle itemBounds = new(
                            _listBounds.X,
                            _listBounds.Y + i * ItemHeight,
                            _listBounds.Width,
                            ItemHeight);

                        if (itemBounds.Contains(mousePos))
                        {
                            _highlightedIndex = i;
                            break;
                        }
                    }
                }
            }
        }

        if (_isFocused)
        {
            if (_isExpanded)
            {
                if (input.MoveUp?.IsPressed == true)
                {
                    if (_highlightedIndex > 0) _highlightedIndex--;
                    else _highlightedIndex = _options.Count - 1;
                }
                else if (input.MoveDown?.IsPressed == true)
                {
                    if (_highlightedIndex < _options.Count - 1) _highlightedIndex++;
                    else _highlightedIndex = 0;
                }
                else if (input.Confirm?.IsPressed == true)
                {
                    if (_highlightedIndex >= 0)
                        SelectedIndex = _highlightedIndex;
                    Close();
                }
                else if (input.Cancel?.IsPressed == true)
                {
                    Close();
                }
            }
            else
            {
                if (input.Confirm?.IsPressed == true)
                    Open();
                else if (input.MoveUp?.IsPressed == true)
                {
                    if (_selectedIndex > 0) SelectedIndex = _selectedIndex - 1;
                }
                else if (input.MoveDown?.IsPressed == true)
                {
                    if (_selectedIndex < _options.Count - 1) SelectedIndex = _selectedIndex + 1;
                    else if (_selectedIndex == -1 && _options.Count > 0) SelectedIndex = 0;
                }
            }
        }
    }

    /// <inheritdoc/>
    public override void Draw(SpriteBatch spriteBatch)
    {
        if (!IsVisible || Pixel is null) return;

        float opacity = EffectiveOpacity;
        Color border = _isFocused ? FocusBorderColor : BorderColor;

        // Header background
        if (NineSliceTexture is not null)
            DrawHelper.DrawNineSlice(spriteBatch, NineSliceTexture, Bounds, NineSliceBorder, Color.White * opacity);
        else
        {
            spriteBatch.Draw(Pixel, Bounds, HeaderColor * opacity);
            DrawHelper.DrawBorder(Pixel, spriteBatch, Bounds, border * opacity, 1);
        }

        // Selected text
        if (Font is not null && SelectedText.Length > 0)
        {
            var textPos = new Vector2(Bounds.X + TextPadding, Bounds.Y + (Bounds.Height - Font.LineSpacing) / 2f);
            spriteBatch.DrawString(Font, SelectedText, textPos, TextColor * opacity);
        }

        // Arrow indicator using ASCII characters (universally available in SpriteFont)
        if (Font is not null)
        {
            string arrow = _isExpanded ? "^" : "v";
            Vector2 arrowSize = Font.MeasureString(arrow);
            var arrowPos = new Vector2(Bounds.Right - ArrowWidth, Bounds.Y + (Bounds.Height - arrowSize.Y) / 2f);
            spriteBatch.DrawString(Font, arrow, arrowPos, TextColor * opacity);
        }

        // Expanded list is drawn by DropdownOverlay via UIOverlayManager (correct Z-order)
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

        if (Bounds.Contains(args.Position))
        {
            if (_isExpanded) Close(); else Open();
            args.Handled = true;
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
    }

    /// <inheritdoc/>
    public void OnFocusLost()
    {
        _isFocused = false;
        Close();
    }

    #endregion
}
