# MonoGame Editor — Wiki de desarrollo

Documentación técnica del editor visual para proyectos MonoGame. Esta wiki explica cómo funciona el editor internamente: su arquitectura, paneles, flujos de trabajo y cómo extenderlo.

---

## Índice

| Documento | Descripción |
|-----------|-------------|
| [Arquitectura general](arquitectura.md) | Proyectos de la solución, patrones usados, comunicación entre capas |
| [Paneles de la interfaz](paneles.md) | Todos los paneles de UI, sus controles y comportamiento |
| [Flujos principales](flujos.md) | Paso a paso de las operaciones más importantes |
| [Flujos completos (hoja de ruta)](flujos-completos.md) | Mapa de referencia de todos los flujos internos con rutas de archivo |
| [Sistema de comandos (Undo/Redo)](comandos.md) | Cómo funciona el historial de acciones |
| [Modelos de datos](modelos.md) | Clases de dominio: escenas, entidades, behaviours, etc. |
| [Generación de código](codegen.md) | SourceGenerator Roslyn + cómo el editor produce archivos `.cs` del juego |
| [Modo juego (Play/Stop)](modo-juego.md) | Ciclo de vida del modo de ejecución dentro del editor |
| [Atajos de teclado y referencia rápida](atajos.md) | Todos los atajos y accesos directos del editor |

---

## Estructura de la solución

```
MonoGame.Editor.slnx
├── MonoGame.Editor.Core             ← Lógica del editor, sin UI
├── MonoGame.Editor.Maui             ← Aplicación MAUI (Windows), interfaz de usuario
└── MonoGame.Editor.SourceGenerator  ← Roslyn IIncrementalGenerator (netstandard2.0)
```

- **Core** no contiene ninguna referencia a MAUI ni a `System.Windows.Forms`.
- **Maui** solo referencia a `Core`.
- **SourceGenerator** se empaqueta como analizador en `GameApp.csproj`; convierte `*.scene.json` en C# estático compatible AOT.
- La comunicación entre paneles es exclusivamente a través de `IEditorEventBus`.

---

## Estructura de carpetas del proyecto de juego objetivo

```
MiJuego/
├── project.json                 ← Descriptor del proyecto (en la raíz)
├── .editor/                     ← Carpeta interna del editor (versionable con git)
│   ├── config/
│   │   └── settings.json        ← Configuración (namespace, build config, etc.)
│   ├── logs/                    ← Logs del editor y del proceso de juego
│   ├── scenes/                  ← Archivos .scene.json
│   └── prefabs/                 ← Archivos .prefab.json
└── src/                         ← Código fuente del juego (generado al crear proyecto)
    ├── MiJuego.slnx             ← Solución del juego
    ├── GameApp/                 ← Proyecto ejecutable
    │   ├── GameApp.csproj
    │   ├── Program.cs
    │   ├── Game1.cs
    │   ├── Content/             ← Assets compilados (MGCB)
    │   └── i18n/                ← Archivos de localización *.json
    └── GameScripts/             ← Librería de scripts del juego
        └── GameScripts.csproj
```
