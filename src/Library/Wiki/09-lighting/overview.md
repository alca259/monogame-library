# Iluminación — Visión General

**Namespace:** `Alca.MonoGame.Kernel.Lighting`

El sistema de iluminación es un sistema CPU que acumula contribuciones de múltiples luces en un punto del mundo. Para mejor rendimiento, incluye un pipeline GPU opcional que usa shaders HLSL.

---

## Arquitectura

```
GameWorld.LightingWorld (LightingWorld)
    │
    ├── AmbientLight        ← luz ambiental global
    ├── DirectionalLight2D  ← luz direccional (sin posición)
    ├── PointLight2D        ← esfera de luz con falloff
    └── SpotLight2D         ← cono de luz con ángulos inner/outer

    │  (GPU path, opcional)
    └── LightingRenderPipeline
            ├── BeginSceneCapture / EndSceneCapture
            └── ApplyLighting (shader pass)
```

---

## Integración con GameWorld

```csharp
protected override GameWorld? CreateWorld()
{
    return new GameWorld
    {
        LightingWorld = new LightingWorld { AmbientColor = new Color(30, 30, 60) }
    };
}
```

---

## Flujo CPU (sin GPU)

```csharp
// En Update o Draw, resolver el color de iluminación en un punto:
var lightColor = World.LightingWorld.Resolve(entity.Transform.Position2d, LightingLayer.World);
// Usar como tint del sprite
spriteBatch.Draw(texture, position, null, lightColor);
```

---

## Capas de iluminación

| Capa | Descripción |
|---|---|
| `World` | Objetos del mundo (terreno, personajes) |
| `UI` | Elementos de UI con iluminación independiente |
| `Underground` | Zonas subterráneas, mazmorras |
| `Overlay` | Overlays o efectos de primer plano |

---

## Ver también

- [Tipos de luz →](light-types.md)
- [Pipeline GPU →](gpu-pipeline.md)
