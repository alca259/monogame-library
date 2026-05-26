# Localización

**Namespace:** `Alca.MonoGame.Kernel.Localization`

`LocalizationManager` carga diccionarios de strings en JSON y permite cambiar de idioma en tiempo de ejecución. Disponible como `Core.Localization`.

---

## LocalizationManager

Implementa `IStringLocalizer` de `Microsoft.Extensions.Localization`.

### Propiedades

| Propiedad | Tipo | Descripción |
|---|---|---|
| `CurrentCulture` | `string` | Idioma activo (ej. `"es"`, `"en"`) |

### Eventos

| Evento | Descripción |
|---|---|
| `CultureChanged` | Disparado tras cargar un nuevo idioma |

### Métodos

| Método | Descripción |
|---|---|
| `LoadLanguage(culture)` | Carga el archivo JSON del idioma indicado |
| `this[name]` | Devuelve un `LocalizedString` por clave |
| `this[name, args]` | Devuelve un `LocalizedString` con formato |
| `GetAllStrings(includeParent)` | Enumera todas las strings del idioma activo |

### Extensión

```csharp
// Shorthand para obtener el string como string nativo
string text = Core.Localization.Get("ui.btn.play");
string fmt  = Core.Localization.Get("ui.lbl.score", score);
```

---

## Formato del JSON de strings

Los archivos de idioma deben colocarse en la carpeta de contenido del juego bajo una ruta estándar (ej. `Content/Localization/es.json`):

```json
{
  "ui.btn.play":    "Jugar",
  "ui.btn.options": "Opciones",
  "ui.btn.quit":    "Salir",
  "ui.lbl.score":   "Puntuación: {0}",
  "ui.lbl.level":   "Nivel {0} — {1} jugadores",
  "menu.welcome":   "¡Bienvenido, {0}!"
}
```

---

## Ejemplo: cambio de idioma en tiempo de ejecución

```csharp
public sealed class LanguageDropdown : UIContainer
{
    public LanguageDropdown(UIOverlayManager overlay, SpriteFont font, Texture2D pixel)
    {
        var dd = new Dropdown(overlay) { Font = font, Pixel = pixel };
        dd.AddItem("Español");
        dd.AddItem("English");
        dd.AddItem("Français");
        dd.SelectedIndex = CurrentLanguageIndex();

        dd.SelectionChanged += index =>
        {
            string culture = index switch { 0 => "es", 1 => "en", _ => "fr" };
            Core.Localization.LoadLanguage(culture);
        };

        Add(dd);
    }

    private int CurrentLanguageIndex() => Core.Localization.CurrentCulture switch
    {
        "es" => 0, "en" => 1, _ => 2
    };
}
```

---

## Ejemplo: etiqueta reactiva al cambio de idioma

```csharp
public sealed class LocalizedLabel : Label
{
    private readonly string _key;

    public LocalizedLabel(string key, SpriteFont font)
    {
        _key = key;
        Font = font;
        UpdateText();
        Core.Localization.CultureChanged += UpdateText;
    }

    private void UpdateText() =>
        Text = Core.Localization.Get(_key);
}
```

---

## Notas

- `LoadLanguage` es una operación síncrona; si el archivo es grande, usa `AsyncContentLoader` para cargarlo en background antes de llamar a `LoadLanguage`.
- Si una clave no existe en el JSON, `LocalizationManager` devuelve la clave como fallback (no lanza excepción).
- El formato usa `string.Format` estándar — `{0}`, `{1}`, etc.

---

## Ver también

- [AsyncContentLoader →](async-content.md)
- [Core →](../01-core/core.md)
