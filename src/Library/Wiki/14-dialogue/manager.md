# DialogueManager

**Namespace:** `Alca.MonoGame.Kernel.Dialogue`

`DialogueManager` es el servicio central del sistema de diálogos. Gestiona el avance del script, la evaluación de condiciones para filtrar opciones, y notifica a los suscriptores mediante eventos. Es una `sealed class` que puede usarse de forma standalone o asignarse a `GameWorld.DialogueManager`.

---

## Ciclo de vida

```
StartDialogue(script)
        │
        ▼
    OnStarted
        │
        ▼
    OnLineChanged(línea 0)
        │
        ├── [HasChoices = false]
        │       │
        │       ▼
        │   Advance()  ──────────────┐
        │       │                   │
        │       ▼                   │
        │   OnLineChanged(línea N)   │
        │       │                   │
        │       └───────────────────┘
        │
        └── [HasChoices = true]
                │
                ▼
           SelectChoice(i)
                │
                ▼
           OnChoiceMade(i)
                │
                ▼
           OnLineChanged(línea destino)
                │
               ...
                │
                ▼ (última línea o NextLineIndex = -1)
             OnEnded
```

---

## Propiedades

| Propiedad | Tipo | Descripción |
|---|---|---|
| `IsActive` | `bool` | `true` mientras hay un diálogo en curso |
| `CurrentLine` | `DialogueLine` | Línea actual; solo válida cuando `IsActive = true` |
| `CurrentLineIndex` | `int` | Índice de la línea actual |
| `FilteredChoices` | acceso de solo lectura | Opciones de la línea actual filtradas por `EvaluateCondition` |
| `EvaluateCondition` | `Func<DialogueCondition, bool>?` | Delegado para evaluar condiciones; `null` = todas las opciones visibles |

---

## Eventos

| Evento | Firma | Se dispara cuando |
|---|---|---|
| `OnStarted` | `Action?` | Se llama a `StartDialogue` |
| `OnLineChanged` | `Action<DialogueLine>?` | Cambia la línea activa (incluida la primera) |
| `OnChoiceMade` | `Action<int>?` | El jugador selecciona una opción |
| `OnEnded` | `Action?` | El diálogo termina (por `EndDialogue`, fin del script, o `NextLineIndex = -1`) |

---

## Métodos

| Método | Descripción |
|---|---|
| `StartDialogue(DialogueScript script)` | Inicia la reproducción del script desde la línea 0 |
| `Advance()` | Avanza a la siguiente línea; si la línea actual tiene opciones, no hace nada |
| `SelectChoice(int choiceIndex)` | Selecciona la opción con ese índice (sobre `FilteredChoices`) y salta a `NextLineIndex` |
| `EndDialogue()` | Termina el diálogo inmediatamente disparando `OnEnded` |

---

## Uso básico

```csharp
using Alca.MonoGame.Kernel.Dialogue;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

// --- Initialize / LoadContent ---
private DialogueManager _manager;
private TypewriterEffect _typewriter;
private DialogueScript _introScript;

protected override void Initialize()
{
    base.Initialize();

    _manager    = new DialogueManager();
    _typewriter = new TypewriterEffect(maxTextLength: 512);

    _manager.OnLineChanged += line =>
        _typewriter.SetText(LocalizationManager.Get(line.LocalizationKey));

    _manager.OnEnded += () => _isDialogueOpen = false;
}

protected override void LoadContent()
{
    _introScript = new DialogueScript.Builder()
        .AddLine("narrator", "intro.line1")
        .AddLine("narrator", "intro.line2")
        .AddLine("narrator", "intro.line3")
        .Build();
}

// --- Update ---
public override void Update(GameTime gameTime)
{
    float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
    _typewriter.Advance(dt);

    KeyboardState kb = Keyboard.GetState();
    if (kb.IsKeyDown(Keys.Space) && _previousKb.IsKeyUp(Keys.Space))
    {
        if (!_typewriter.IsComplete)
            _typewriter.CompleteInstantly();
        else if (_manager.IsActive && !_manager.CurrentLine.HasChoices)
            _manager.Advance();
    }

    _previousKb = kb;
}
```

---

## Condiciones dinámicas

El delegado `EvaluateCondition` permite filtrar opciones en función del estado de juego:

```csharp
// Suponiendo un diccionario de flags del juego
Dictionary<string, string> _gameFlags = new()
{
    { "has_key",     "true"  },
    { "skill_steal", "false" },
};

_manager.EvaluateCondition = condition =>
{
    if (condition.IsEmpty) return true; // sin condición = siempre visible
    return _gameFlags.TryGetValue(condition.Key, out string? val) && val == condition.Value;
};
```

Las opciones que no superan la evaluación no aparecen en `FilteredChoices`.

---

## Integrar con TypewriterEffect

```csharp
// Suscribirse en Initialize (una sola vez)
_manager.OnLineChanged += line =>
{
    string text = LocalizationManager.Get(line.LocalizationKey);
    _typewriter.SetText(text);
};

// En Update, cuando el jugador pulsa la tecla de avance:
if (inputPressed)
{
    if (!_typewriter.IsComplete)
        _typewriter.CompleteInstantly(); // revelar todo el texto
    else if (!_manager.CurrentLine.HasChoices)
        _manager.Advance();              // pasar a la siguiente línea
}
```

---

## Integrar con GameWorld

```csharp
protected override GameWorld? CreateWorld()
{
    var manager = new DialogueManager
    {
        EvaluateCondition = condition =>
            GameFlags.Check(condition.Key, condition.Value)
    };

    manager.OnEnded += () => World!.IsPaused = false;

    return new GameWorld
    {
        DialogueManager = manager
    };
}

// Desde cualquier GameBehaviour:
World.DialogueManager!.StartDialogue(_questScript);
```

---

## Ver también

- [Visión general →](overview.md)
- [TypewriterEffect →](typewriter.md)
- [DialogueBoxBehaviour y ChoicesPanelBehaviour →](choices.md)
