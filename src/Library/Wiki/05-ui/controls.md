# Controles de UI

**Namespace:** `Alca.MonoGame.Kernel.UI.Controls`

El framework incluye 16 controles listos para usar, todos heredando de `UIElement`. Los controles interactivos implementan `IUIInteractable` y/o `IFocusable`.

---

## Button

Botón presionable con animación hover/press y soporte de foco.

```csharp
public sealed class Button : UIElement, IUIInteractable, IFocusable
{
    public Button(SpriteFont? font, string text, Texture2D? backgroundTexture = null)

    // Colores por estado
    public Color NormalColor   { get; set; }
    public Color HoveredColor  { get; set; }
    public Color PressedColor  { get; set; }
    public Color DisabledColor { get; set; }
    public Color NormalTextColor   { get; set; }
    public Color HoveredTextColor  { get; set; }
    public Color PressedTextColor  { get; set; }
    public Color DisabledTextColor { get; set; }

    // Apariencia
    public Vector2?   FixedSize  { get; set; }
    public HAlign     HAlign     { get; set; }
    public Texture2D? BackgroundPixel { get; set; }
    public Texture2D? Texture    { get; set; }

    // Evento
    public event Action? Clicked;
}
```

```csharp
var btn = new Button(_font, "Jugar")
{
    NormalColor  = Color.DarkBlue,
    HoveredColor = Color.Blue,
    PressedColor = Color.Navy,
    TabIndex     = 0
};
btn.Clicked += () => Core.SceneManager.RequestChange(new GameplayScene());
```

---

## Label

Texto estático con alineación horizontal/vertical y ajuste de línea.

```csharp
public sealed class Label : UIElement
{
    public string     Text      { get; set; }
    public SpriteFont? Font     { get; set; }
    public Color      Color     { get; set; }
    public HAlign     HAlign    { get; set; }
    public VAlign     VAlign    { get; set; }
    public bool       WrapText  { get; set; }
}
```

---

## TextBox

Campo de texto de una línea con teclado del sistema.

```csharp
public sealed class TextBox : TextBoxBase, IUIInteractable, IFocusable
{
    public TextBox(SpriteFont? font, Texture2D? backgroundTexture = null, GameWindow? window = null)
}
```

Hereda las propiedades de `TextBoxBase`: `Text`, `Placeholder`, `MaxLength`, `BorderColor`, `FocusBorderColor`, `CursorColor`, `SelectionColor`.

---

## TextArea

Campo de texto multilínea con `MaxLines` y ajuste de línea.

```csharp
public sealed class TextArea : TextBoxBase, IUIInteractable, IFocusable
{
    public TextArea(SpriteFont? font, Texture2D? backgroundTexture = null, GameWindow? window = null)
    public int  MaxLines  { get; set; }
    public bool WordWrap  { get; set; }
}
```

---

## NumericBox

TextBox especializado para entrada numérica con validación de rango.

```csharp
public sealed class NumericBox : TextBoxBase, IUIInteractable, IFocusable
{
    public NumericBox(SpriteFont? font, Texture2D? backgroundTexture = null, GameWindow? window = null)
    public bool  IsInt         { get; set; }   // true = enteros, false = decimales
    public float MinValue      { get; set; }
    public float MaxValue      { get; set; }
    public float Step          { get; set; }
    public int   DecimalPlaces { get; set; }
    public int   IntValue      { get; }
    public float FloatValue    { get; }
}
```

---

## Slider

Control deslizante horizontal o vertical.

```csharp
public sealed class Slider : UIElement, IUIInteractable, IFocusable
{
    public Slider(Texture2D? pixel)
    public float       MinValue    { get; set; }
    public float       MaxValue    { get; set; }
    public float       Step        { get; set; }
    public Orientation Orientation { get; set; }
    public int         TrackThickness { get; set; }
    public int         ThumbSize   { get; set; }
    public Color       TrackColor  { get; set; }
    public Color       FillColor   { get; set; }
    public Color       ThumbColor  { get; set; }
    public Color       FocusBorderColor { get; set; }
    public float       Value       { get; set; }
    public event Action<float>? ValueChanged;
}
```

```csharp
var vol = new Slider(_pixel) { MinValue = 0, MaxValue = 1, Value = 0.8f };
vol.ValueChanged += v => Core.Audio.Master.Volume = v;
```

---

## Checkbox

Casilla de verificación con etiqueta.

```csharp
public sealed class Checkbox : UIElement, IUIInteractable, IFocusable
{
    public Checkbox(SpriteFont? font, string label)
    public int       BoxSize      { get; set; }
    public Color     BoxColor     { get; set; }
    public Color     CheckColor   { get; set; }
    public Color     LabelColor   { get; set; }
    public Texture2D? BoxTexture  { get; set; }
    public Texture2D? CheckTexture{ get; set; }
    public Texture2D? Pixel       { get; set; }
    public bool      IsChecked    { get; set; }
    public event Action<bool>? CheckedChanged;
    public void Toggle();
}
```

---

## RadioButton y RadioGroup

Botones de opción exclusivos agrupados por `RadioGroup`.

```csharp
// Crear el grupo
var group = new RadioGroup();
group.SelectionChanged += index => Console.WriteLine($"Opción {index}");

// Crear botones asociados al grupo
var rb1 = new RadioButton(_font, _pixel, "Fácil",   group);
var rb2 = new RadioButton(_font, _pixel, "Normal",  group) { IsSelected = true };
var rb3 = new RadioButton(_font, _pixel, "Difícil", group);
```

`RadioGroup` — propiedades y métodos:

| Miembro | Descripción |
|---|---|
| `SelectedButton` | Botón actualmente seleccionado |
| `SelectedIndex` | Índice del botón seleccionado (basado en el orden de registro) |
| `Count` | Número de botones registrados |
| `Select(button)` | Selecciona un botón concreto |
| `SelectAt(index)` | Selecciona por índice |
| `ClearSelection()` | Deselecciona todo |
| `event SelectionChanged` | `Action<int>` disparado al cambiar la selección |

---

## Dropdown

Lista desplegable modal que usa el `UIOverlayManager`.

```csharp
public sealed class Dropdown : UIElement, IUIInteractable, IFocusable
{
    public Dropdown(UIOverlayManager overlayManager)
    public SpriteFont? Font             { get; set; }
    public Texture2D?  Pixel            { get; set; }
    public int         ItemHeight       { get; set; }
    public Color       HeaderColor      { get; set; }
    public Color       ListBackgroundColor { get; set; }
    public Color       HighlightColor   { get; set; }
    public Color       TextColor        { get; set; }
    public Color       BorderColor      { get; set; }
    public Color       FocusBorderColor { get; set; }
    public int         SelectedIndex    { get; set; }
    public string      SelectedText     { get; }
    public bool        IsExpanded       { get; }
    public event Action<int>? SelectionChanged;
    public void AddItem(string text);
    public void ClearItems();
    public void Open();
    public void Close();
}
```

```csharp
var dd = new Dropdown(UIRoot!.OverlayManager!);
dd.AddItem("Español");
dd.AddItem("English");
dd.AddItem("Français");
dd.SelectionChanged += i => Core.Localization.LoadLanguage(i == 0 ? "es" : "en");
```

---

## ProgressBar

Barra de progreso con gradiente de color opcional.

```csharp
public sealed class ProgressBar : UIElement
{
    public float       Value           { get; set; }  // 0–1
    public Color       FillColor       { get; set; }
    public Color       BackgroundColor { get; set; }
    public Texture2D?  Pixel           { get; set; }
    public Orientation Orientation     { get; set; }
    public bool        ColorGradient   { get; set; }
    public Color       LowColor        { get; set; }
    public Color       HighColor       { get; set; }
}
```

Cuando `ColorGradient = true`, la barra interpola entre `LowColor` y `HighColor` según `Value`.

---

## Panel

Contenedor visual con fondo y borde opcionales. Soporta nine-slice.

```csharp
public sealed class Panel : UIContainer
{
    public Color      BackgroundColor   { get; set; }
    public Texture2D? BackgroundTexture { get; set; }
    public Color      BorderColor       { get; set; }
    public int        BorderThickness   { get; set; }
    public Texture2D? NineSliceTexture  { get; set; }
    public Rectangle  NineSliceBorder   { get; set; }
}
```

---

## ScrollView

Contenedor con desplazamiento vertical u horizontal.

```csharp
public sealed class ScrollView : UIContainer
{
    public ScrollView(GraphicsDevice graphicsDevice)
    public Vector2    ContentSize  { get; set; }
    public Vector2    ScrollOffset { get; set; }
    public Vector2?   FixedSize    { get; set; }
    public Texture2D? Pixel        { get; set; }
    public Color      BackColor    { get; set; }
    public Color      BorderColor  { get; set; }
    public float      ScrollSpeed  { get; set; }
    public void ScrollBy(Vector2 delta);
}
```

---

## ColorPickerRGB

Selector de color con tres sliders independientes (R, G, B).

```csharp
public sealed class ColorPickerRGB : UIContainer, IDisposable
{
    public ColorPickerRGB(GraphicsDevice? gd, SpriteFont? font, Texture2D? pixel)
    public Texture2D? Pixel        { get; set; }
    public Color      BorderColor  { get; set; }
    public Color      SelectedColor { get; set; }
    public event Action<Color>? ColorChanged;   // dispara en cada cambio
    public event Action<Color>? ColorCommitted; // dispara al soltar el slider
    public void Dispose();
}
```

---

## Tooltip

Etiqueta flotante que aparece junto al cursor.

```csharp
public sealed class Tooltip : UIElement
{
    public string     Text            { get; set; }
    public SpriteFont? Font           { get; set; }
    public Color      BackgroundColor { get; set; }
    public Color      TextColor       { get; set; }
    public Texture2D? Pixel           { get; set; }
    public Rectangle  ScreenBounds    { get; set; }   // para clampear dentro de pantalla
    public void Show();
    public void Hide();
}
```

---

## UISprite

Imagen estática con región de origen opcional.

```csharp
public sealed class UISprite : UIElement
{
    public Texture2D?  Texture    { get; set; }
    public Rectangle?  SourceRect { get; set; }
    public Color       Color      { get; set; }
    public SpriteDrawMode DrawMode { get; set; }
}
```

---

## Ejemplo: pantalla de opciones completa

```csharp
protected override void InitializeUI()
{
    var grid = new GridLayout();
    grid.ColumnDefinitions.Add(GridTrack.Auto());
    grid.ColumnDefinitions.Add(GridTrack.Star());
    for (int i = 0; i < 5; i++)
        grid.RowDefinitions.Add(GridTrack.Fixed(44));

    // Fila 0: volumen
    var lblVol = new Label { Text = "Volumen:", Font = _font, Color = Color.White };
    var sldVol = new Slider(_pixel) { MinValue = 0, MaxValue = 1, Value = 0.8f, TabIndex = 0 };
    sldVol.ValueChanged += v => Core.Audio.Master.Volume = v;
    grid.Add(lblVol); grid.SetCell(lblVol, 0, 0);
    grid.Add(sldVol); grid.SetCell(sldVol, 0, 1);

    // Fila 1: pantalla completa
    var lblFs = new Label { Text = "Pantalla completa:", Font = _font, Color = Color.White };
    var chkFs = new Checkbox(_font, "") { IsChecked = false, TabIndex = 1 };
    chkFs.CheckedChanged += v => Core.Platform.SetFullscreen(v);
    grid.Add(lblFs); grid.SetCell(lblFs, 1, 0);
    grid.Add(chkFs); grid.SetCell(chkFs, 1, 1);

    // Fila 2: idioma
    var lblLang = new Label { Text = "Idioma:", Font = _font, Color = Color.White };
    var ddLang  = new Dropdown(UIRoot!.OverlayManager!) { TabIndex = 2 };
    ddLang.AddItem("Español"); ddLang.AddItem("English");
    grid.Add(lblLang); grid.SetCell(lblLang, 2, 0);
    grid.Add(ddLang);  grid.SetCell(ddLang,  2, 1);

    // Fila 4: botón Aceptar
    var btnOk = new Button(_font, "Aceptar") { TabIndex = 3 };
    btnOk.Clicked += () => Core.SceneManager.PopScene();
    grid.Add(btnOk); grid.SetCell(btnOk, 4, 0, colSpan: 2);

    UIRoot!.Add(grid);
}
```

---

## Ver también

- [Layouts →](layout.md)
- [Foco →](focus.md)
- [Interacción →](interaction.md)
