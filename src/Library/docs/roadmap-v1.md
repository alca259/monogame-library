# Roadmap: Extensión Completa de Alca.MonoGame.Kernel

## Contexto

La librería ya tiene bases sólidas: Core, ECS, Events, Graphics 2D (sprites/animaciones/tilemaps), Input (teclado/ratón/gamepad), Audio básico, Scenes con fade, y UI mínima.

El objetivo es extenderla con todos los sistemas cubiertos por las skills disponibles, añadiendo `MonoGame.Extended` como dependencia para los módulos de Particles, Tweening, Tiled y BitmapFonts. Los sistemas propios (Math, Camera, UI completo, Audio 3D, ECS extendido, Localization, Platform, Shaders, 3D) se implementan desde cero.

**Reglas transversales a todos los archivos:**
- `sealed` por defecto; `abstract` solo si es base class explícita
- File-scoped namespaces: `namespace Alca.MonoGame.Kernel.{Módulo};`
- `_camelCase` para campos privados, PascalCase para públicos
- Sin LINQ en `Update()`/`Draw()` — solo `for` indexado
- Sin `new` de clases en `Update()`/`Draw()` (structs sí)
- XML docs en todos los miembros públicos (single-line `<summary>` si cabe)
- `#nullable enable` asumido en todo el proyecto
- **Se debe usar siempre Dependency Injection**. Se usará Microsoft.Extensions.DependencyInjection

**Reglas transversales a todos los desarrollos:**
- Al terminar, debe actualizarse el fichero roadmap.md, marcando que se ha hecho de los TODOs.
- Al terminar el desarrollo de una fase, **debe** escribirse el correspondiente Test unitario con xUnit en el proyecto `src\Library\Alca.MonoGame.Kernel.UnitTests`
- Todos los ficheros de tests que se escriban deben estar bajo la misma nomenclatura que la carpeta origen, es decir, si un fichero de servicio/helper/otros está en `Utils/MyFileName.cs` en el proyecto de test debe estar bajo la carpeta `Utils`.
- Para facilitar la búsqueda de tests, todos los ficheros de test deben llevar el nombre del fichero que prueban y `Tests` concatenado al final.

---

## Prerrequisito: Dependencias NuGet ✅ COMPLETADO

**Archivo:** `src/Library/Alca.MonoGame.Kernel/Alca.MonoGame.Kernel.csproj`

Paquete añadido:
- `MonoGame.Extended` 6.0.* (incluye Tiled, Tweening, Particles, BitmapFonts en un único paquete a partir de v6)

---

## FASE 1 — Fundamentos Matemáticos y de Cámara ✅ COMPLETADA

> **Objetivo:** Infraestructura matemática y de cámara que el resto de sistemas necesita.

### Milestone 1.1 — Math Utilities ✅

**`Mathematics/MathUtils.cs`** — `static sealed class MathUtils`
- `DistanceSquared(Vector2 a, Vector2 b)` — evita sqrt innecesario
- `AngleBetween(Vector2 from, Vector2 to)` — radianes
- `AngleToVector2(float radians)` — conversión directa
- `WrapAngle(float angle)` — normaliza a [-π, π]
- `Clamp(float value, float min, float max)` — inline de MathHelper
- `Lerp(float a, float b, float t)` y overloads para `Vector2`, `Color`
- `SmoothStep(float a, float b, float t)` — suavizado cúbico
- `MapRange(float value, float inMin, float inMax, float outMin, float outMax)`

**`Mathematics/BoundingHelpers.cs`** — `static sealed class BoundingHelpers`
- `CreateBoundingSphere(Vector3 center, float radius)`
- `CreateBoundingBox(Vector3 min, Vector3 max)`
- `RayIntersectsPlane(Ray ray, Plane plane, out float distance)`
- `RayIntersectsSphere(Ray ray, BoundingSphere sphere, out float distance)`
- `ScreenToWorldRay(Vector2 screenPos, Matrix view, Matrix projection, Viewport viewport)`

---

### Milestone 1.2 — Camera 2D ✅

**`Graphics/Camera/Camera2D.cs`** — `sealed class Camera2D`
- Campos pre-allocated: `_transform` (Matrix), `_inverseTransform` (Matrix)
- Propiedades: `Position` (Vector2), `Zoom` (float, clamp min/max), `Rotation` (float)
- `GetTransformMatrix(Viewport viewport)` → Matrix para pasar a `SpriteBatch.Begin`
- `ScreenToWorld(Vector2 screenPos, Viewport viewport)` → Vector2
- `WorldToScreen(Vector2 worldPos, Viewport viewport)` → Vector2
- `Follow(Vector2 target, float lerpFactor)` — suavizado sin alloc
- `ClampToBounds(Rectangle worldBounds)` — limita la cámara al mapa

---

### Milestone 1.3 — Camera 3D ✅

**`Graphics/Camera/Camera3D.cs`** — `abstract class Camera3D`
- Propiedades abstractas: `View` (Matrix), `Projection` (Matrix)
- `Position` (Vector3), `Target` (Vector3), campo `_up` (Vector3.Up pre-set)
- `GetFrustum()` → `BoundingFrustum` (recalcula solo si dirty)
- Flag `_dirty` para recálculo lazy de matrices

**`Graphics/Camera/FixedCamera3D.cs`** — `sealed class FixedCamera3D : Camera3D`
- Posición y target constantes. View calculado una vez.

**`Graphics/Camera/FirstPersonCamera3D.cs`** — `sealed class FirstPersonCamera3D : Camera3D`
- `Yaw`, `Pitch` (float) con clamp en Pitch (±89°)
- `MoveForward(float speed)`, `Strafe(float speed)` — sin alloc
- `Look(float deltaYaw, float deltaPitch)`

**`Graphics/Camera/ThirdPersonCamera3D.cs`** — `sealed class ThirdPersonCamera3D : Camera3D`
- `TargetEntity` (referencia a posición del jugador)
- `Distance`, `Height`, `HorizontalAngle` (float)
- Spring-damper: `_velocity` (Vector3 field), `SpringStiffness`, `DampingRatio`
- `Update(GameTime)` — aplica spring smoothing sin alloc

**`Graphics/Camera/TopDownCamera3D.cs`** — `sealed class TopDownCamera3D : Camera3D`
- `Follow(Vector3 target)` con lerp opcional
- Zoom por altura (Y)

---

### Milestone 1.4 — Resolution Independence ✅

**`Graphics/ResolutionManager.cs`** — `sealed class ResolutionManager`
- `VirtualWidth`, `VirtualHeight` (int) — resolución de diseño (default 1920×1080)
- `ScaleMatrix` (Matrix) — para pasar a `SpriteBatch.Begin` de UI
- `WorldScaleMatrix` (Matrix) — para cámara 2D del mundo
- `LetterboxViewport` (Viewport) — viewport con barras negras
- `Update(int screenWidth, int screenHeight)` — recalcula matrices y viewport
- `ScreenToVirtual(Vector2 screenPos)` → Vector2 — para input mapping correcto
- Suscripción a `ClientSizeChanged` del `GameWindow`

---

## FASE 2 — Rendering Avanzado ✅ COMPLETADA

> **Objetivo:** Shaders, efectos de post-procesado y soporte 3D.

### Milestone 2.1 — Render Targets y Post-Processing ✅

**`Graphics/Effects/RenderTargetManager.cs`** — `sealed class RenderTargetManager : IDisposable`
- `_targetA`, `_targetB` (RenderTarget2D) — ping-pong pre-allocated
- `BeginCapture()` → establece `_targetA` como render target activo
- `EndCapture()` → devuelve al backbuffer
- `Apply(Effect effect)` → renderiza `_targetA` → `_targetB` con el effect, luego copia al backbuffer
- `ApplyChain(Effect[] effects)` — encadena múltiples passes

**`Graphics/Effects/PostProcessEffect.cs`** — `abstract class PostProcessEffect`
- Wrappea un `Effect` cargado
- `abstract void SetParameters(...)` — overrideado por efectos concretos
- `Apply(RenderTargetManager rtm)` — llama a SetParameters + rtm.Apply

---

### Milestone 2.2 — Material y Shader System ✅

**`Graphics/Shaders/Material.cs`** — `abstract class Material`
- `Effect Effect { get; }` — el Effect cargado vía `Content.Load<Effect>`
- `abstract void Apply()` — setea parámetros del Effect antes de renderizar
- `GetParameter<T>(string name)` — helper tipado para `Effect.Parameters[name]`

**`Graphics/Shaders/SpriteMaterial.cs`** — `sealed class SpriteMaterial : Material`
- Listo para usar con `SpriteBatch.Begin(effect: material.Effect)`
- Parámetros comunes: `Alpha` (float), `TintColor` (Color)

---

### Milestone 2.3 — 3D Rendering ✅

**`Graphics/ThreeD/MeshRenderer.cs`** — `sealed class MeshRenderer`
- `Load(ContentManager content, string assetName)` → carga un `Model`
- `Draw(Camera3D camera, Matrix worldTransform)` — itera `model.Meshes` sin alloc
- `SetTexture(Texture2D texture)` — override de textura en BasicEffect
- `BoundingSphere` property — para frustum culling

**`Graphics/ThreeD/PrimitiveBatch.cs`** — `sealed class PrimitiveBatch : IDisposable`
- Buffer fijo pre-allocated de `VertexPositionColor[]` (capacidad configurable)
- `Begin(Camera3D camera, PrimitiveType type)`
- `AddVertex(Vector3 pos, Color color)` — escribe en el buffer
- `End()` → llama `DrawUserPrimitives` y resetea el índice
- Helpers: `DrawLine(Vector3 a, Vector3 b, Color color)`, `DrawWireSphere(...)`, `DrawWireBox(...)`

---

## FASE 3 — Sistemas de Juego Core ✅ COMPLETADA

> **Objetivo:** Partículas, tweening, audio avanzado, ECS extendido y escenas con stack.

### Milestone 3.0 - Refactoring Core.cs y Tilemap, Tileset ✅ COMPLETADO

**Decisiones tomadas:**

**Core.cs — DI refactor:**
- `Microsoft.Extensions.DependencyInjection` añadido como dependencia.
- `ServiceCollection` se construye dentro de `Initialize()` (tras `base.Initialize()`) para que `GraphicsDevice`, `SpriteBatch` y demás objetos MonoGame estén inicializados.
- Las propiedades estáticas se mantienen para rendimiento en el game loop (sin resolución DI en hot path); se cachean tras `BuildServiceProvider()`.
- Hook `ConfigureServices(IServiceCollection)` virtual añadido para que subclases registren sus propios servicios.
- Gestión de escenas duplicada eliminada: `ChangeScene()` y `TransitionScene()` eliminados. **Migration:** usar `Core.SceneManager.RequestChange(scene)`.
- `GameGraphicsDevice` eliminado (era duplicado de `GraphicsDevice`). **Migration:** usar `Core.GraphicsDevice`.
- `GC.Collect()` eliminado de la transición de escenas (mala práctica).
- `SceneManager` expuesto como propiedad estática de `Core`, registrado vía DI.

**SceneManager.cs — limpieza:**
- Clase sellada (`sealed`).
- `ContentManager` redundante eliminado de `SetupAndStartScene` (la `Scene` ya crea el suyo propio).
- Código comentado `//scene.Setup(...)` eliminado.
- `SetupAndStartScene` convertido a `private`.

**Tilemap/Tileset — eliminados:**
- `Graphics/Models/Tilemap.cs` y `Graphics/Models/Tileset.cs` eliminados.
- Motivo: formato XML custom sin soporte de tooling; la alternativa estándar es el sistema Tiled (TMX/JSON) de MonoGame.Extended.
- **Migration:** Milestone 6.1 provee `TiledMapRenderer` y `TiledObjectLayer` como sustitutos.

### Milestone 3.1 — Particle System (via MonoGame.Extended.Particles) ✅ COMPLETADO

**`Graphics/Particles/ParticleEffectWrapper.cs`** — `sealed class ParticleEffectWrapper`
- Wrappea `MonoGame.Extended.Particles.ParticleEffect`
- `LoadFromFile(ContentManager content, string assetName)`
- `Update(GameTime gameTime, Vector2 emitterPosition)`
- `Draw(SpriteBatch spriteBatch, BlendState blendState)`
- `Trigger(Vector2 position)` — burst manual

**`Graphics/Particles/ParticleBuilder.cs`** — `sealed class ParticleBuilder`
- Fluent API sobre los emitters de Extended para construir efectos en código:
  - `WithSprayProfile(float spread, float speed)`
  - `WithCircleProfile(float radius)`
  - `WithGravity(float gravityY)`
  - `WithLifetime(float min, float max)`
  - `WithColorRange(Color start, Color end)`
  - `Build()` → `ParticleEffect`

---

### Milestone 3.2 — Tweening System (via MonoGame.Extended.Tweening) ✅ COMPLETADO

**`Tweening/TweeningManager.cs`** — `sealed class TweeningManager`
- Wrappea `MonoGame.Extended.Tweening.Tweener`
- `TweenTo<T>(T target, Expression<Func<T, float>> member, float toValue, float duration, EasingFunction easing)`
- `Update(GameTime gameTime)` — delegado al Tweener interno
- `CancelAll()`, `Cancel(ITween tween)`

**`Tweening/EasingCatalog.cs`** — `static class EasingCatalog`
- Re-exporta las funciones de `EasingFunctions` de Extended con nombres más cómodos:
  - `Linear`, `EaseIn`, `EaseOut`, `EaseInOut`, variantes Quad/Cubic/Bounce/Elastic/Back

Integración en `Core.cs`: añadir `Tweening` como propiedad estática de tipo `TweeningManager`.

---

### Milestone 3.3 — Audio Extendido ✅ COMPLETADO

**`Audio/SoundEffectPool.cs`** — `sealed class SoundEffectPool : IDisposable`
- Constructor: `SoundEffectPool(SoundEffect effect, int capacity)` — pre-alloca `capacity` instancias
- `_instances` (SoundEffectInstance[]) — buffer fijo, sin alloc en Play
- `Play(float volume, float pitch, float pan)` — rota por el buffer (round-robin)
- `StopAll()`
- Captura `InstancePlayLimitException` silenciosamente

**`Audio/AudioListener3D.cs`** — `sealed class AudioListener3D`
- Wrappea `AudioListener`
- `Position` (Vector3), `Forward` (Vector3), `Up` (Vector3), `Velocity` (Vector3)
- `Update(Vector3 position, Vector3 forward)` — actualiza el listener sin alloc

**`Audio/AudioEmitter3D.cs`** — `sealed class AudioEmitter3D`
- Wrappea `AudioEmitter`
- `Position`, `Forward`, `Velocity` (Vector3)
- `Apply3D(SoundEffectInstance instance, AudioListener3D listener)` — llama `instance.Apply3D`

**`Audio/AudioController.cs`** — **MODIFICAR**
- Añadir `CreatePool(SoundEffect effect, int capacity)` → `SoundEffectPool`
- Añadir `_listener` (AudioListener3D field, inicializado en constructor)
- `UpdateListener(Vector3 position, Vector3 forward)` — actualiza la posición del oyente

---

### Milestone 3.4 — ECS Extendido ✅ COMPLETADO

**`ECS/GameEntity.cs`** — **MODIFICAR**
- Campo: `_tags` (HashSet<string> pre-allocated en constructor, capacidad 8)
- `AddTag(string tag)`, `RemoveTag(string tag)`, `HasTag(string tag)` → bool
- `GetTags()` → retorna `IReadOnlySet<string>`

**`ECS/GameWorld.cs`** — **MODIFICAR**
- `GetEntitiesByTag(string tag)` → llena un `List<GameEntity>` pasado por parámetro (sin alloc retornando lista nueva)
- `GetBehavioursWithInterface<TInterface>()` → llena `List<TInterface>` pasada por parámetro

**`ECS/EntityPool.cs`** — `sealed class EntityPool`
- Constructor: `EntityPool(GameWorld world, int capacity)` — pre-alloca `capacity` entidades inactivas
- `Get()` → reactiva y devuelve entidad del pool
- `Return(GameEntity entity)` → desactiva y devuelve al pool
- Útil para proyectiles, enemigos frecuentes

---

### Milestone 3.5 — Scene Stack ✅ COMPLETADO

**`Scenes/Scene.cs`** — **MODIFICAR**
- Añadir `bool IsOverlay` property (default `false`) — si `true`, la escena anterior sigue dibujándose debajo

**`Scenes/SceneManager.cs`** — **MODIFICAR**
- Añadir `_sceneStack` (Stack<Scene> con capacidad inicial 4, pre-allocated)
- `PushScene(Scene overlay)` — apila sin destruir la escena actual; llama Initialize en el overlay
- `PopScene()` — destruye el overlay del tope y reactiva la escena anterior
- `RequestChange()` — sigue funcionando igual (replace completo, limpia el stack)
- `Update()` y `Draw()` — cuando hay stack: dibuja desde abajo hacia arriba, solo el tope recibe Update completo

---

## FASE 4 — Infraestructura y Plataforma ✅ COMPLETADA

> **Objetivo:** Localización multi-idioma, gestión de plataforma/resolución.

### Milestone 4.1 — Localization ✅ COMPLETADO

**`Localization/IStringLocalizer.cs`** — interfaz
- `string this[string key] { get; }` — acceso directo
- `string Get(string key, params object[] args)` — con format arguments

**`Localization/LocalizationManager.cs`** — `sealed class LocalizationManager : IStringLocalizer`
- `_strings` (Dictionary<string, string> pre-allocated)
- `CurrentCulture` (string, e.g. `"es"`, `"en"`)
- `LoadLanguage(string culture)` — carga `{culture}.json` via `TitleContainer.OpenStream`, deserializa con `System.Text.Json`, no-alloc en hot path
- `event Action? CultureChanged` — notifica cambio de idioma
- `this[string key]` — lookup con fallback a key si no existe
- `Get(string key, params object[] args)` — `string.Format` (aceptable fuera del game loop)

Integración en `Core.cs`: añadir `Localization` como propiedad estática.

---

### Milestone 4.2 — Platform Manager ✅ COMPLETADO

**`Platform/PlatformManager.cs`** — `sealed class PlatformManager`
- `CurrentPlatform` (enum: Desktop, Mobile, Console) — detectado en constructor via `#if` y `Environment.OSVersion`
- `IsDesktop`, `IsMobile`, `IsConsole` — bool shortcuts
- `VirtualWidth`, `VirtualHeight` — delega a `ResolutionManager`
- Suscripción a `Game.Window.ClientSizeChanged` → dispara `event Action? ScreenResized`
- `SupportedOrientations` (DisplayOrientation) — para móvil
- Suscripción a `Game.Deactivated` → dispara `event Action? AppPaused` para auto-save

---

## FASE 5 — Sistema UI Completo ✅ COMPLETADO

> **Objetivo:** UI production-ready con layout, interacción, focus y todos los controles.

### Milestone 5.1 — UI Core Refactor ✅ COMPLETADO

**`UI/UIElement.cs`** — **REFACTORIZAR**
- Añadir: `Rectangle Bounds` (posición y tamaño en screen-space)
- Añadir: `float Opacity` (float 0–1, multiplicado en Draw)
- Añadir: `bool IsEnabled` (controla interacción, no visibilidad)
- Añadir: `UIElement? Parent` (referencia al padre, set interno)
- Añadir: `bool _layoutDirty` flag + `Invalidate()` — propaga hacia arriba hasta el root
- Añadir: `virtual void Measure(Vector2 availableSize)` → calcula `DesiredSize`
- Añadir: `virtual void Arrange(Rectangle finalBounds)` → establece `Bounds`
- `DesiredSize` (Vector2, resultado de Measure)

**`UI/UIContainer.cs`** — **MODIFICAR**
- Override de `Measure`: itera hijos (for indexado) y calcula tamaño contenedor
- Override de `Arrange`: asigna Bounds a hijos
- `Add(UIElement child)` → setea `child.Parent = this`, llama `Invalidate()`
- `Remove(UIElement child)` → limpia `child.Parent`, llama `Invalidate()`

---

### Milestone 5.2 — UI Interaction ✅ COMPLETADO

**`UI/Interaction/UIPointerEventArgs.cs`** — `readonly struct UIPointerEventArgs`
- `Point Position` — posición en screen-space
- `MouseButton Button` — qué botón (usa el enum existente)
- `bool Handled` — flag para consumir el evento (ref parameter en handlers)

**`UI/Interaction/IUIInteractable.cs`** — interfaz
- `void OnPointerDown(ref UIPointerEventArgs args)`
- `void OnPointerUp(ref UIPointerEventArgs args)`
- `void OnPointerEnter()`
- `void OnPointerLeave()`
- `bool IsHovered { get; }` — estado hover actual

**`UI/Interaction/UIInteractionManager.cs`** — `sealed class UIInteractionManager`
- `_hoveredElement` (UIElement? field)
- `Update(UIRoot root, MouseInfo mouse)` — hit testing DFS reverso (último dibujado = primero testeado)
- Hit test: recorre árbol en reversa, comprueba `Bounds.Contains(mousePos)` y `IsVisible && IsEnabled`
- Genera eventos OnPointerEnter/Leave en cambios de hover
- Genera OnPointerDown/Up en clicks con event bubbling (sube por `Parent` hasta que `Handled = true`)

---

### Milestone 5.3 — UI Focus System ✅ COMPLETADO

**`UI/Focus/IFocusable.cs`** — interfaz
- `int TabIndex { get; }` — orden de navegación Tab
- `int? FocusNeighborUp/Down/Left/Right { get; }` — IDs para D-Pad
- `void OnFocusGained()`
- `void OnFocusLost()`
- `bool IsFocused { get; }`

**`UI/Focus/UIFocusManager.cs`** — `sealed class UIFocusManager`
- `_focusables` (List<IFocusable> pre-allocated)
- `_focused` (IFocusable? field)
- `Register(IFocusable element)`, `Unregister(IFocusable element)`
- `FocusNext()`, `FocusPrevious()` — por TabIndex
- `FocusUp/Down/Left/Right()` — por neighbor IDs
- `SetFocus(IFocusable element)` — llama OnFocusLost en anterior, OnFocusGained en nuevo
- `Update(KeyboardInfo kb, GamePadInfo pad)` — gestiona Tab y D-Pad

---

### Milestone 5.4 — UI Layout System ✅ COMPLETADO

Todos los layouts heredan de `UIContainer` y overridean `Measure` y `Arrange`.  
**Regla crítica:** Sin LINQ. Solo `for` indexado en todos los métodos de layout.

**`UI/Layout/StackPanel.cs`** — `sealed class StackPanel : UIContainer`
- `Orientation` (enum: Horizontal, Vertical)
- `Spacing` (float) — gap entre hijos
- Measure: suma dimensiones del eje principal + máximo del eje secundario
- Arrange: distribuye hijos secuencialmente con spacing

**`UI/Layout/Canvas.cs`** — `sealed class Canvas : UIContainer`
- Posición de cada hijo via `Canvas.SetOffset(child, Vector2 offset)` — stored en Dictionary<UIElement, Vector2> pre-allocated
- Arrange: posiciona cada hijo en `ParentBounds.Location + offset`

**`UI/Layout/FlowLayoutPanel.cs`** — `sealed class FlowLayoutPanel : UIContainer`
- Orientación principal Horizontal (wrap en eje vertical)
- `Spacing` (float) — gap horizontal y vertical
- Measure: simula el wrap para calcular altura total
- Arrange: asigna posiciones haciendo wrap cuando supera el ancho disponible

**`UI/Layout/AnchorLayout.cs`** — `sealed class AnchorLayout : UIContainer`
- Enum `Anchor`: TopLeft, TopCenter, TopRight, MiddleLeft, Center, MiddleRight, BottomLeft, BottomCenter, BottomRight, Fill
- `SetAnchor(UIElement child, Anchor anchor, Vector2 offset)` — stored en Dictionary
- Arrange: calcula posición según anchor + offset relativo a `Bounds`

**`UI/Layout/GridLayout.cs`** — `sealed class GridLayout : UIContainer`
- `ColumnDefinitions` (List<GridTrack>), `RowDefinitions` (List<GridTrack>)
- `struct GridTrack` con `SizeMode` (Fixed/Auto/Star) y `Value` (float)
- `SetCell(UIElement child, int row, int col, int rowSpan = 1, int colSpan = 1)` — stored en Dictionary
- `CellAlignment` (HAlign + VAlign enums)
- Measure: auto cols primero → auto rows → distribuye stars
- Arrange: calcula rectangles de cada celda → arranja hijos con span

---

### Milestone 5.5 — UI Overlay Manager ✅ COMPLETADO

**`UI/UIOverlayManager.cs`** — `sealed class UIOverlayManager`
- `_overlays` (List<UIElement> pre-allocated)
- `Show(UIElement overlay)`, `Hide(UIElement overlay)`
- `Update(...)`, `Draw(SpriteBatch sb)` — dibuja sobre el UIRoot
- Usado por Dropdown (lista desplegada), Tooltip
- Integración: `UIRoot.DrawAll()` llama `_overlayManager.Draw()` al final

---

### Milestone 5.6 — UI Controls Básicos ✅ COMPLETADO

**`UI/Controls/Label.cs`** — `sealed class Label : UIElement`
- `Text` (string), `Font` (SpriteFont), `Color` (Color)
- `HAlign` (enum: Left/Center/Right), `VAlign` (enum: Top/Middle/Bottom)
- `WrapText` (bool) — activa word wrap si true
- Measure: mide el string con `Font.MeasureString`

**`UI/Controls/Button.cs`** — `sealed class Button : UIElement, IUIInteractable, IFocusable`
- Estados internos: Normal/Hovered/Pressed (enum, no alloc)
- `_scaleAnim` (float field) — animación de pulse en hover (sin Tween, manual lerp)
- `event Action? Clicked` — disparado en OnPointerUp si IsHovered
- `Text` (string), `Font`, colores por estado (pre-allocated array de Color[3])

**`UI/Controls/Panel.cs`** — `sealed class Panel : UIContainer`
- `BackgroundColor` (Color), `BorderColor` (Color), `BorderThickness` (int)
- `NineSliceTexture` (Texture2D?)` — si no null, usa 9-slice para el fondo
- Usa `DrawHelper.DrawRect` y `DrawHelper.DrawBorder`

**`UI/Controls/ProgressBar.cs`** — `sealed class ProgressBar : UIElement`
- `Value` (float 0–1), `FillColor` (Color), `BackgroundColor` (Color)
- `Orientation` (Horizontal/Vertical)
- `ColorGradient` (bool) — si true, interpola entre `LowColor` y `HighColor` según Value

**`UI/Controls/ScrollView.cs`** — `sealed class ScrollView : UIContainer`
- `_scrollOffset` (Vector2 field)
- `ContentSize` (Vector2) — tamaño total del contenido scrolleable
- `Draw`: establece ScissorRectangle antes de dibujar hijos, restaura después
- Captura scroll wheel en Update para desplazar `_scrollOffset`

**`UI/Controls/UISprite.cs`** — `sealed class UISprite : UIElement`
- `Texture` (Texture2D), `Color` (Color)
- `DrawMode` (enum: Stretch/Fit/Crop/Tile)
- Calcula el sourceRect en Draw según DrawMode

**`UI/Controls/Checkbox.cs`** — `sealed class Checkbox : UIElement, IUIInteractable, IFocusable`
- `IsChecked` (bool), `Label` (string), `Font` (SpriteFont)
- `event Action<bool>? CheckedChanged`
- Visual: cuadrado + check mark (o solo cuadrado relleno)

**`UI/Controls/Tooltip.cs`** — `sealed class Tooltip : UIElement`
- `Text` (string), `Font` (SpriteFont)
- `Show(Vector2 anchorPos)` — calcula posición con screen-edge clamping
- `Hide()`
- Registrado en `UIOverlayManager`

---

### Milestone 5.7 — UI Controls de Entrada ✅ COMPLETADO

**`UI/Controls/Slider.cs`** — `sealed class Slider : UIElement, IUIInteractable, IFocusable`
- `MinValue`, `MaxValue`, `Value` (float)
- `Step` (float, 0 = continuo, >0 = snap a múltiplos)
- `Orientation` (Horizontal/Vertical)
- `_isDragging` (bool field), `_thumbRect` (Rectangle field)
- Hit area extendida del thumb (+8px cada lado, const)
- Keyboard/Gamepad: incrementa/decrementa `Step` (o 1% del rango si Step=0)
- `event Action<float>? ValueChanged`

**`UI/Controls/TextBox.cs`** — `sealed class TextBox : UIElement, IUIInteractable, IFocusable`
- `Text` (string), `Placeholder` (string)
- `_cursorIndex` (int field), `_selectionStart` (int field)
- `_blinkTimer` (float field) — cursor blink sin alloc
- Usa `Window.TextInput` event (suscripción en OnFocusGained, desuscripción en OnFocusLost)
- Soporta: Backspace, Delete, Home, End, flechas izquierda/derecha, Shift+flechas (selección)
- `MaxLength` (int, -1 = sin límite)
- `event Action<string>? TextChanged`

**`UI/Controls/NumericBox.cs`** — `sealed class NumericBox : UIElement, IUIInteractable, IFocusable`
- Hereda lógica de TextBox internamente (composición)
- Filtra: solo dígitos, punto decimal opcional, signo opcional
- `MinValue`, `MaxValue` (float) — clamp al perder foco
- `DecimalPlaces` (int)

**`UI/Controls/PasswordBox.cs`** — `sealed class PasswordBox : UIElement, IUIInteractable, IFocusable`
- Hereda lógica de TextBox internamente
- `MaskChar` (char, default `'•'`) — todo el texto renderizado enmascarado

**`UI/Controls/TextArea.cs`** — `sealed class TextArea : UIElement, IUIInteractable, IFocusable`
- `_lines` (List<string> pre-allocated en constructor)
- Multiline con Enter para nueva línea
- Word wrap automático según `Bounds.Width`
- Scroll vertical interno (sin ScrollView externo)
- `MaxLines` (int, -1 = sin límite)

---

### Milestone 5.8 — UI Controls Avanzados ✅ COMPLETADO

**`UI/Controls/Dropdown.cs`** — `sealed class Dropdown : UIElement, IUIInteractable, IFocusable`
- `_options` (List<string> pre-allocated), `SelectedIndex` (int)
- `_isExpanded` (bool field)
- `_overlayList` (Panel con items) registrado en `UIOverlayManager`
- Screen-boundary flip: si al expandir se sale del fondo, lista se abre hacia arriba
- Click fuera: `UIInteractionManager` notifica a Dropdown via `OnPointerDown` no-handled
- Keyboard: Up/Down destaca opción, Enter selecciona, Escape cierra
- `event Action<int>? SelectionChanged`

**`UI/Controls/RadioButton.cs`** — `sealed class RadioButton : UIElement, IUIInteractable, IFocusable`
- `IsSelected` (bool, read-only desde fuera; set por `RadioGroup`)
- `GroupId` (string) — vincula con un `RadioGroup`
- Visual: círculo exterior + círculo interior relleno si Selected

**`UI/Controls/RadioGroup.cs`** — `sealed class RadioGroup`
- `_buttons` (List<RadioButton> pre-allocated)
- `Register(RadioButton button)`, `Unregister(RadioButton button)`
- `Select(RadioButton button)` — deselecciona todos, selecciona el indicado
- `SelectedIndex` (int), `SelectedButton` (RadioButton?)
- Teclado: Up/Down dentro del grupo, Tab sale del grupo (solo el selected tiene TabIndex activo)
- `event Action<int>? SelectionChanged`

**`UI/Controls/ColorPickerRGB.cs`** — `sealed class ColorPickerRGB : UIContainer`
- Tres Slider internos (R 0–255, G 0–255, B 0–255)
- `_preview` (Panel field) — swatch de color resultante
- `SelectedColor` (Color property) — construido desde los tres sliders
- `HexInput` (TextBox) — acepta `#RRGGBB`, valida y sincroniza sliders
- `event Action<Color>? ColorChanged`
- `event Action<Color>? ColorCommitted` — disparado al soltar slider o confirmar hex

**`UI/Controls/ColorPickerHSV.cs`** — `sealed class ColorPickerHSV : UIContainer`
- `_hueBar` (Slider vertical, 0–360)
- `_svSquare` (UIElement custom con gradiente SV) — requiere `RenderTargetManager` para dibujar el gradiente
- Conversión HSV↔RGB via `ColorPickerUtils` (static helper)
- `HexInput`, preview swatch, `ColorChanged`, `ColorCommitted` — igual que RGB variant

**`UI/Controls/ColorPickerUtils.cs`** — `static class ColorPickerUtils`
- `HsvToRgb(float h, float s, float v)` → Color
- `RgbToHsv(Color color, out float h, out float s, out float v)`
- `HexToColor(string hex)` → Color?
- `ColorToHex(Color color)` → string

---

## FASE 6 — Extended Libraries Integration

> **Objetivo:** Wrappers sobre MonoGame.Extended para Tiled y BitmapFonts.

### Milestone 6.1 — Tiled Map Support ✅ COMPLETADO

**`Graphics/Tiled/TiledMapRenderer.cs`** — `sealed class TiledMapRenderer : IDisposable`
- `Load(ContentManager content, string assetName)` → carga `TiledMap` de MonoGame.Extended
- `Update(GameTime gameTime)` — actualiza animaciones de tiles
- `Draw(Camera2D camera, SpriteBatch spriteBatch)` — renderiza con la transform matrix de la cámara
- `GetLayer(string name)` → `TiledMapLayer?`
- Soporte para layer interleaving (mezclar tiles con sprites del juego por `layerDepth`)

**`Graphics/Tiled/TiledObjectLayer.cs`** — `sealed class TiledObjectLayer`
- `GetObjects(string layerName)` → `IReadOnlyList<TiledMapObject>`
- `GetObjectsByType(string type)` → filtra por custom property "type" (for loop, sin LINQ)
- `GetSpawnPoints()` → lista de Vector2 de objetos marcados como "spawn"
- `GetCollisionRects()` → lista de Rectangle para zonas de colisión

---

### Milestone 6.2 — Bitmap Fonts ✅ COMPLETADO

**`Graphics/Fonts/BitmapFontRenderer.cs`** — `sealed class BitmapFontRenderer`
- `Load(ContentManager content, string assetName)` → carga `BitmapFont` de MonoGame.Extended
- `DrawString(SpriteBatch sb, string text, Vector2 pos, Color color)`
- `DrawString(SpriteBatch sb, string text, Vector2 pos, Color color, float scale, float rotation)`
- `MeasureString(string text)` → Vector2
- `DrawCentered(SpriteBatch sb, string text, Rectangle bounds, Color color)` — helper de centrado

---

## Milestone 6.3 — Input Manager Extendido ✅ COMPLETADO

> Wrapper sobre el sistema de input actual del kernel, añadiendo helpers de alto nivel y soporte para input mapping configurable.

**`Input/InputAction.cs`** — `sealed class InputAction`
- `Name` (string) — identificador de la acción ("Jump", "Fire", "MoveLeft"…)
- `_keyBindings` (Keys[] field, pre-allocated) — teclas asociadas
- `_padBindings` (Buttons[] field, pre-allocated) — botones de gamepad asociados
- `_mouseBindings` (MouseButton[] field, pre-allocated)
- `IsPressed`, `IsReleased`, `IsHeld` (bool, calculados en Update)
- Sin alloc: no usa LINQ, no crea listas en Update

**`Input/InputActionMap.cs`** — `sealed class InputActionMap`
- `_actions` (Dictionary<string, InputAction> pre-allocated)
- `Register(InputAction action)`, `Unregister(string name)`
- `Get(string name)` → `InputAction?`
- `Update(KeyboardState curr, KeyboardState prev, MouseState currM, MouseState prevM, GamePadState currP, GamePadState prevP)` — itera `for` indexado sobre las acciones registradas y actualiza cada una

**`Input/InputManager.cs`** — `sealed class InputManager`
- Gestiona los estados prev/curr de teclado, ratón y gamepad (el patrón del skill)
- `_activeMap` (InputActionMap? field) — mapa activo
- `LoadMap(InputActionMap map)` / `UnloadMap()`
- `Update(GameTime gameTime)` — actualiza todos los estados y propaga al mapa activo
- Helpers de conveniencia delegados: `IsKeyPressed(Keys)`, `IsKeyHeld(Keys)`, `IsKeyReleased(Keys)`, `MousePosition` (Vector2 field, no `new` inline)
- Integración en `Core.cs`: `public static InputManager Input { get; }` expuesto como propiedad estática

**`Input/InputBinding.cs`** — `readonly struct InputBinding`
- Representa un binding serializable: `DeviceType` (enum: Keyboard/Mouse/Gamepad), `Code` (int, cast desde Keys/Buttons/MouseButton)
- `ToDisplayString()` → string legible ("Space", "A (Gamepad)", …)

**`Input/InputSerializer.cs`** — `sealed class InputSerializer`
- `Save(InputActionMap map, string filePath)` — serializa a JSON con `System.Text.Json` (async, `Task`)
- `Load(string filePath)` → `Task<InputActionMap>` — deserializa en background, sin tocar GraphicsDevice
- Patrón del skill async: el caller debe aplicar el resultado en el main thread

---

## Milestone 6.4 — Async Content Loading ✅ COMPLETADO

> Infraestructura para carga asíncrona de assets con pantalla de carga, progreso y cancelación.

**`Content/AsyncContentLoader.cs`** — `sealed class AsyncContentLoader : IDisposable`
- `_pendingAssets` (Queue<string> pre-allocated) — cola de assets pendientes de cargar en main thread
- `_cancelSource` (CancellationTokenSource field)
- `LoadAsync<T>(string assetName, IProgress<float>? progress, CancellationToken ct)` → `Task<T>`
  - Fases: lectura de bytes en background (`Task.Run`) → enqueue del asset name → `Content.Load<T>()` en main thread
  - Patrón del skill: **nunca llama `Content.Load<T>()` desde el body de `Task.Run`**
- `FlushPending(ContentManager content)` — llamado desde `Update()` en main thread; procesa la cola de pendientes (máx. N assets por frame, configurable)
- `Cancel()` — cancela operaciones en curso vía `CancellationTokenSource`
- `Dispose()` — limpia `CancellationTokenSource`

**`Content/ContentLoadGroup.cs`** — `sealed class ContentLoadGroup`
- `_assetNames` (List<string> pre-allocated)
- `Add(string assetName)` — registra un asset en el grupo
- `LoadAllAsync(AsyncContentLoader loader, IProgress<float>? progress, CancellationToken ct)` → `Task`
  - Progreso: `(assetsCompleted / totalAssets)` reportado vía `IProgress<float>`
  - Sin bloqueo: itera con `for` indexado, `await` cada `LoadAsync` individualmente

**`Scenes/LoadingScene.cs`** — `sealed class LoadingScene : Scene`
- `_loader` (AsyncContentLoader field)
- `_group` (ContentLoadGroup field)
- `_progress` (float field, 0–1)
- `Configure(ContentLoadGroup group, Scene nextScene)` — setup antes de iniciar
- `Initialize()` — arranca `group.LoadAllAsync(...)` con `Progress<float>` que actualiza `_progress`; sigue el patrón: `_loadTask = Task.Run(...)`, no bloquea
- `Update()` — llama `_loader.FlushPending(Content)` (main thread GPU upload); comprueba `_loadTask.IsCompleted` sin `.Wait()` ni `.Result` bloqueante; cuando completa hace `SceneManager.RequestChange(nextScene)`
- `Draw()` — renderiza barra de progreso usando `ProgressBar` del sistema UI o `DrawHelper` directamente

**`Content/ContentGroupBuilder.cs`** — `sealed class ContentGroupBuilder`
- Fluent API para construir un `ContentLoadGroup`:
  - `Add(string assetName)` → `ContentGroupBuilder`
  - `AddRange(IEnumerable<string> names)` → `ContentGroupBuilder` (solo fuera del game loop)
  - `Build()` → `ContentLoadGroup`

**Tests** (`UnitTests/Content/AsyncContentLoaderTests.cs`):
- Verifican que `FlushPending` no llama a `Content.Load<T>()` si la cola está vacía
- Verifican que `Cancel()` aborta el `CancellationTokenSource`
- Verifican progreso incremental en `ContentLoadGroup` (mock de `IProgress<float>`)

## Integración en Core.cs

Tras todas las fases, `Core.cs` debe exponer los nuevos managers como propiedades estáticas:

```csharp
// Nuevas propiedades estáticas en Core
public static ResolutionManager Resolution { get; }
public static TweeningManager Tweening { get; }
public static LocalizationManager Localization { get; }
public static PlatformManager Platform { get; }
public static UIFocusManager UIFocus { get; }
public static UIInteractionManager UIInteraction { get; }
public static UIOverlayManager UIOverlay { get; }
```

Todos inicializados en `Initialize()` de Core.

---

## Estructura Final de Carpetas

```
src/Library/Alca.MonoGame.Kernel/
├── Audio/
│   ├── AudioController.cs         (modificar)
│   ├── AudioEmitter3D.cs          (nuevo)
│   ├── AudioListener3D.cs         (nuevo)
│   └── SoundEffectPool.cs         (nuevo)
├── ECS/
│   ├── EntityPool.cs              (nuevo)
│   ├── GameBehaviour.cs
│   ├── GameEntity.cs              (modificar: tags)
│   └── GameWorld.cs               (modificar: tag queries)
├── Events/
│   └── EventBus.cs
├── Graphics/
│   ├── Camera/
│   │   ├── Camera2D.cs
│   │   ├── Camera3D.cs
│   │   ├── FixedCamera3D.cs
│   │   ├── FirstPersonCamera3D.cs
│   │   ├── ThirdPersonCamera3D.cs
│   │   └── TopDownCamera3D.cs
│   ├── Effects/
│   │   ├── PostProcessEffect.cs
│   │   └── RenderTargetManager.cs
│   ├── Fonts/
│   │   └── BitmapFontRenderer.cs
│   ├── Models/                    (existentes sin cambios)
│   ├── Particles/
│   │   ├── ParticleBuilder.cs
│   │   └── ParticleEffectWrapper.cs
│   ├── Shaders/
│   │   ├── Material.cs
│   │   └── SpriteMaterial.cs
│   ├── ThreeD/
│   │   ├── MeshRenderer.cs
│   │   └── PrimitiveBatch.cs
│   ├── Tiled/
│   │   ├── TiledMapRenderer.cs
│   │   └── TiledObjectLayer.cs
│   ├── DrawHelper.cs
│   └── ResolutionManager.cs
├── Input/                         (existentes sin cambios)
├── Localization/
│   ├── IStringLocalizer.cs
│   └── LocalizationManager.cs
├── Mathematics/
│   ├── BoundingHelpers.cs
│   └── MathUtils.cs
├── Platform/
│   └── PlatformManager.cs
├── Scenes/
│   ├── Scene.cs                   (modificar: IsOverlay)
│   └── SceneManager.cs            (modificar: scene stack)
├── Tweening/
│   ├── EasingCatalog.cs
│   └── TweeningManager.cs
├── UI/
│   ├── Controls/
│   │   ├── Button.cs
│   │   ├── Checkbox.cs
│   │   ├── ColorPickerHSV.cs
│   │   ├── ColorPickerRGB.cs
│   │   ├── ColorPickerUtils.cs
│   │   ├── Dropdown.cs
│   │   ├── Label.cs
│   │   ├── NumericBox.cs
│   │   ├── PasswordBox.cs
│   │   ├── Panel.cs
│   │   ├── ProgressBar.cs
│   │   ├── RadioButton.cs
│   │   ├── RadioGroup.cs
│   │   ├── ScrollView.cs
│   │   ├── Slider.cs
│   │   ├── TextArea.cs
│   │   ├── TextBox.cs
│   │   └── UISprite.cs
│   ├── Focus/
│   │   ├── IFocusable.cs
│   │   └── UIFocusManager.cs
│   ├── Interaction/
│   │   ├── IUIInteractable.cs
│   │   ├── UIInteractionManager.cs
│   │   └── UIPointerEventArgs.cs
│   ├── Layout/
│   │   ├── AnchorLayout.cs
│   │   ├── Canvas.cs
│   │   ├── FlowLayoutPanel.cs
│   │   ├── GridLayout.cs
│   │   └── StackPanel.cs
│   ├── UIContainer.cs             (modificar)
│   ├── UIElement.cs               (refactorizar)
│   ├── UIOverlayManager.cs
│   └── UIRoot.cs
├── Core.cs                        (modificar: nuevas propiedades)
├── Globals.cs
└── Alca.MonoGame.Kernel.csproj    (añadir NuGet packages)
```

**Total:** ~75 archivos (16 existentes modificados, ~59 nuevos)

---

## Resumen del Roadmap

| Fase | Milestones | Archivos | Dependencias |
|------|-----------|----------|--------------|
| **0 — NuGet** | Añadir MonoGame.Extended | 1 (.csproj) | — |
| **1 — Fundamentos** | Math, Camera2D, Camera3D×5, Resolution | 9 | — |
| **2 — Rendering** | RenderTargets, Material/Shaders, 3D | 6 | Fase 1 |
| **3 — Game Core** | Particles, Tweening, Audio 3D, ECS+, Scenes+ | 12 | Fase 1 |
| **4 — Plataforma** | Localization, Platform | 3 | Fase 1 |
| **5 — UI** | Core refactor, Interaction, Focus, 5 layouts, 19 controles, Overlay | 35 | Fases 1–4 |
| **6 — Extended** | Tiled wrapper, BitmapFont wrapper | 3 | Fase 1 |

---

## Verificación por Fase

- **Fase 1:** `Camera2D.GetTransformMatrix()` se pasa a `SpriteBatch.Begin` y el mundo se renderiza correctamente. `ResolutionManager.ScreenToVirtual()` remapea la posición del ratón.
- **Fase 2:** `RenderTargetManager.Apply(effect)` produce un post-proceso visible. `PrimitiveBatch` dibuja wireframes sin GC allocation (verificar con VS Diagnostic Tools).
- **Fase 3:** 100 llamadas a `SoundEffectPool.Play()` sin `InstancePlayLimitException`. `TweeningManager.TweenTo()` anima sin alloc en Update. Scene stack: `PushScene` → la escena anterior sigue visible debajo.
- **Fase 4:** `LocalizationManager.LoadLanguage("es")` + `GetString("key")` devuelve texto correcto. Cambio a `"en"` dispara `CultureChanged`.
- **Fase 5:** UIRoot → GridLayout (3×2) → Buttons + Label + Slider. Tab navega entre elementos. Dropdown se abre, flipa si está abajo de pantalla. TextBox acepta input vía `Window.TextInput`.
- **Fase 6:** `TiledMapRenderer` renderiza un `.tmx` correctamente con `Camera2D`. `TiledObjectLayer.GetSpawnPoints()` devuelve las posiciones de objetos marcados.

---

## Referencia Técnica del Proyecto

> Esta sección documenta la estructura fija del repo para que no sea necesario explorarla en cada sesión.

### Solución y proyectos

```
src/Library/
├── Alca.MonoGame.Kernel/                  ← librería principal
│   └── Alca.MonoGame.Kernel.csproj
└── Alca.MonoGame.Kernel.UnitTests/        ← tests xUnit
    └── Alca.MonoGame.Kernel.UnitTests.csproj
```

### Dependencias NuGet (Kernel)

| Paquete | Versión | Notas |
|---------|---------|-------|
| `MonoGame.Framework.DesktopGL` | 3.8.* | `PrivateAssets=All` — no se propaga al consumidor |
| `MonoGame.Extended` | 6.0.* | Incluye Particles, Tweening, Tiled, BitmapFonts |
| `Microsoft.Extensions.DependencyInjection` | 10.0.* | DI container |

### Global Usings

**Kernel** (`Globals.cs`):
```csharp
global using Microsoft.Xna.Framework;
global using Microsoft.Xna.Framework.Audio;
global using Microsoft.Xna.Framework.Content;
global using Microsoft.Xna.Framework.Graphics;
global using Microsoft.Xna.Framework.Input;
global using Microsoft.Xna.Framework.Media;
global using Microsoft.Extensions.DependencyInjection;
```

### Notas de API críticas

**Tweener.TweenTo constraint:** El método genérico `Tweener.TweenTo<TTarget, TMember>` de MonoGame.Extended impone `where TTarget : class`. Cualquier wrapper que exponga este método debe propagar la misma restricción:
```csharp
public Tween TweenTo<T>(T target, ...) where T : class { ... }
```

**Easing functions:** En MonoGame.Extended 6.0 no existe el delegado `EasingFunction`. Las funciones de easing son métodos estáticos en `EasingFunctions` con firma `float Method(float value)`, compatibles con `Func<float, float>`.

**ParticleEmitter constructors:** `ParticleEmitter()` y `ParticleEmitter(int initialCapacity)`. La `TextureRegion` (`Texture2DRegion`) es una propiedad, no un parámetro de constructor — puede ser `null` en tests sin crash.