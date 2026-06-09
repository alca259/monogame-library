# Librería Alca.MonoGame.Kernel (`src/Library/`)

Librería de utilidades MonoGame en C# 14 sobre .NET 10 (DesktopGL / SDL2 + OpenGL),
con su proyecto de tests xUnit. Solución: `src/Library/Alca.MonoGame.slnx`.

> Se carga automáticamente al trabajar con ficheros bajo `src/Library/`.
> Las convenciones generales de C#/MonoGame están en los ficheros de instrucciones
> de usuario; aquí solo lo específico de la librería. No duplicar reglas genéricas.

## Comandos

```bash
# Compilar toda la solución
dotnet build

# Todos los tests
dotnet test

# Tests sin hardware de audio (CI / máquinas sin OpenAL)
dotnet test --filter "Category!=RequiresAudio"

# Una sola clase de test
dotnet test --filter "FullyQualifiedName~GameEntityTests"
```

## Estructura

```
src/Library/
├── Alca.MonoGame.Kernel/                  ← librería principal
│   └── Alca.MonoGame.Kernel.csproj
└── Alca.MonoGame.Kernel.UnitTests/        ← tests xUnit
    └── Alca.MonoGame.Kernel.UnitTests.csproj
```
- El proyecto de tests referencia directamente al Kernel.
- Los tipos de `MonoGame.Extended` llegan **transitivamente**: no añadir la
  referencia explícita en el `.csproj` de tests.

## Flujo de trabajo (SDD)

- Antes de codificar una tarea, leer su especificación en `docs/specs/`.
- Diseñar primero las firmas públicas del componente, validar que cumplen la
  API de MonoGame y esperar confirmación. No hacer "vibe coding".

## Tests unitarios

Regla transversal del Kernel: **al terminar cada fase de desarrollo, escribir
su test xUnit** en `Alca.MonoGame.Kernel.UnitTests`.

- **Ubicación:** el test espeja la carpeta de origen.
  `ECS/GameEntity.cs` → `ECS/GameEntityTests.cs`.
- **Fichero:** una clase de test `sealed` por clase testeada, nombre `{Clase}Tests.cs`.
- **Nombre del test:** `Método_Escenario_ResultadoEsperado`.
- **Aserciones:** xUnit con `[Fact]`; floats con `Assert.Equal(expected, actual, precision)`.

### Dependencias NuGet (proyecto de tests)

| Paquete | Versión |
|---|---|
| `xunit` | 2.9.3 |
| `xunit.runner.visualstudio` | 3.1.4 |
| `Microsoft.NET.Test.Sdk` | 17.14.1 |
| `coverlet.collector` | 6.0.4 |
| `MonoGame.Framework.DesktopGL` | 3.8.* (`PrivateAssets=All`) |
| `Microsoft.Extensions.DependencyInjection` | 10.0.* |

### Global usings de tests (`Globals.cs`)

```csharp
global using Microsoft.Xna.Framework;
global using Microsoft.Xna.Framework.Graphics;
global using Alca.MonoGame.Kernel.Mathematics;
global using Alca.MonoGame.Kernel.Graphics.Camera;
```

Para cualquier otro namespace de Kernel o de `MonoGame.Extended`, añadir el
`using` explícito en el fichero de test.

### Tests con audio (OpenAL)

- `SoundEffect` y `SoundEffectInstance` requieren OpenAL inicializado.
- Los tests de `SoundEffectPool` que instancien un `SoundEffect` **real** deben
  marcarse con `[Trait("Category", "RequiresAudio")]`, o validarse mediante
  reflexión sobre la API pública si no necesitan el hardware.

### Tests con GraphicsDevice (GPU)

- `GraphicsDevice` requiere un contexto SDL2+OpenGL real.
- **No usar WinForms:** el proyecto es DesktopGL (SDL2/OpenGL), no DirectX; los
  handles de WinForms no son compatibles.
- Infraestructura disponible en `UnitTests/Fixtures/`:

| Clase | Rol |
|---|---|
| `GraphicsDeviceFixture` | `Game` headless (ventana 1×1) que sale tras un frame; expone `GraphicsDevice` y `SpriteBatch` vivos hasta `Dispose()` |
| `GraphicsCollectionDefinition` | `[CollectionDefinition]` de xUnit — una instancia del fixture por colección |
| `GraphicsCollection.Name` | Constante `"GraphicsDevice"` |

- **Patrón:** las clases que necesitan GPU comparten
  `[Collection(GraphicsCollection.Name)]` y reutilizan la **misma instancia** del
  fixture (setup barato). Las que no usan GPU son clases normales, sin `[Collection]`.

```csharp
[Collection(GraphicsCollection.Name)]
public sealed class MiClaseGpuTests
{
    private readonly GraphicsDeviceFixture _fx;

    public MiClaseGpuTests(GraphicsDeviceFixture fx) => _fx = fx;

    [Fact]
    public void Constructor_ConArgumentosValidos_NoLanzaExcepcion()
    {
        using var sut = new MiClase(_fx.GraphicsDevice);
        Assert.NotNull(sut);
    }
}
```
