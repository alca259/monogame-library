# Alca.MonoGame — Librería y Editor

Framework de juegos 2D/3D para MonoGame construido sobre **.NET 10 y C# 14**, con un editor visual en desarrollo.

---

## ¿Qué es esto?

Este repositorio contiene dos proyectos principales:

### Alca.MonoGame.Kernel — La librería

`Alca.MonoGame.Kernel` es un framework de alto nivel que extiende MonoGame con todos los sistemas necesarios para desarrollar juegos completos sin partir de cero:

- **ECS** — entidades, componentes (`GameBehaviour`), jerarquías padre-hijo y pools zero-alloc
- **Escenas** — gestor de escenas con transiciones fade y overlays apilables
- **Gráficos 2D/3D** — cámaras, sprites, animación, partículas, shaders, post-procesado, Tiled
- **UI** — más de 20 controles listos para usar, 5 tipos de layout, foco con teclado/gamepad
- **Audio** — mixer por canales, audio 3D espacial, pools y crossfade de música
- **Input** — action maps remapeables con soporte de teclado, ratón y gamepad
- **Física 2D** — integración con Aether.Physics2D: rigid bodies, colliders, joints y queries
- **Iluminación 2D** — luces CPU/GPU: ambiental, direccional, puntual y spot
- **Navegación** — A* sobre NavGrid, NavAgent autónomo y steering behaviors
- **Networking** — servidor/cliente UDP (LiteNetLib), replicación de campos con NetFields
- **Módulos auxiliares** — persistencia, FSM, tweening, event bus, localización, timers y más

Todo el game loop es **zero-alloc**: sin LINQ, sin `new` de clases en `Update`/`Draw`.

### MonoGame Editor — El editor _(en desarrollo)_

Editor visual de MonoGame construido en WinForms, orientado a facilitar la creación de escenas, configuración de entidades y ajuste de propiedades sin necesidad de recompilar.

Actualmente en fase inicial — los proyectos relevantes son:
- `src/Editor/MonoGame.Editor.Core` — lógica central del editor
- `src/Editor/MonoGame.Editor.WinForms` — interfaz de usuario WinForms

---

## Estructura del repositorio

```
src/
├── Library/
│   ├── Alca.MonoGame.Kernel/          ← librería principal
│   ├── Alca.MonoGame.Kernel.UnitTests/← tests xUnit (~700 tests)
│   └── Wiki/                          ← documentación completa
├── Editor/
│   ├── MonoGame.Editor.Core/          ← núcleo del editor (WIP)
│   └── MonoGame.Editor.WinForms/      ← UI del editor (WIP)
└── Demo/
    └── Alca.MonoGame.Demo/            ← proyecto de demostración
```

---

## Requisitos

| Componente | Versión |
|---|---|
| .NET | 10.0 |
| MonoGame.Framework.DesktopGL | 3.8.x |
| MonoGame.Extended | 6.0.x |
| Aether.Physics2D.MG | 2.2.x |
| LiteNetLib | 1.3.x |
| Microsoft.Extensions.DependencyInjection | 10.0.x |

---

## Documentación

La documentación completa de `Alca.MonoGame.Kernel` está disponible en la wiki integrada:

**[→ Wiki / Documentación](src/Library/Wiki/README.md)**

Incluye inicio rápido, referencia de API con ejemplos de código y notas de rendimiento para todos los módulos.
