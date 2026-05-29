# TypewriterEffect

**Namespace:** `Alca.MonoGame.Kernel.Dialogue`

`TypewriterEffect` revela texto carácter a carácter emulando el efecto de máquina de escribir. Usa un buffer `char[]` pre-asignado en el constructor para que el bucle de juego no produzca asignaciones en el heap salvo una por cada nuevo carácter revelado.

---

## Constructor

```csharp
TypewriterEffect(int maxTextLength = 1024)
```

Pre-asigna el buffer interno con capacidad para `maxTextLength` caracteres. Ajusta este valor al texto más largo que pueda aparecer en el juego; un valor generoso (256–1024) no perjudica al rendimiento.

---

## Propiedades

| Propiedad | Tipo | Descripción |
|---|---|---|
| `IsComplete` | `bool` | `true` cuando todos los caracteres están visibles |
| `CurrentText` | `string` | Texto actualmente visible; **1 asignación por nuevo carácter revelado, cero por frame cuando `IsComplete`** |
| `CharsPerSecond` | `float` | Velocidad de revelado (default: `30`) |
| `OnComplete` | `Action?` | Disparado cuando se revela el último carácter |

---

## Métodos

| Método | Descripción |
|---|---|
| `SetText(string text)` | Copia los caracteres al buffer interno y reinicia el progreso; **no almacena la referencia al string** |
| `Advance(float deltaTime)` | Avanza el contador de caracteres visibles según `CharsPerSecond` |
| `CompleteInstantly()` | Revela todo el texto de inmediato; útil al pulsar una tecla de aceleración |
| `Reset()` | Vacía el texto visible sin liberar el buffer |

---

## Modelo mental

| Estado | `IsComplete` | Coste por frame en `Advance` |
|---|---|---|
| Revelando caracteres | `false` | Actualiza un contador `float`; 1 `string` alloc por nuevo carácter |
| Texto completo | `true` | Solo comprueba el flag; **cero asignaciones** |
| Sin texto (`Reset`) | `true` | Cero asignaciones |

> El único momento en que se asigna memoria es cuando el contador de caracteres avanza un entero: se crea un nuevo `string` con `new string(_buffer, 0, visibleCount)`. Una vez que `IsComplete = true`, no hay más asignaciones hasta la siguiente llamada a `SetText`.

---

## Uso básico

```csharp
using Alca.MonoGame.Kernel.Dialogue;
using Microsoft.Xna.Framework;

// --- Initialize ---
private TypewriterEffect _typewriter;
private SpriteFont _font;

protected override void Initialize()
{
    base.Initialize();
    _typewriter = new TypewriterEffect(maxTextLength: 256);
    _typewriter.CharsPerSecond = 40f; // velocidad personalizada
}

protected override void LoadContent()
{
    _font = Content.Load<SpriteFont>("Fonts/DialogueFont");
    _typewriter.SetText("¡Bienvenido al pueblo, viajero!");
}

// --- Update ---
public override void Update(GameTime gameTime)
{
    float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
    _typewriter.Advance(dt);
}

// --- Draw ---
public override void Draw(GameTime gameTime)
{
    SpriteBatch.Begin();
    SpriteBatch.DrawString(_font, _typewriter.CurrentText,
        position: new Vector2(64, 400), color: Color.White);
    SpriteBatch.End();
}
```

---

## Integrar con DialogueManager

```csharp
// En Initialize — suscripción única
_manager.OnLineChanged += line =>
{
    string translated = LocalizationManager.Get(line.LocalizationKey);
    _typewriter.SetText(translated);
};

// Callback opcional cuando el texto termina de revelarse
_typewriter.OnComplete += () =>
{
    // Mostrar indicador de "pulsa para continuar"
    _continueIndicator.IsVisible = true;
};
```

---

## Revelar instantáneamente al pulsar una tecla

```csharp
// En Update
KeyboardState kb = Keyboard.GetState();
bool confirmPressed = kb.IsKeyDown(Keys.Space) && _previousKb.IsKeyUp(Keys.Space)
                   || kb.IsKeyDown(Keys.Enter) && _previousKb.IsKeyUp(Keys.Enter);

if (confirmPressed && _manager.IsActive)
{
    if (!_typewriter.IsComplete)
    {
        // Primera pulsación: revelar el resto del texto al instante
        _typewriter.CompleteInstantly();
    }
    else if (!_manager.CurrentLine.HasChoices)
    {
        // Segunda pulsación: pasar a la siguiente línea
        _manager.Advance();
    }
}

_previousKb = kb;
```

---

## Notas de rendimiento

- **1 `string` alloc por carácter revelado**: `CurrentText` reconstruye el string cada vez que el contador de enteros avanza. A 30 caracteres/s el impacto es mínimo.
- **Cero alloc por frame cuando `IsComplete = true`**: el método `Advance` solo actualiza un acumulador `float`; no se crea ningún objeto.
- **Sin referencias externas al texto original**: `SetText` copia los caracteres al buffer interno, por lo que el string original puede ser recogido por el GC sin afectar al efecto.
- **Buffer reutilizado**: llamar `SetText` repetidamente no asigna nuevos buffers; el mismo `char[]` se reutiliza para todos los textos mientras quepan en `maxTextLength`. Si el texto es más largo que el buffer, se trunca.

---

## Ver también

- [DialogueManager →](manager.md)
- [Visión general →](overview.md)
