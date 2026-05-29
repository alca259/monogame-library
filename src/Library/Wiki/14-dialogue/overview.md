# Sistema de Diálogo — Visión General

**Namespace:** `Alca.MonoGame.Kernel.Dialogue`

El sistema de diálogos permite crear conversaciones ramificadas con efectos de escritura a máquina, opciones condicionales y soporte completo para localización multi-idioma. Está diseñado bajo los principios zero-alloc del kernel: ninguna clase se instancia en `Update` o `Draw`.

---

## Arquitectura

```
DialogueScript (datos)
    │
    │  líneas y opciones inmutables
    ▼
DialogueManager (lógica)
    │  gestiona avance, ramificación y condiciones
    │
    ├──▶ Events: OnStarted / OnLineChanged / OnChoiceMade / OnEnded
    │
    ├──▶ TypewriterEffect (presentación de texto)
    │        └── char[] buffer pre-asignado → CurrentText
    │
    └──▶ [opcional] GameBehaviour presentación
              ├── DialogueBoxBehaviour   (dibuja caja + texto)
              └── ChoicesPanelBehaviour  (dibuja lista de opciones)
```

El flujo de datos es unidireccional: el script es inmutable, el manager es el único que modifica estado, y los behaviours solo leen y renderizan.

---

## Tipos de datos

| Tipo | Categoría | Descripción |
|---|---|---|
| `DialogueLine` | `readonly struct` | Una línea de diálogo: speaker, clave de localización, retrato y opciones |
| `DialogueChoice` | `readonly struct` | Una opción seleccionable por el jugador: clave, índice de destino y condición |
| `DialogueCondition` | `readonly struct` | Condición evaluable externamente para filtrar opciones; `None` = siempre visible |
| `DialogueScript` | `sealed class` | Colección inmutable de `DialogueLine[]`; construida con `DialogueScript.Builder` |
| `DialogueManager` | `sealed class` | Servicio que avanza el script, filtra opciones y dispara eventos |
| `TypewriterEffect` | `sealed class` | Revela el texto carácter a carácter con buffer pre-asignado |
| `DialogueBoxBehaviour` | `sealed GameBehaviour` | Dibuja el fondo de la caja y el texto del diálogo |
| `ChoicesPanelBehaviour` | `sealed GameBehaviour` | Dibuja la lista de opciones filtradas |

---

## Quickstart — 3 líneas sin opciones

```csharp
using Alca.MonoGame.Kernel.Dialogue;
using Microsoft.Xna.Framework.Input;

// 1. Construir el script (una vez, en LoadContent o Initialize)
DialogueScript script = new DialogueScript.Builder()
    .AddLine("npc_mayor", "npc.mayor.greeting")
    .AddLine("player",    "player.response")
    .AddLine("npc_mayor", "npc.mayor.farewell")
    .Build();

// 2. Crear el manager
DialogueManager manager = new DialogueManager();

// 3. Conectar el efecto de escritura
TypewriterEffect typewriter = new TypewriterEffect(maxTextLength: 256);
manager.OnLineChanged += line => typewriter.SetText(
    LocalizationManager.Get(line.LocalizationKey));

// 4. Iniciar el diálogo (p. ej. al pulsar la tecla de interacción)
manager.StartDialogue(script);

// --- En Update ---
typewriter.Advance((float)gameTime.ElapsedGameTime.TotalSeconds);

if (Keyboard.GetState().IsKeyDown(Keys.Space))
{
    if (typewriter.IsComplete)
        manager.Advance();
    else
        typewriter.CompleteInstantly();
}
```

---

## Quickstart — con opciones

```csharp
DialogueScript script = new DialogueScript.Builder()
    .AddLine("npc",    "npc.question")           // índice 0
    .AddLine("player", "player.yes_branch")      // índice 1  (rama "Sí")
    .AddLine("npc",    "npc.good_outcome")        // índice 2
    .AddLine("player", "player.no_branch")        // índice 3  (rama "No")
    .AddLine("npc",    "npc.bad_outcome")         // índice 4
    .WithChoice("choice.yes", nextLineIndex: 1)   // en línea 0: opción A
    .WithChoice("choice.no",  nextLineIndex: 3)   // en línea 0: opción B
    .Build();
// Nota: las elecciones se asocian a la última línea añadida antes de llamar Build().
// Ver script.md para el uso completo con AddLine + WithChoice encadenados.
```

Cuando `manager.CurrentLine.HasChoices` es `true`, el jugador debe llamar `manager.SelectChoice(index)` en lugar de `Advance()`.

---

## Integración con GameWorld

`GameWorld` expone una propiedad opcional `DialogueManager`:

```csharp
protected override GameWorld? CreateWorld()
{
    return new GameWorld
    {
        DialogueManager = new DialogueManager
        {
            EvaluateCondition = condition =>
                GameFlags.Check(condition.Key, condition.Value)
        }
    };
}

// Acceso en cualquier GameBehaviour:
World.DialogueManager?.StartDialogue(myScript);
```

---

## Ver también

- [DialogueScript y tipos de datos →](script.md)
- [DialogueManager →](manager.md)
- [TypewriterEffect →](typewriter.md)
- [DialogueBoxBehaviour y ChoicesPanelBehaviour →](choices.md)
