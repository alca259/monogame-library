# MonoGame Editor — Wiki de desarrollo

Documentación técnica del editor visual para proyectos MonoGame. Esta wiki explica cómo funciona el editor internamente: su arquitectura, paneles, flujos de trabajo y cómo extenderlo.

---

## Índice

| Documento | Descripción |
|-----------|-------------|
| [Arquitectura general](arquitectura.md) | Proyectos de la solución, patrones usados, comunicación entre capas |
| [Paneles de la interfaz](paneles.md) | Todos los paneles de UI, sus controles y comportamiento |
| [Flujos principales](flujos.md) | Paso a paso de las operaciones más importantes |
| [Sistema de comandos (Undo/Redo)](comandos.md) | Cómo funciona el historial de acciones |
| [Modelos de datos](modelos.md) | Clases de dominio: escenas, entidades, behaviours, etc. |
| [Generación de código](codegen.md) | Cómo el editor produce archivos `.cs` del juego |
| [Modo juego (Play/Pause/Stop)](modo-juego.md) | Ciclo de vida del modo de ejecución dentro del editor |
| [Atajos de teclado y referencia rápida](atajos.md) | Todos los atajos y accesos directos del editor |

---

## Estructura de la solución

```
MonoGame.Editor.sln
├── MonoGame.Editor.Core       ← Lógica del editor, sin UI
└── MonoGame.Editor.WinForms   ← Aplicación WinForms, interfaz de usuario
```

- **Core** no contiene ninguna referencia a `System.Windows.Forms`.
- **WinForms** solo referencia a `Core`.
- La comunicación entre paneles es exclusivamente a través de `IEditorEventBus`.

---

## Estructura de carpetas del proyecto de juego objetivo

```
MiJuego/
├── MiJuego.sln
├── src/
│   ├── MiJuego.csproj           ← Apuntado por gameCsprojPath en project.json
│   ├── Game.cs
│   ├── Behaviours/
│   └── Scenes/
│       ├── GameplayScene.cs     ← Parte manual (partial class del desarrollador)
│       └── Generated/
│           └── GameplayScene.Generated.cs  ← AUTO-GENERADO por el editor
├── Content/
│   ├── Content.mgcb
│   ├── Textures/
│   ├── Audio/
│   └── Fonts/
├── Localization/
│   ├── es.json
│   └── en.json
└── Editor/                      ← Archivos del editor (versionables con git)
    ├── project.json             ← Descriptor del proyecto + rutas
    ├── settings.json            ← Configuración (namespace, build config, etc.)
    ├── Scenes/                  ← Archivos .scene.json
    └── Prefabs/                 ← Archivos .prefab.json
```
