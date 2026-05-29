# Modo juego (Play / Stop)

El modo Play lanza el juego compilado como **proceso externo** (`GameApp.exe`). El editor no embebe ningún `GraphicsDevice` de MonoGame para el juego: el proceso tiene su propia ventana nativa.

---

## Estados del editor

```
┌────────────┐  Play  ┌────────────┐
│            │───────►│            │
│  Editing   │        │  Playing   │
│            │◄───────│            │
└────────────┘  Stop  └────────────┘
```

| Estado | Viewport editor | Proceso externo | Inspector |
|--------|----------------|-----------------|-----------|
| `Editing` | Activo (gizmos, render) | — | Editable |
| `Playing` | Solo modo editor en pausa | `GameApp.exe` en ejecución | Solo lectura |

---

## Flujo detallado: entrar en modo Play

1. El usuario hace clic en **Play** (o atajo `F5`).
2. Si no hay escena activa: se muestra un `MessageBox` informativo y se cancela.
3. Se guarda la escena automáticamente si tiene cambios sin guardar.
4. **Compilación**: `EditorForm` ejecuta `dotnet build src/GameApp/GameApp.csproj -c Debug` vía `Process`. Cada línea de salida se envía al `ConsolePanel` vía `BuildOutputLineEvent`.
5. Si el build falla: se cancela el modo Play y los errores se muestran en la consola.
6. Si el build tiene éxito: se calcula la ruta al ejecutable:
   ```
   {RootPath}/src/GameApp/bin/Debug/net10.0/GameApp.exe
   ```
7. Se crea `ExternalPlayLauncher` y se llama a `Launch(exePath, scenePath, logLine)`:
   - `scenePath`: ruta absoluta al archivo `.scene.json` activo.
   - `logLine`: callback que envía cada línea de stderr al `ConsolePanel`.
8. Internamente, `ExternalPlayLauncher` lanza:
   ```
   GameApp.exe --scene "{path/.editor/scenes/NombreEscena.scene.json}"
   ```
9. `context.SetState(EditorState.Playing)`.
10. Se publica `EditorStateChangedEvent(Editing, Playing)`.

---

## Flujo detallado: detener el modo Play (Stop)

1. El usuario hace clic en **Stop**.
2. `_playLauncher.Stop()` llama a `Process.Kill(entireProcessTree: true)` — termina el proceso y todos sus hijos.
3. `context.SetState(EditorState.Editing)`.
4. Se publica `EditorStateChangedEvent(Playing, Editing)`.
5. El viewport del editor vuelve a estar activo.

---

## `ExternalPlayLauncher`: gestión del proceso

`ExternalPlayLauncher` encapsula el `Process` del juego.

| Miembro | Descripción |
|---------|-------------|
| `IsRunning` | `true` si el proceso existe y no ha terminado |
| `Launch(exePath, scenePath, logLine?)` | Inicia el proceso con `--scene {scenePath}`. Redirige stderr; cada línea invoca `logLine`. |
| `Stop()` | `Process.Kill(entireProcessTree: true)` |
| `Dispose()` | Llama a `Stop()` |

### Argumentos del proceso de juego

`GameApp.exe` generado por el scaffolding espera:

```csharp
// Program.cs generado
string scenePath = args.SkipWhile(a => a != "--scene").Skip(1).FirstOrDefault() ?? string.Empty;
using var game = new Game1(scenePath);
game.Run();
```

Si `scenePath` está vacío, el juego arranca sin cargar una escena específica.

---

## Comparación con el modo Play embebido anterior

| Aspecto | Antes (embebido) | Ahora (externo) |
|---------|-----------------|-----------------|
| Renderizado | `MonoGameControl` dentro del editor | Ventana nativa del proceso |
| Inicio | Conversión de escena a `GameWorld` por reflexión | Compilación + `GameApp.exe` |
| Assets | Sin `ContentManager` disponible | `ContentManager` completo del juego |
| Sprites | No se renderizaban | Se renderizan correctamente |
| Stop | `PlayModeRunner.Dispose()` + restaurar snapshot | `Process.Kill()` |

---

## Consideraciones al desarrollar con el modo Play

1. **El primer Play siempre compila**. Si el código tiene errores, el modo Play no arranca.
2. **Los cambios de escena durante Play no persisten** — la escena en disco no cambia mientras el juego corre.
3. **Los behaviours pueden tener cualquier constructor** — ya no es necesario el constructor sin parámetros que requería la reflexión del editor.
4. **La salida de stderr del juego** (excepciones no capturadas, logs de depuración) aparece en el `ConsolePanel` del editor.
5. **La ventana del juego es independiente** — el usuario puede redimensionarla o moverla libremente.
