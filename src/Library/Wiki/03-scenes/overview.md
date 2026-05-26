# Scenes — Visión General

**Namespace:** `Alca.MonoGame.Kernel.Scenes`

El sistema de escenas gestiona qué parte del juego está activa (menú, gameplay, pausa, créditos) y las transiciones entre ellas. Cada escena tiene su propio `ContentManager`, `GameWorld` opcional y `UIRoot` opcional.

---

## Diagrama del ciclo de vida

```
SceneManager.RequestChange(new GameScene())
  └─> Fade out (0.3 s)
  └─> Scene anterior: UnloadContent() → World.Destroy() → Content.Unload()
  └─> Scene nueva: Initialize()
        ├─> PreInitialize()        ← EnableUI() aquí si se necesita UI
        ├─> World = CreateWorld()  ← retorna null si no hay ECS
        ├─> InitializeWorld()      ← poblar el mundo con entidades
        ├─> LoadContent()          ← cargar assets con Content.Load<T>()
        └─> PostInitialize()
              └─> InitializeUI()   ← construir el árbol de UI
  └─> Fade in (0.3 s)

Cada frame:
  Scene.Update(gameTime)
    └─> World?.Update(gameTime)

  Scene.Draw(gameTime)
    └─> World?.Draw(gameTime, spriteBatch)
    └─> UIRoot?.DrawAll(spriteBatch)
```

---

## Tipos de escena

| Modo | Descripción | API |
|---|---|---|
| **Escena normal** | Reemplaza la escena actual con fade | `SceneManager.RequestChange(scene)` |
| **Overlay** | Se apila encima; la escena base sigue visible | `SceneManager.PushScene(overlay)` |

---

## Relación con otros módulos

```
Scene
  ├── ContentManager   ← assets propios de la escena
  ├── GameWorld        ← ECS: entidades, physics, lighting...
  └── UIRoot           ← árbol de UI: controles, layouts
```

---

## Ver también

- [Scene (clase base) →](scene.md)
- [SceneManager →](scene-manager.md)
- [GameWorld →](../02-ecs/game-world.md)
- [UI Overview →](../05-ui/overview.md)
