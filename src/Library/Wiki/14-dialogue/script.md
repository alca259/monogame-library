# DialogueScript

**Namespace:** `Alca.MonoGame.Kernel.Dialogue`

`DialogueScript` es la representación inmutable de un guion de diálogo. Encapsula un array de `DialogueLine` construido a través de la API fluent `DialogueScript.Builder`. Una vez construido, el script no puede modificarse y es seguro compartirlo entre múltiples managers.

---

## DialogueLine

Cada línea del guion. Es un `readonly struct`, por lo que acceder a ella no produce asignaciones en el heap.

| Propiedad | Tipo | Descripción |
|---|---|---|
| `SpeakerId` | `string` | Identificador del personaje que habla (p. ej. `"npc_mayor"`) |
| `LocalizationKey` | `string` | Clave de localización para obtener el texto traducido |
| `PortraitKey` | `string` | Clave de textura del retrato del personaje (puede ser vacía) |
| `Choices` | `DialogueChoice[]` | Opciones disponibles en esta línea (array vacío si no hay opciones) |
| `HasChoices` | `bool` | `true` cuando `Choices.Length > 0`; shortcut de conveniencia |

---

## DialogueChoice

Una opción seleccionable. Es un `readonly struct`.

| Propiedad | Tipo | Descripción |
|---|---|---|
| `LocalizationKey` | `string` | Clave de localización del texto de la opción |
| `NextLineIndex` | `int` | Índice de la línea a la que salta el script al seleccionar esta opción; **`-1` termina el diálogo** |
| `Condition` | `DialogueCondition` | Condición que debe cumplirse para que esta opción aparezca; `DialogueCondition.None` = siempre visible |

---

## DialogueCondition

Condición evaluada externamente por `DialogueManager.EvaluateCondition`. Es un `readonly struct`.

| Miembro | Tipo | Descripción |
|---|---|---|
| `Key` | `string` | Nombre de la variable o flag a comprobar |
| `Value` | `string` | Valor esperado de la variable |
| `IsEmpty` | `bool` | `true` cuando es equivalente a `None` (key vacía) |
| `None` | `static DialogueCondition` | Sentinel sin condición — la opción siempre es visible |

---

## DialogueScript

| Miembro | Firma | Descripción |
|---|---|---|
| `Count` | `int` | Número total de líneas |
| `GetLine` | `ref readonly DialogueLine GetLine(int index)` | Acceso zero-alloc por referencia de solo lectura |
| `TryGetLine` | `bool TryGetLine(int index, out DialogueLine line)` | Acceso seguro; devuelve `false` si el índice está fuera de rango |

---

## Construcción con Builder

El `Builder` es una clase anidada que usa la API fluent. Las llamadas a `WithChoice` se aplican a la **última línea añadida**.

```csharp
using Alca.MonoGame.Kernel.Dialogue;

DialogueScript script = new DialogueScript.Builder()
    // Línea 0 — presentación
    .AddLine("npc_guard", "guard.stop",        portraitKey: "portrait_guard")

    // Línea 1 — sigue si el jugador tiene el pase
    .AddLine("npc_guard", "guard.welcome",     portraitKey: "portrait_guard")

    // Línea 2 — rechazado sin pase
    .AddLine("npc_guard", "guard.no_pass",     portraitKey: "portrait_guard")

    // Línea 3 — fin de la rama bienvenida
    .AddLine("player",    "player.thank_you")

    // Añadir opciones a la línea 0 (la que acabamos de "dejar abierta" con el builder)
    // Nota: las opciones deben añadirse ANTES de la siguiente AddLine que las "cierra"
    // → por eso el builder acepta la sintaxis encadenada a continuación del AddLine correcto.
    // Reconstruimos el ejemplo de forma más explícita:
    .Build(); // solo para ilustrar la firma

// Ejemplo completo con ramificación:
DialogueScript branchingScript = new DialogueScript.Builder()
    .AddLine("merchant", "merchant.offer")          // índice 0 — con opciones
        .WithChoice("choice.buy",    nextIndex: 2)  // opción 0 → línea 2
        .WithChoice("choice.no",     nextIndex: 3)  // opción 1 → línea 3
        .WithChoice("choice.steal",  nextIndex: 4,
            condition: new DialogueCondition("skill_steal", "true")) // opción 2 — condicional
    .AddLine("merchant", "merchant.wait")           // índice 1 — no alcanzable en este ejemplo
    .AddLine("merchant", "merchant.deal")           // índice 2 — rama compra
    .AddLine("merchant", "merchant.goodbye")        // índice 3 — rama no compra; NextLineIndex=-1 (fin)
    .AddLine("merchant", "merchant.caught")         // índice 4 — rama robo
    .Build();
```

---

## Acceso zero-alloc

Usar `ref readonly` evita copiar el struct en cada acceso:

```csharp
// En Update — sin asignaciones en el heap
ref readonly DialogueLine line = ref script.GetLine(manager.CurrentLineIndex);
string speakerId = line.SpeakerId;
bool hasChoices  = line.HasChoices;
```

---

## Diagrama de ramificación

El siguiente ejemplo muestra cómo los `NextLineIndex` forman un grafo:

```
[0] ¿Compras algo?
  ├─ "Sí, compro"   → [2] ¡Trato hecho!   → fin (-1)
  ├─ "No, gracias"  → [3] Hasta luego      → fin (-1)
  └─ "Robar" [cond] → [4] ¡Alto, ladrón!  → fin (-1)

[1] (línea de espera — inalcanzable en el ejemplo anterior)
```

Cada `NextLineIndex: -1` en una `DialogueChoice` (o avanzar más allá de la última línea) termina el diálogo automáticamente.

---

## Ver también

- [Visión general →](overview.md)
- [DialogueManager →](manager.md)
