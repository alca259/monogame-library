# Sistema de comandos (Undo/Redo)

Cada acción que modifica la escena o el estado del editor se encapsula en un objeto `IEditorCommand`. Esto permite deshacer (`Ctrl+Z`) y rehacer (`Ctrl+Y`) cualquier operación de forma fiable.

---

## Contrato de un comando

```csharp
public interface IEditorCommand
{
    string Description { get; }  // Texto que aparece en el historial
    void Execute();               // Aplica el cambio
    void Undo();                  // Revierte el cambio
}
```

- `Execute()` se llama una sola vez al crear el comando.
- `Undo()` restaura el estado anterior al `Execute()`.
- Un segundo `Execute()` (el redo) vuelve a aplicar el cambio.

---

## CommandStack — El historial

`CommandStack` mantiene dos estructuras:
- **Historial de undo**: lista enlazada de comandos ejecutados (máximo 100 por defecto).
- **Pila de redo**: comandos que se han deshecho y pueden rehacerse.

### Ciclo de vida de un comando

```
Usuario hace acción
    → CommandStack.Execute(cmd)
        → cmd.Execute()
        → Guarda cmd en historial
        → Limpia pila de redo
        → Llama context.MarkSceneDirty()

Usuario presiona Ctrl+Z
    → CommandStack.Undo()
        → Saca cmd del historial
        → cmd.Undo()
        → Guarda cmd en pila de redo
        → Publica UndoPerformedEvent(cmd.Description)

Usuario presiona Ctrl+Y
    → CommandStack.Redo()
        → Saca cmd de la pila de redo
        → cmd.Execute()
        → Guarda cmd en historial
        → Publica RedoPerformedEvent(cmd.Description)
```

**Regla importante**: cuando se ejecuta un comando nuevo, la pila de redo se vacía. Esto es comportamiento estándar (como en cualquier editor).

---

## Lista completa de comandos disponibles

### Entidades

| Comando | Qué hace | Undo |
|---------|----------|------|
| `CreateEntityCommand` | Añade una entidad a la escena (o como hija de otra) | Elimina la entidad creada |
| `DeleteEntityCommand` | Elimina una entidad (guarda índice y padre) | Reinsertar la entidad en su posición original |
| `RenameEntityCommand` | Cambia el nombre de la entidad | Restaura el nombre anterior |
| `ReparentEntityCommand` | Cambia la entidad de padre en la jerarquía | Restaura el padre y posición en el árbol |

### Transforms

| Comando | Qué hace | Undo |
|---------|----------|------|
| `MoveEntityCommand` | Actualiza `Position` (X e Y) | Restaura la posición anterior |
| `MoveEntityZCommand` | Actualiza `PositionZ` (profundidad 2.5D) | Restaura el Z anterior |
| `RotateEntityCommand` | Actualiza `Rotation` | Restaura la rotación anterior |
| `ScaleEntityCommand` | Actualiza `Scale` | Restaura la escala anterior |

### Behaviours

| Comando | Qué hace | Undo |
|---------|----------|------|
| `AddBehaviourCommand` | Añade un `EditorBehaviour` a una entidad | Elimina el behaviour añadido |
| `RemoveBehaviourCommand` | Elimina un behaviour de una entidad (guarda copia) | Restaura el behaviour y sus propiedades |
| `SetPropertyCommand<T>` | Modifica una propiedad de un behaviour | Restaura el valor anterior de la propiedad |

### Prefabs

| Comando | Qué hace | Undo |
|---------|----------|------|
| `ApplyPrefabCommand` | Guarda los cambios de la instancia al archivo `.prefab.json` | Restaura el archivo de prefab al estado anterior |
| `RevertPrefabCommand` | Reemplaza la instancia con una copia limpia del prefab | Restaura la instancia al estado antes de revertir |

### Tags

| Comando | Qué hace | Undo |
|---------|----------|------|
| `SetTagsCommand` | Reemplaza la lista de tags de una entidad | Restaura la lista de tags anterior |

### Tilemaps

| Comando | Qué hace | Undo |
|---------|----------|------|
| `PaintTileCommand` | Pinta un tile en una celda de una capa | Restaura el tile anterior en esa celda |
| `EraseTileCommand` | Borra el tile de una celda | Restaura el tile borrado |

### Input Maps

| Comando | Qué hace | Undo |
|---------|----------|------|
| `AddInputActionCommand` | Añade una action al mapa de input | Elimina la action añadida |
| `RemoveInputActionCommand` | Elimina una action del mapa | Restaura la action eliminada |
| `AddInputBindingCommand` | Añade un binding a una action | Elimina el binding añadido |
| `RemoveInputBindingCommand` | Elimina un binding de una action | Restaura el binding eliminado |

### Localización

| Comando | Qué hace | Undo |
|---------|----------|------|
| `SetLocalizationValueCommand` | Establece un valor en `[locale][clave]` | Restaura el valor anterior |

### Generación de código

| Comando | Qué hace | Undo |
|---------|----------|------|
| `GenerateSceneCodeCommand` | Genera el archivo `.Generated.cs` de la escena | Restaura el backup del archivo previo (si existía) |

---

## Cómo crear un comando nuevo

Al añadir una nueva operación al editor que modifique datos, hay que crear un comando. Pasos:

1. Crear una clase en `MonoGame.Editor.Core/Commands/` que implemente `IEditorCommand`.
2. El constructor recibe los datos necesarios para ejecutar Y para deshacer la operación.
3. Implementar `Execute()` y `Undo()` como operaciones inversas.
4. En el código de UI (WinForms), en lugar de modificar el modelo directamente, llamar:

```csharp
var cmd = new MiNuevoComando(parametros...);
_context.CommandStack.Execute(cmd);
```

### Ejemplo: `SetPropertyCommand<T>`

Es el comando más genérico. Toma una función para leer y escribir el valor:

```csharp
// Cómo se usa desde el inspector:
var cmd = new SetPropertyCommand<float>(
    $"Set Speed",
    () => behaviour.Properties["Speed"].GetSingle(),     // getter del valor actual
    v => behaviour.Properties["Speed"] = JsonSerializer.SerializeToElement(v),  // setter
    nuevoValor);
_context.CommandStack.Execute(cmd);
```

---

## Integración con la jerarquía y el inspector

Cuando se ejecuta Undo o Redo:
1. Se publica `UndoPerformedEvent` o `RedoPerformedEvent`.
2. La jerarquía recibe el evento y reconstruye el árbol completo.
3. El inspector recibe el evento y recarga los controles del objeto seleccionado.

Esto garantiza que la UI siempre esté sincronizada con el estado real de la escena.
