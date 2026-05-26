# Persistencia

**Namespace:** `Alca.MonoGame.Kernel.Persistence`

El sistema de persistencia guarda y carga el estado del juego en slots binarios. Usa serialización manual zero-alloc con `SaveDataWriter` y `SaveDataReader`.

---

## ISaveable

Interfaz que deben implementar los objetos que participan en el guardado.

```csharp
public interface ISaveable
{
    void Save(SaveDataWriter writer)
    void Load(SaveDataReader reader)
}
```

---

## SaveDataWriter y SaveDataReader

Escritor y lector binarios. `SaveDataWriter` devuelve su buffer al pool al hacer `Dispose`.

| Escritor | Lector | Tipo |
|---|---|---|
| `Write(bool)` | `ReadBool()` | 1 byte |
| `Write(byte)` | `ReadByte()` | 1 byte |
| `Write(int)` | `ReadInt()` | 4 bytes |
| `Write(uint)` | `ReadUInt()` | 4 bytes |
| `Write(float)` | `ReadFloat()` | 4 bytes |
| `Write(double)` | `ReadDouble()` | 8 bytes |
| `Write(string)` | `ReadString()` | UTF-8 prefijado |
| `Write(Vector2)` | `ReadVector2()` | 8 bytes |
| `Write(Vector3)` | `ReadVector3()` | 12 bytes |
| `Write(Color)` | `ReadColor()` | 4 bytes (RGBA) |

`SaveDataWriter.ToReadOnlySpan()` — devuelve los bytes escritos para pasarlos al `SaveManager`.

`SaveDataReader.IsAtEnd` — `true` cuando se ha leído todo el buffer.

---

## SaveSlot

Metadatos de un slot de guardado.

| Propiedad | Tipo | Descripción |
|---|---|---|
| `Name` | `string` | Identificador del slot |
| `Timestamp` | `DateTimeOffset` | Momento del guardado (UTC) |
| `PlayTimeSeconds` | `float` | Tiempo de juego acumulado |
| `ThumbnailPath` | `string?` | Ruta a la miniatura (opcional) |

---

## SaveManager

### Constructores

```csharp
new SaveManager()                    // carpeta de datos de la aplicación
new SaveManager(string rootPath)     // carpeta personalizada
```

### Métodos

| Método | Descripción |
|---|---|
| `SlotExists(name)` | Comprueba si el slot existe en disco |
| `SaveAsync(name, objects, playTime, thumbnail, ct)` | Guarda todos los objetos al slot |
| `LoadAsync(name, objects, ct)` | Carga el slot; devuelve `false` si no existe |
| `GetSlotsAsync(ct)` | Lista todos los slots ordenados por timestamp |
| `DeleteSlot(name)` | Elimina el slot del disco |

---

## Ejemplo: guardado de progreso

```csharp
public sealed class PlayerProgress : ISaveable
{
    public int Level        { get; set; }
    public int Gold         { get; set; }
    public Vector2 Position { get; set; }

    public void Save(SaveDataWriter w)
    {
        w.Write(Level);
        w.Write(Gold);
        w.Write(Position);
    }

    public void Load(SaveDataReader r)
    {
        Level    = r.ReadInt();
        Gold     = r.ReadInt();
        Position = r.ReadVector2();
    }
}
```

```csharp
// Guardar
var progress = new PlayerProgress { Level = 3, Gold = 200, Position = new Vector2(300, 400) };
await saveManager.SaveAsync("slot1", [progress], playTimeSeconds: 1234f);

// Cargar
var loaded = new PlayerProgress();
bool ok = await saveManager.LoadAsync("slot1", [loaded]);
if (!ok) loaded = new PlayerProgress(); // datos por defecto

// Listar slots para la pantalla de carga
var slots = await saveManager.GetSlotsAsync();
foreach (var slot in slots)
    Console.WriteLine($"{slot.Name} — {slot.Timestamp:g} — {slot.PlayTimeSeconds / 60:F0} min");
```

---

## Notas

- `SaveDataWriter` usa un pool interno para el buffer; siempre llama a `Dispose()` (o úsalo en un `using`).
- El orden de escritura en `Save` debe coincidir exactamente con el de lectura en `Load`.
- Para añadir campos nuevos de forma compatible hacia atrás, añádelos al final y comprueba `reader.IsAtEnd` antes de leer.

---

## Ver también

- [Core →](../01-core/core.md)
- [Async Content →](async-content.md)
