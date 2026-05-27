# Modo juego (Play / Pause / Stop)

El editor incluye un modo de ejecución en tiempo real que permite probar el juego directamente dentro del viewport, sin salir del editor.

---

## Estados del editor

```
┌────────────┐  Play  ┌────────────┐  Pause  ┌────────────┐
│            │───────►│            │────────►│            │
│  Editing   │        │  Playing   │         │  Paused    │
│            │◄───────│            │◄────────│            │
└────────────┘  Stop  └────────────┘ Resume  └────────────┘
      ▲                                              │
      └──────────────────── Stop ───────────────────┘
```

| Estado | Update | Draw | Gizmos | Inspector |
|--------|--------|------|--------|-----------|
| `Editing` | No | Render del editor | Visibles | Editable |
| `Playing` | Sí (`GameWorld.Update`) | `GameWorld.Draw` | Ocultos | Solo lectura |
| `Paused` | No | `GameWorld.Draw` | Visibles | Editable en caliente |

---

## Flujo detallado: entrar en modo Play

1. El usuario hace clic en el botón **Play** (o usa el atajo `F5` si está configurado).
2. Si no hay escena activa: se muestra un `MessageBox` informativo y se cancela.
3. Si hay un archivo `Content.mgcb`, el editor compila el contenido primero (`MgcbRunner`).
4. **TakePlaySnapshot()**: se serializa la escena activa completa a JSON en memoria. Este snapshot se usará para restaurar la escena al detener.
5. Se llama a `context.SetState(EditorState.Playing)`.
6. Se publica `EditorStateChangedEvent(Editing, Playing)`.
7. `EditorForm` crea un `PlayModeRunner` con la escena actual.
8. `PlayModeRunner` llama a `SceneToWorldConverter.Convert(scene, registry)` para construir un `GameWorld` real del Kernel.
9. El viewport cambia al modo de juego (los gizmos se ocultan).
10. El game loop arranca: cada frame llama `_playRunner.Update(elapsed)` y `_playRunner.Draw(elapsed)`.

---

## Flujo detallado: Pause y Resume

### Pause
1. El usuario hace clic en **Pause**.
2. `context.SetState(EditorState.Paused)`.
3. `PlayModeRunner.Update` deja de llamarse.
4. `PlayModeRunner.Draw` sigue llamándose (se ve el estado congelado del juego).
5. Los gizmos se vuelven a dibujar sobre el juego pausado.
6. El inspector se vuelve editable para modificar valores "en caliente".

### Resume
1. El usuario hace clic en **Play** (mientras está en Paused).
2. `context.SetState(EditorState.Playing)`.
3. `Update` se reanuda desde donde se pausó.

---

## Flujo detallado: detener el modo Play (Stop)

1. El usuario hace clic en **Stop**.
2. `PlayModeRunner.Dispose()` libera el `SpriteBatch` y limpia los recursos.
3. `context.RestoreFromSnapshot()`: deserializa el JSON del snapshot guardado antes de entrar en Play.
4. `context.SetActiveScene(escenaRestaurada)`.
5. `context.ClearPlaySnapshot()`: libera el snapshot de memoria.
6. `context.SetState(EditorState.Editing)`.
7. Se publica `EditorStateChangedEvent(Playing, Editing)`.
8. El viewport vuelve al modo editor con los gizmos visibles.
9. La jerarquía y el inspector reflejan el estado original de la escena (antes de Play).

**Regla importante**: cualquier cambio hecho durante el modo Play (mover entidades, modificar propiedades) se **descarta** al pulsar Stop, porque se restaura el snapshot pre-play.

---

## `SceneToWorldConverter`: de escena editor a GameWorld real

`SceneToWorldConverter.Convert(editorScene, registry)` traduce el modelo del editor al modelo del Kernel:

### Conversión de entidades

Para cada `EditorGameObject`:
1. Crea un `GameEntity` con `world.CreateEntity(nombre, posición)`.
2. Aplica el transform (posición, rotación, escala).
3. Establece `entity.Active = obj.Active`.
4. Añade cada tag: `entity.AddTag(tag)`.
5. Establece el padre si tiene: `entity.SetParent(padreEntity)`.
6. Para cada behaviour, intenta instanciarlo y añadirlo.

### Instanciación de behaviours

1. Busca el tipo por `TypeName` en el registry.
2. Llama a `Activator.CreateInstance(type)` para crear la instancia.
3. Usa `entity.Add<T>()` con `MakeGenericMethod` para añadirlo al entity.
4. Deserializa cada propiedad del diccionario `JsonElement` al tipo correcto.
5. Asigna las propiedades via reflexión.

### Casos especiales

- **`SpriteRendererBehaviour`**: se omite silenciosamente en PlayMode (requiere `Texture2D` como parámetro del constructor, y en el editor no tenemos el `ContentManager` del juego disponible).
- **Entidades inactivas**: se transmite correctamente `entity.Active = false`.
- **Tags**: se transmiten todos para que las queries `World.GetEntitiesByTag` funcionen en los `Start()`.

### Configuración de subsistemas

Antes del bucle de entidades, `ApplyWorldConfig(world, scene.WorldConfig)` inicializa los subsistemas:

```
UsePhysics2D=true  → world.PhysicsWorld = new Physics2DWorld(gravedad)
UseLighting=true   → world.LightingWorld = new LightingWorld { AmbientColor = ... }
UseNavigation=true → world.NavGrid = new NavGrid(...); world.Pathfinder = new Pathfinder(...)
UseAudio=true      → world.AudioController = new AudioController()
```

---

## `PlayModeRunner`: el game loop del editor

`PlayModeRunner` encapsula el `GameWorld` activo durante el modo Play.

| Método | Qué hace |
|--------|----------|
| Constructor | Llama a `SceneToWorldConverter.Convert()` para construir el mundo |
| `EnsureInitialized(graphicsDevice)` | Crea el `SpriteBatch` la primera vez que se llama (en el render thread) |
| `Update(elapsed)` | Llama a `world.Update(gameTime)` con el delta time del frame |
| `Draw(elapsed)` | Llama a `SpriteBatch.Begin()`, `world.Draw(...)`, `SpriteBatch.End()`. Envuelto en try-catch para que los behaviours sin contenido fallen silenciosamente. |
| `Dispose()` | Libera el `SpriteBatch` |

---

## Consideraciones importantes al desarrollar behaviours para Play Mode

1. **SpriteRendererBehaviour no funciona en Play Mode del editor** (el sprite no se renderiza). Para ver sprites necesitas ejecutar el juego real con `Project → Run Game`.

2. **Los cambios del modo Play se descartan**. Si necesitas probar cambios persistentes, edita en modo `Editing`.

3. **Los behaviours deben tener constructor sin parámetros** para que el editor pueda instanciarlos. Si tu behaviour necesita dependencias, usa el método `Awake()` o `Start()` del ciclo de vida.

4. **Las propiedades deben ser públicas con getter y setter** para que el editor pueda leerlas y escribirlas por reflexión.

5. **Los queries por tag funcionan correctamente** en PlayMode (los tags se transfieren del editor al Kernel).

6. **Los subsistemas (Physics, Lighting, etc.) solo están disponibles si se configuran** en `Scene → Configure World Subsystems...`.
