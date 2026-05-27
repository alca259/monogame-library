# Roadmap Demo: Proyecto Alca.MonoGame.Demo

> Especificación completa del proyecto ejecutable para probar visualmente los sistemas de la librería.
>
> **Referencia cruzada:** Este documento describe únicamente el proyecto Demo. El roadmap de la librería principal está en [`roadmap-v2.md`](roadmap-v2.md).

---

## Proyecto Demo

**Reglas transversales a todos los desarrollos:**
- Al terminar, actualizar este fichero marcando los TODOs completados.

**`src/Demo/Alca.MonoGame.Demo/`** — proyecto ejecutable (`OutputType=WinExe`)
- `DemoGame.cs` — `sealed class DemoGame : Core`
- `UIScene_Menu` — menú principal de navegación; punto de entrada de la aplicación
- 41 escenas de demostración accesibles desde el menú (UI + ECS + Audio + Input + Gráficos + Física + Naveg. + Redes + ...)
- Cada escena expone un botón `← Menú` que vuelve a `UIScene_Menu`
- Añadir escenas nuevas conforme se completen nuevas fases

**Para añadir fuentes al demo:**
1. Crear `Content/DefaultFont.spritefont` con un SpriteFont de MonoGame Content Builder
2. Compilar el `.xnb` con MGCB y copiar a `Content/`
3. Ejecutar el proyecto para ver las escenas renderizadas

---

## Estructura de carpetas

```
src/Demo/Alca.MonoGame.Demo/
├── Scenes/
│   ├── UIScene_Menu.cs               [OK]
│   ├── UIScene_BasicControls.cs      [OK]
│   ├── UIScene_InputText.cs          [OK]
│   ├── UIScene_TextArea.cs           [OK]
│   ├── UIScene_Sliders.cs            [OK]
│   ├── UIScene_Selection.cs          [OK]
│   ├── UIScene_ColorPicker.cs        [OK]
│   ├── UIScene_Layout.cs             [OK]
│   ├── UIScene_ScrollView.cs         [OK]
│   ├── UIScene_Tooltip.cs            [OK]
│   ├── UIScene_Focus.cs              [OK]
│   ├── UIScene_Transitions.cs        [OK]
│   ├── EcsDemoScene.cs               [OK]
│   ├── Camera2DScene.cs              [OK]
│   ├── Physics2DScene.cs             [OK]
│   ├── NavigationScene.cs            [OK]
│   ├── AudioBasicScene.cs            [OK]
│   ├── AudioSpatialScene.cs          [TODO]
│   ├── AudioAdvancedScene.cs         [TODO]
│   ├── InputScene.cs                 [OK]
│   ├── AnimationScene.cs             [OK]
│   ├── Camera3DScene.cs              [TODO]
│   ├── ParticlesScene.cs             [OK]
│   ├── SpriteMaterialScene.cs        [TODO]
│   ├── PostProcessScene.cs           [TODO]
│   ├── ResolutionScene.cs            [TODO]
│   ├── TiledMapScene.cs              [TODO]
│   ├── BitmapFontScene.cs            [TODO]
│   ├── Physics2DJointsScene.cs       [TODO]
│   ├── SteeringScene.cs              [TODO]
│   ├── EntityPoolScene.cs            [TODO]
│   ├── EventBusScene.cs              [OK]
│   ├── StateMachineScene.cs          [OK]
│   ├── TimersScene.cs                [OK]
│   ├── TweeningScene.cs              [OK]
│   ├── DebugScene.cs                 [TODO]
│   ├── PersistenceScene.cs           [TODO]
│   ├── LocalizationScene.cs          [TODO]
│   ├── AsyncContentScene.cs          [TODO]
│   ├── LightingScene.cs              [TODO]
│   ├── NetworkingScene.cs            [TODO]
│   └── PlatformScene.cs              [OK]
├── DemoGame.cs
├── Globals.cs
├── Program.cs
└── Alca.MonoGame.Demo.csproj
```

**`DemoGame.cs`:**
- Registrar todas las escenas en `ConfigureServices` como `AddTransient`.
- Escenas ya registradas (01–16, 19, 20, 22, 31–34, 41): `UIScene_Menu`, `UIScene_BasicControls`, `UIScene_InputText`,
  `UIScene_TextArea`, `UIScene_Sliders`, `UIScene_Selection`, `UIScene_ColorPicker`,
  `UIScene_Layout`, `UIScene_ScrollView`, `UIScene_Tooltip`, `UIScene_Focus`,
  `UIScene_Transitions`, `EcsDemoScene`, `Camera2DScene`, `Physics2DScene`, `NavigationScene`,
  `AudioBasicScene`, `InputScene`, `AnimationScene`, `ParticlesScene`, `EventBusScene`,
  `StateMachineScene`, `TimersScene`, `TweeningScene`, `PlatformScene`.
- Escenas pendientes de registrar (17–18, 21, 23–30, 35–40): `AudioSpatialScene`, `AudioAdvancedScene`,
  `AudioAdvancedScene`, `InputScene`, `AnimationScene`, `Camera3DScene`, `ParticlesScene`,
  `SpriteMaterialScene`, `PostProcessScene`, `ResolutionScene`, `TiledMapScene`,
  `BitmapFontScene`, `Physics2DJointsScene`, `SteeringScene`, `EntityPoolScene`,
  `EventBusScene`, `StateMachineScene`, `TimersScene`, `TweeningScene`, `DebugScene`,
  `PersistenceScene`, `LocalizationScene`, `AsyncContentScene`, `LightingScene`,
  `NetworkingScene`, `PlatformScene`.
- `PostInitialize` arranca en `UIScene_Menu`.

---

## Dependencias de contenido

Algunas escenas requieren assets en `Content/` adicionales a `DefaultFont.spritefont`:

| Asset | Escenas que lo usan | Formato |
|-------|--------------------|---------| 
| `SFX/beep.wav` | AudioBasicScene, AudioSpatialScene, AudioAdvancedScene | WAV/OGG |
| `SFX/ambient.ogg` | AudioSpatialScene | WAV/OGG |
| `Sprites/character_sheet.png` + `.xml` | AnimationScene | TextureAtlas |
| `Fonts/DefaultBitmapFont.fnt` + `.png` | BitmapFontScene | MonoGame.Extended BitmapFont |
| `Maps/demo.tmx` + tilesets | TiledMapScene | Tiled JSON/XML |
| `Shaders/SpriteTint.fx` | SpriteMaterialScene | MGFX shader |
| `Shaders/Vignette.fx` | PostProcessScene | MGFX shader |

---

## Infraestructura común en cada demo

Cada escena declara como mínimo:
```csharp
private readonly UIRoot _uiRoot = new();
private readonly UIInteractionManager _interactionManager = new();
private Texture2D _pixel = null!;
private SpriteFont _font = null!;
```

Patrón del botón "← Menú" en `BuildUI`:
```csharp
var backBtn = new Button(_font, "← Menú") { BackgroundPixel = _pixel };
backBtn.Clicked += () => Core.SceneManager.RequestChange(Core.GetService<UIScene_Menu>());
controls.Add(backBtn);
```

---

## UI Demo Scenes (01–11)

### Menu — `UIScene_Menu.cs` [OK]

> Hub de navegación. Punto de entrada de la aplicación.

**Layout:** `AnchorLayout` centrado; `ScrollView` (700×500 px) con `StackPanel` vertical de botones.

| Elemento | Configuración |
|----------|--------------|
| `Label` título | "MonoGame UI Demo — Selecciona una escena" — `Color.Yellow`, `HAlign.Center` |
| 41 `Button` | Uno por escena, texto numerado, `HAlign.Left` |
| `ScrollView` | 700×500 px |

---

### Scene 01 — `UIScene_BasicControls.cs` [OK]

> Controles: **Button**, **Label**, **Checkbox**, **Panel**

---

### Scene 02 — `UIScene_InputText.cs` [OK]

> Controles: **TextBox**, **NumericBox**, **PasswordBox**

---

### Scene 03 — `UIScene_TextArea.cs` [OK]

> Controles: **TextArea**

---

### Scene 04 — `UIScene_Sliders.cs` [OK]

> Controles: **Slider** (H y V), **ProgressBar** (H y V, gradiente)

---

### Scene 05 — `UIScene_Selection.cs` [OK]

> Controles: **Dropdown**, **RadioGroup** / **RadioButton**

---

### Scene 06 — `UIScene_ColorPicker.cs` [OK]

> Controles: **ColorPickerRGB**, **ColorPickerHSV**

---

### Scene 07 — `UIScene_Layout.cs` [OK]

> Controles: **StackPanel**, **FlowLayoutPanel**, **GridLayout**, **AnchorLayout**, **Canvas**

---

### Scene 08 — `UIScene_ScrollView.cs` [OK]

> Controles: **ScrollView** (vertical y horizontal)

---

### Scene 09 — `UIScene_Tooltip.cs` [OK]

> Controles: **Tooltip**, **UISprite**, **UIOverlayManager**

---

### Scene 10 — `UIScene_Focus.cs` [OK]

> Controles: **UIFocusManager**, navegación Tab + flechas

---

### Scene 11 — `UIScene_Transitions.cs` [OK]

> Sistemas: **UITransitionManager**, **UITweenExtensions** (FadeIn/Out, SlideIn/Out ×4)

---

## ECS + Cámara + Física + Navegación (12–15)

### Scene 12 — `EcsDemoScene.cs` [OK]

> Sistemas: **GameEntity**, **TransformBehaviour**, **GameWorld**

---

### Scene 13 — `Camera2DScene.cs` [OK]

> Sistemas: **Camera2D**, **CameraEffects** (Shake, ZoomTo, PanTo, Follow)

---

### Scene 14 — `Physics2DScene.cs` [OK]

> Sistemas: **Physics2DWorld**, **RigidBody2D**, **BoxCollider2D**, **CircleCollider2D**, **Physics2DQuery**

---

### Scene 15 — `NavigationScene.cs` [OK]

> Sistemas: **NavGrid**, **Pathfinder**, **NavAgent**, **NavPath**

---

## Audio (16–18)

### Scene 16 — `AudioBasicScene.cs` [OK]

> Sistemas: **AudioController**, **AudioMixer**, **AudioMixerChannel**

**Dependencias de contenido:** `Content/SFX/beep.wav`, `Content/Music/theme.ogg`

**Layout:** `StackPanel` vertical con controles de mezcla.

| Control | Comportamiento |
|---------|---------------|
| `Label` header | "Audio Demo — Mixer & Channels" |
| `Button` "Play SFX" | `AudioController.PlaySfx("SFX/beep")` |
| `Button` "Play Music" | `AudioController.PlayMusic("Music/theme")` |
| `Button` "Stop Music" | `AudioController.StopMusic()` |
| `Slider` "Master Vol" | 0–1 → `AudioMixer.Master.Volume` |
| `Slider` "Music Vol" | 0–1 → `AudioMixer.Music.Volume` |
| `Slider` "SFX Vol" | 0–1 → `AudioMixer.Sfx.Volume` |
| `Label` reactivo | "Canales: Master={v} Music={v} SFX={v}" |

**Notas:**
- `AudioController` se obtiene vía `Core.GetService<AudioController>()` o inyectado en el constructor.
- Los canales se acceden como `AudioMixer.Master`, `AudioMixer.Music`, `AudioMixer.Sfx`.
- Registrar en `DemoGame.ConfigureServices` si `AudioController` no es singleton global.

---

### Scene 17 — `AudioSpatialScene.cs` [TODO]

> Sistemas: **SpatialAudioSource**, **SpatialAudioListener**, **AudioZone**, **AudioEmitter3D**, **AudioListener3D**

**Dependencias de contenido:** `Content/SFX/ambient.ogg`

**Layout:** Vista 2D cenital (pixel art) con listener y source posicionados en pantalla.

| Elemento | Descripción |
|----------|-------------|
| Rectángulo azul 20×20 | Listener — posición del jugador |
| Rectángulo naranja 20×20 | Source — emite sonido en loop |
| `Label` distancia | "Distancia: {d:F0} px — Vol: {v:F2}" |
| `Button` "Mover Source" | Teletransporta la source a posición aleatoria |
| `Button` "Toggle Loop" | Pausa/reanuda el sonido espacial |
| `Slider` "Radio máx" | Controla `SpatialAudioSource.MaxDistance` |

**Notas:**
- Listener sigue al ratón en `Update`.
- El volumen percibido (`1 - distancia/MaxDistance`) se calcula y muestra.
- `AudioZone` dibujado como círculo semitransparente alrededor de la source.

---

### Scene 18 — `AudioAdvancedScene.cs` [TODO]

> Sistemas: **SoundEffectPool**, **AudioCrossfader**

**Dependencias de contenido:** `Content/SFX/beep.wav`, `Content/Music/track_a.ogg`, `Content/Music/track_b.ogg`

**Layout:** Dos columnas.

**Columna Pool:**
- `Label` "SoundEffectPool Demo"
- `Button` "Spawn Sound" → obtiene instancia del pool, reproduce, y la devuelve al pool automáticamente
- `Label` reactivo "Pool: {active}/{capacity} instancias activas"
- `Slider` "Pitch" 0.5–2.0 → pitch de la próxima reproducción

**Columna Crossfader:**
- `Label` "AudioCrossfader Demo"
- `Button` "Fade A→B" → crossfade de track_a a track_b
- `Button` "Fade B→A" → crossfade inverso
- `Slider` "Duración crossfade" 0.5–3.0 s
- `Label` reactivo "Reproduciendo: Track {A/B}"

---

## Input (19)

### Scene 19 — `InputScene.cs` [OK]

> Sistemas: **InputManager**, **InputAction**, **InputActionMap**, **InputBinding**, **InputSerializer**

**Layout:** Dos columnas.

**Columna izquierda — Estado en tiempo real:**
- `Label` "Input Demo — Actions & Rebinding"
- `Label` reactivo teclado: "Tecla pulsada: {key}"
- `Label` reactivo ratón: "Mouse: {x},{y} Btn: {btn}"
- `Label` reactivo acción: "Jump: {pressed} | Fire: {pressed}"
- `Label` "Mantén Space/Click para Fire, W/↑ para Jump"

**Columna derecha — Rebinding:**
- `Label` "Rebinding de acciones"
- `Button` "Rebind Jump" → activa modo escucha; próxima tecla pulsada se asigna a Jump
- `Button` "Rebind Fire" → ídem para Fire
- `Label` reactivo "Jump: {binding} | Fire: {binding}"
- `Button` "Guardar bindings" → `InputSerializer.Save(actionMap, "bindings.json")`
- `Button` "Cargar bindings" → `InputSerializer.Load(actionMap, "bindings.json")`
- `Label` "Estado: Idle / Esperando tecla..."

**Notas:**
- `InputActionMap` con dos `InputAction`: `"Jump"` (Keys.W + Keys.Up) y `"Fire"` (Keys.Space + MouseButton.Left).
- En modo rebinding, capturar la siguiente tecla presionada y crear un nuevo `InputBinding`.
- `InputSerializer.Save/Load` persiste los bindings en disco.

---

## Animación (20)

### Scene 20 — `AnimationScene.cs` [OK]

> Sistemas: **TextureAtlas**, **TextureRegion**, **Sprite**, **AnimatedSprite**, **Animation**,
> **AnimationStateMachine**, **AnimatedSpriteBehaviour**, **AnimationStateMachineBehaviour**

**Dependencias de contenido:** `Content/Sprites/character_sheet.png` + `character_sheet.xml`
(atlas de 4×4 frames de 32×32 px — puede generarse proceduralmente en LoadContent si no existe el asset)

**Layout:** Dos columnas.

**Columna izquierda — Visor del sprite:**
- Sprite animado grande (escala 3×) en el centro
- `Label` "Frame: {n}/{total} — Estado: {state}"
- `Label` "FPS: {fps:F1}"

**Columna derecha — Controles:**
- `Label` "Animation Demo"
- `Dropdown` "Estado:" — Idle, Walk, Run, Attack (los 4 estados de la state machine)
- `Slider` "Velocidad" 0.5–4.0× → `AnimatedSprite.FrameRate`
- `Button` "Play/Pause" → alterna `AnimatedSprite.IsPlaying`
- `Checkbox` "Loop" → `AnimatedSprite.IsLooping`
- `Button` "Reset Frame" → `AnimatedSprite.Reset()`

**Notas:**
- Si no existe el asset, crear el atlas proceduralmente en `LoadContent`:
  4 tiras de 4 frames, cada frame un color distinto (Idle=azul, Walk=verde, Run=naranja, Attack=rojo).
- `AnimationStateMachine` con transición automática Walk→Run si velocidad >2.0.
- Registrar `AnimationStateMachineBehaviour` en la entidad ECS.

---

## Gráficos 3D (21)

### Scene 21 — `Camera3DScene.cs` [TODO]

> Sistemas: **Camera3D**, **FirstPersonCamera3D**, **ThirdPersonCamera3D**, **TopDownCamera3D**,
> **FixedCamera3D**, **PrimitiveBatch**, **MeshRenderer**

**Layout:** Panel de control superpuesto (UI en screen space) + mundo 3D.

**Mundo 3D (PrimitiveBatch):**
- Grid de suelo 10×10 unidades (líneas wireframe)
- Cubo wireframe en el origen
- Esfera/cápsula en posición (0, 0.5, 0) representando al "jugador"

**Panel UI (topleft):**
- `Label` "Camera3D Demo"
- `Dropdown` "Modo cámara:" — First Person, Third Person, Top Down, Fixed
- `Label` reactivo "Pos: {x:F1},{y:F1},{z:F1}"
- `Label` reactivo "Dir: {yaw:F0}° {pitch:F0}°"
- `Label` "WASD: mover | Ratón: rotar (FP/TP) | Scroll: zoom (TP/TD)"

**Notas:**
- `FirstPersonCamera3D`: ratón controla yaw/pitch; WASD mueve la cámara.
- `ThirdPersonCamera3D`: orbita alrededor del target; scroll cambia distancia.
- `TopDownCamera3D`: vista cenital fija en Y; WASD desplaza.
- `FixedCamera3D`: cámara estática en posición predefinida.
- `PrimitiveBatch` se inicializa con `Core.GraphicsDevice`.
- Capturar ratón con `Core.Input.Mouse` para el delta de rotación.

---

## Partículas (22)

### Scene 22 — `ParticlesScene.cs` [OK]

> Sistemas: **ParticleBuilder**, **ParticleEffectWrapper**, **ParticleEmitterBehaviour**

**Layout:** Pantalla de partículas + panel UI esquina inferior izquierda.

**Viewport de partículas:**
- Click izquierdo en pantalla → emite burst de partículas en la posición del cursor
- Emitter continuo en el centro (fuego/nieve según preset seleccionado)

**Panel UI:**
- `Label` "Particles Demo"
- `Dropdown` "Preset:" — Fire, Snow, Explosion, Sparks
- `Slider` "Emisión/s" 10–500
- `Slider` "Vida (s)" 0.5–5.0
- `Slider` "Velocidad" 10–300
- `Button` "Burst aquí" → emite 200 partículas en el centro
- `Button` "Limpiar" → elimina todas las partículas activas
- `Label` reactivo "Partículas activas: {n}"

**Notas:**
- `ParticleBuilder` construye el `ParticleEffect` con los parámetros del panel.
- `ParticleEffectWrapper` actualiza y dibuja el efecto.
- `ParticleEmitterBehaviour` adjunto a una `GameEntity` centrada para el emitter continuo.
- Reconstruir el efecto al cambiar preset/parámetros (botón Apply o en tiempo real).

---

## Shaders y Materiales (23)

### Scene 23 — `SpriteMaterialScene.cs` [TODO]

> Sistemas: **Material**, **SpriteMaterial**

**Dependencias de contenido:** `Content/Shaders/SpriteTint.fx` (shader MGFX básico con parámetro tint + intensity)

**Layout:** Dos columnas.

**Columna izquierda — Visor:**
- Sprite de prueba (64×64 px generado proceduralmente) renderizado con el material activo
- Segundo sprite sin material (referencia)
- `Label` "Con material" / "Sin material"

**Columna derecha — Parámetros:**
- `Label` "SpriteMaterial Demo"
- `Checkbox` "Activar material" → alterna entre render con/sin shader
- `ColorPickerRGB` "Tint color" → actualiza `SpriteMaterial.Parameters["TintColor"]`
- `Slider` "Intensity" 0–2 → `SpriteMaterial.Parameters["Intensity"]`
- `Slider` "Hue Shift" 0–360° → parámetro de rotación de tono
- `Label` reactivo "Efecto activo: {nombre}"

**Notas:**
- Si no existe `SpriteTint.fx`, mostrar mensaje "Asset no encontrado — compilar Shaders/SpriteTint.fx con MGCB"
  en lugar del visor y deshabilitar controles.
- El shader básico multiplica el color del texel por el tint y la intensidad.

---

## Post-Procesado (24)

### Scene 24 — `PostProcessScene.cs` [TODO]

> Sistemas: **RenderTargetManager**, **PostProcessEffect**

**Dependencias de contenido:** `Content/Shaders/Vignette.fx`, `Content/Shaders/Grayscale.fx`

**Layout:** Mundo animado de fondo + panel UI superpuesto.

**Mundo de fondo:**
- Grid de colores (rectángulos de colores vivos) para que el efecto sea claramente visible
- Sprites moviéndose (círculos orbitando) para demostrar que el efecto aplica a todo el frame

**Panel UI:**
- `Label` "PostProcess Demo"
- `Dropdown` "Efecto:" — Ninguno, Vignette, Grayscale, Vignette+Grayscale
- `Slider` "Intensidad" 0–1 → parámetro del shader
- `Label` reactivo "RenderTarget: {w}×{h}"
- `Label` reactivo "Efecto activo: {nombre}"

**Notas:**
- Flujo: `RenderTargetManager.SetRenderTarget(rt)` → dibuja mundo → `RenderTargetManager.Reset()` →
  aplica `PostProcessEffect` al RenderTarget → dibuja resultado en pantalla.
- Si no existen los shaders, mostrar aviso y deshabilitar Dropdown.
- `RenderTargetManager` se crea con `new RenderTargetManager(Core.GraphicsDevice, screenW, screenH)`.

---

## Resolución Virtual (25)

### Scene 25 — `ResolutionScene.cs` [TODO]

> Sistemas: **ResolutionManager**

**Layout:** Panel UI + mundo renderizado en resolución virtual.

**Demostración:**
- Mundo 2D de referencia (cuadrícula + texto "VIRTUAL 320×180") dibujado en resolución virtual
- El mundo se escala letterbox/pillarbox al tamaño real de ventana

**Panel UI:**
- `Label` "ResolutionManager Demo"
- `Dropdown` "Resolución virtual:" — 320×180, 640×360, 800×450, 1280×720
- `Dropdown` "Resolución ventana:" — 640×360, 1280×720, 1920×1080
- `Checkbox` "Letterbox" → activa/desactiva escala proporcional
- `Label` reactivo "Virtual: {vw}×{vh} | Ventana: {ww}×{wh}"
- `Label` reactivo "Escala: {sx:F2}×{sy:F2} | Offset: {ox},{oy}"

**Notas:**
- `ResolutionManager` se instancia con `Core.GraphicsDevice` y la resolución virtual elegida.
- En `Draw`, llamar `ResolutionManager.BeginDraw()` antes del SpriteBatch del mundo.
- La UI se dibuja fuera del ResolutionManager (en screen space real).
- Al cambiar resolución, recrear el ResolutionManager con los nuevos parámetros.

---

## Tiled Maps (26)

### Scene 26 — `TiledMapScene.cs` [TODO]

> Sistemas: **TiledMapRenderer**, **TiledObjectLayer**

**Dependencias de contenido:** `Content/Maps/demo.tmx` + tilesets asociados

**Layout:** Mapa Tiled a pantalla completa + panel UI superpuesto.

**Viewport del mapa:**
- Mapa Tiled renderizado con `TiledMapRenderer`
- Cámara 2D navegable con WASD
- Objetos de la `TiledObjectLayer` destacados con rectángulos de colores

**Panel UI:**
- `Label` "TiledMap Demo"
- `Label` reactivo "Posición cámara: {x:F0},{y:F0}"
- `Label` reactivo "Objetos en mapa: {n}"
- `Checkbox` "Mostrar capa de colisiones" → alterna visibilidad de la capa de colisión
- `Checkbox` "Mostrar objetos" → alterna visibilidad de la capa de objetos
- `Label` "WASD: mover cámara"

**Notas:**
- Si no existe `demo.tmx`, mostrar mensaje de contenido faltante y un rectángulo de marcador.
- `TiledObjectLayer` expone los objetos del mapa para colisiones/triggers.
- `TiledMapRenderer` dibujado dentro del `SpriteBatch.Begin(transformMatrix: camera)`.

---

## Bitmap Fonts (27)

### Scene 27 — `BitmapFontScene.cs` [TODO]

> Sistemas: **BitmapFontRenderer**

**Dependencias de contenido:** `Content/Fonts/DefaultBitmapFont.fnt` + `DefaultBitmapFont_0.png`
(generado con Hiero/AngelCode, importado vía MonoGame.Extended)

**Layout:** Lienzo de texto + panel de control.

**Lienzo:**
- Texto de muestra renderizado con `BitmapFontRenderer` a distintos tamaños y colores
- Texto animado (escala pulsante, color arco iris)
- Caracteres especiales y acentos

**Panel UI:**
- `Label` "BitmapFont Demo"
- `TextBox` "Texto de prueba:" → actualiza el texto del lienzo
- `Slider` "Escala" 0.5–4.0
- `ColorPickerRGB` "Color"
- `Dropdown` "Alineación:" — Left, Center, Right
- `Checkbox` "Animación arco iris"

**Notas:**
- Si no existe el archivo `.fnt`, mostrar aviso y usar el `SpriteFont` de fallback.
- `BitmapFontRenderer` dibuja directamente sobre `SpriteBatch`.

---

## Physics2D Avanzado (28)

### Scene 28 — `Physics2DJointsScene.cs` [TODO]

> Sistemas: **PolygonCollider2D**, **DistanceJoint2D**, **HingeJoint2D**, **SpringJoint2D**,
> **CollisionCategory**, **CollisionMatrix**

**Layout:** Mundo físico a pantalla completa + panel UI derecho.

**Mundo físico:**
- Suelo estático + dos paredes
- Cadena de 6 cuerpos unidos por `DistanceJoint2D` (péndulo múltiple)
- Puerta giratoria unida al suelo con `HingeJoint2D`
- Par de cajas unidas con `SpringJoint2D` (muelle elástico)
- Polígono irregular con `PolygonCollider2D` cayendo desde arriba
- Dos capas de colisión: "Red" y "Blue" — los objetos Red no colisionan entre sí

**Panel UI:**
- `Label` "Physics2D Joints Demo"
- `Button` "Spawn Polygon" → crea un polígono irregular en posición aleatoria
- `Button` "Reset scene" → limpia y recrea el mundo
- `Button` "Toggle layers" → activa/desactiva filtrado de CollisionCategory
- `Label` reactivo "Cuerpos activos: {n}"
- `Label` reactivo "Contactos activos: {n}"

**Notas:**
- `CollisionMatrix` configura qué categorías colisionan entre sí.
- `PolygonCollider2D.SetVertices(points)` define la forma del polígono.
- `HingeJoint2D.EnableMotor = true` con `MotorSpeed` para girar la puerta.
- `SpringJoint2D.Stiffness` y `Damping` configurables desde sliders opcionales.

---

## Steering Behaviors (29)

### Scene 29 — `SteeringScene.cs` [TODO]

> Sistemas: **SteeringController**, **SeekBehavior**, **FleeBehavior**, **ArriveBehavior**,
> **WanderBehavior**, **SeparationBehavior**, **NavAgentProfile**

**Layout:** Mundo 2D a pantalla completa + panel UI izquierdo.

**Mundo 2D:**
- 1 agente "objetivo" (círculo azul) controlado por el ratón (Click izq. → mueve objetivo)
- 5 agentes con comportamientos distintos (círculos de colores)
- Estelas de movimiento visualizando las trayectorias

| Agente | Color | Comportamiento |
|--------|-------|----------------|
| Seek | Verde | `SeekBehavior` → persigue objetivo |
| Flee | Rojo | `FleeBehavior` → huye del objetivo |
| Arrive | Cian | `ArriveBehavior` → llega y desacelera suavemente |
| Wander | Amarillo | `WanderBehavior` → movimiento aleatorio suave |
| Separation | Magenta | `SeparationBehavior` → se mantiene separado del resto |

**Panel UI:**
- `Label` "Steering Behaviors Demo"
- `Slider` "MaxSpeed" 50–400 → `NavAgentProfile.MaxSpeed`
- `Slider` "MaxForce" 50–400 → `NavAgentProfile.MaxForce`
- `Slider` "SlowRadius (Arrive)" 10–200
- `Slider` "WanderRadius" 20–150
- `Slider` "SeparationRadius" 20–200
- `Checkbox` "Mostrar radios de influencia"
- `Label` "Click izq: mover objetivo"

**Notas:**
- Cada agente tiene su propio `SteeringController` con su comportamiento asignado.
- `NavAgentProfile` compartido para MaxSpeed/MaxForce; ajustado desde los sliders.
- Los radios de influencia se dibujan como círculos semitransparentes si el checkbox está activo.

---

## Entity Pool (30)

### Scene 30 — `EntityPoolScene.cs` [TODO]

> Sistemas: **GameEntityPool\<T\>**, **IPoolable**

**Layout:** Mundo de partículas/proyectiles + panel UI derecho.

**Mundo:**
- Disparar proyectiles (círculos rápidos) desde el centro hacia posiciones de click
- Los proyectiles se devuelven al pool al salir de pantalla o tras 3 segundos
- Visualización del estado del pool: activos en verde, en pool en gris

**Panel UI:**
- `Label` "EntityPool Demo"
- `Button` "Disparar ráfaga (×10)" → obtiene 10 entidades del pool y las lanza
- `Slider` "Capacidad pool" 10–200 → recrea el pool con nueva capacidad
- `Label` reactivo "Pool: {active} activos / {pooled} en reserva / {capacity} capacidad"
- `Label` reactivo "Obtenciones totales: {total}"
- `Label` reactivo "Misses (sin pool disponible): {misses}"
- `Checkbox` "Mostrar estado visual"

**Notas:**
- `GameEntityPool<ProjectileBehaviour>` con capacidad configurable.
- `ProjectileBehaviour : GameBehaviour, IPoolable` — implementa `OnObtain()` / `OnReturn()`.
- Comparar FPS con pool vs sin pool (crear/destruir sin pool = GC pressure).

---

## EventBus (31)

### Scene 31 — `EventBusScene.cs` [OK]

> Sistemas: **EventBus**, **EventChannel**, **ICancellableEvent**

**Layout:** Dos columnas — emisores y receptores.

**Columna Emisores:**
- `Label` "Publicar eventos"
- `Button` "Publish: GameStarted" → `EventBus.Publish(new GameStartedEvent())`
- `Button` "Publish: ScoreChanged (+10)" → `EventBus.Publish(new ScoreChangedEvent(+10))`
- `Button` "Publish: PlayerDied (cancelable)" → `EventBus.Publish(new PlayerDiedEvent())`
- `Checkbox` "Cancelar PlayerDied" → si activo, el subscriber cancela el evento

**Columna Receptores:**
- `Label` "Log de eventos recibidos" (scroll de las últimas 10 entradas)
- `Label` reactivo "Score acumulado: {n}"
- `Label` reactivo "PlayerDied recibido: {n} veces | Cancelado: {n} veces"
- `Button` "Limpiar log"
- `Button` "Subscribe/Unsubscribe" → registra/desregistra un receptor adicional

**Notas:**
- Definir 3 tipos de evento locales en el fichero: `GameStartedEvent`, `ScoreChangedEvent(int delta)`,
  `PlayerDiedEvent : ICancellableEvent`.
- `EventChannel<T>` para el canal de score (scoped); `EventBus` global para los otros.
- El log se actualiza en `Update` leyendo una cola de strings (no usar LINQ).

---

## State Machine (32)

### Scene 32 — `StateMachineScene.cs` [OK]

> Sistemas: **StateMachine\<TState\>**, **StateMachineBehaviour\<TState\>**

**Layout:** Diagrama visual de estados + panel de control.

**Diagrama visual:**
- 4 estados dibujados como rectángulos con nombre: Idle, Walk, Run, Attack
- Flechas entre estados mostrando las transiciones válidas
- Estado activo resaltado en amarillo
- Historial de transiciones (últimas 5) como lista

**Panel UI:**
- `Label` "StateMachine Demo"
- `Button` "→ Idle" / "→ Walk" / "→ Run" / "→ Attack" — fuerza transición
- `Button` "Auto Play" → cicla por los estados automáticamente cada 1.5 s
- `Label` reactivo "Estado actual: {state}"
- `Label` reactivo "Tiempo en estado: {t:F1}s"
- `Label` reactivo "Transiciones totales: {n}"

**Notas:**
- `enum PlayerState { Idle, Walk, Run, Attack }`
- `StateMachine<PlayerState>` con `OnEnter`, `OnUpdate`, `OnExit` por estado.
- Definir transiciones válidas: Idle↔Walk, Walk↔Run, any→Attack, Attack→Idle.
- Intentar una transición inválida muestra "Transición {A}→{B} no permitida" en un label.

---

## Timers (33)

### Scene 33 — `TimersScene.cs` [OK]

> Sistemas: **TimerManager**, **GameTimer**

**Layout:** Panel central de timers activos.

| Control | Comportamiento |
|---------|---------------|
| `Button` "One-shot 3s" | Crea `GameTimer` que dispara una vez en 3 s y muestra "¡Disparado!" |
| `Button` "Repeat 1s" | Crea `GameTimer` repetitivo que incrementa contador cada 1 s |
| `Button` "Scaled 0.5s" | Timer que se ejecuta al doble de velocidad (`TimeScale=2`) |
| `Button` "Pause/Resume" | Pausa y reanuda el `TimerManager` global |
| `Button` "Cancel All" | Cancela todos los timers activos |
| `Label` reactivo | "Timers activos: {n}" |
| `Label` reactivo | "Contador repeat: {n}" |
| `Label` reactivo | Lista de los 5 timers más recientes con tiempo restante |

**Notas:**
- `TimerManager` se obtiene vía `Core.GetService<TimerManager>()`.
- `GameTimer.Interval`, `GameTimer.IsRepeating`, `GameTimer.TimeScale`.
- La lista de timers activos se dibuja iterando sobre la colección interna (sin LINQ).

---

## Tweening Standalone (34)

### Scene 34 — `TweeningScene.cs` [OK]

> Sistemas: **TweeningManager**, **EasingCatalog** (todas las funciones)

**Layout:** Cuadrícula visual de curvas + panel de control + objeto animado.

**Cuadrícula de curvas (lado derecho):**
- 12 rectángulos de 80×60 px, uno por función de easing del catálogo
- Cada uno muestra la curva dibujada con líneas (muestreo de 50 puntos)
- La curva seleccionada se resalta en amarillo

**Objeto animado (centro):**
- Cuadrado de 40×40 px que anima de izquierda a derecha con el easing seleccionado
- La animación se reinicia automáticamente al terminar (loop)

**Panel UI (izquierda):**
- `Label` "Tweening Demo"
- `Dropdown` "Easing:" — Linear, QuadIn/Out/InOut, CubicIn/Out/InOut,
  BounceOut, ElasticOut, BackOut, SineIn/Out/InOut
- `Slider` "Duración" 0.3–3.0 s
- `Button` "Play / Reset"
- `Label` reactivo "t: {progress:F2} | valor: {value:F3}"

**Notas:**
- `Core.Tweening.TweenTo(target, prop, endValue, duration, easing)`.
- Dibujar la curva con `SpriteBatch.Draw(_pixel, lineRect, color)` por segmentos.
- El objeto animado usa la propiedad `X` de un `Vector2` local.

---

## Debug Tools (35)

### Scene 35 — `DebugScene.cs` [TODO]

> Sistemas: **DebugDraw**, **DebugOverlay**

**Layout:** Mundo de prueba + panel UI + overlay de debug.

**Mundo de prueba:**
- Varios objetos geométricos dibujados con `DebugDraw`:
  - Líneas, rectángulos, círculos, cruces, texto en mundo
- Objetos moviéndose para demostrar que `DebugDraw` es por-frame (no persistente)

**Panel UI:**
- `Label` "Debug Tools Demo"
- `Checkbox` "Mostrar DebugDraw" → activa/desactiva llamadas a `DebugDraw`
- `Checkbox` "Mostrar DebugOverlay" → activa/desactiva el overlay de stats
- `Checkbox` "Mostrar FPS" → `DebugOverlay.ShowFps`
- `Checkbox` "Mostrar memoria" → `DebugOverlay.ShowMemory`
- `Checkbox` "Mostrar GC" → `DebugOverlay.ShowGcCount`
- `Slider` "Escala overlay" 0.5–2.0

**Notas:**
- `DebugOverlay` se dibuja al final del Draw, sobre todo lo demás.
- `DebugDraw` llama se realizan dentro del `SpriteBatch.Begin/End` del mundo.
- Demostrar que `DebugDraw` no genera allocations en el game loop.

---

## Persistence (36)

### Scene 36 — `PersistenceScene.cs` [TODO]

> Sistemas: **SaveManager**, **SaveSlot**, **ISaveable**, **SaveDataWriter**, **SaveDataReader**

**Layout:** Tres columnas — datos editables, slots de guardado, log.

**Columna datos editables:**
- `Label` "Datos del jugador"
- `TextBox` "Nombre:" (string)
- `NumericBox` "Nivel:" (int, 1–100)
- `NumericBox` "Puntuación:" (int, 0–999999)
- `ColorPickerRGB` "Color favorito:"
- `Checkbox` "Tutorial completado"

**Columna slots:**
- `Label` "Slots de guardado (1–3)"
- Por slot: `Button` "Guardar", `Button` "Cargar", `Button` "Borrar"
- `Label` reactivo por slot: "Slot {n}: {nombre} Nv.{nivel} — {fecha}"
- `Label` "Slot vacío" si no hay datos

**Columna log:**
- `Label` "Log de operaciones"
- Lista de las últimas 8 operaciones (Guardado, Cargado, Borrado + timestamp)
- `Button` "Limpiar log"

**Notas:**
- Definir `PlayerSaveData : ISaveable` con los campos del panel izquierdo.
- `SaveManager` gestiona los slots; `SaveDataWriter/Reader` para serialización.
- Los slots se persisten en disco (carpeta `%AppData%/AlcaMonoGameDemo/`).

---

## Localización (37)

### Scene 37 — `LocalizationScene.cs` [TODO]

> Sistemas: **LocalizationManager**, **StringLocalizerExtensions**

**Dependencias de contenido:** `Content/Localization/es.json`, `Content/Localization/en.json`,
`Content/Localization/fr.json`

**Layout:** Pantalla dividida en dos mitades.

**Mitad izquierda — Selector de idioma:**
- `Label` "Localización Demo"
- `RadioGroup` con 3 opciones: Español (ES), English (EN), Français (FR)
- `Button` "Aplicar idioma"
- `Label` reactivo "Idioma activo: {code}"

**Mitad derecha — Textos localizados:**
- 8 Labels mostrando cadenas localizadas:
  - título, subtítulo, descripción, menú.inicio, menú.opciones, menú.salir, msg.bienvenida, msg.puntuacion

**Notas:**
- `LocalizationManager.SetLanguage("es")` en `PostInitialize`.
- `LocalizationManager.Get("menu.inicio")` → string localizado.
- Los ficheros JSON tienen estructura `{ "key": "valor" }`.
- Si no existen los ficheros de localización, generarlos en `LoadContent` con datos de ejemplo.

---

## Async Content (38)

### Scene 38 — `AsyncContentScene.cs` [TODO]

> Sistemas: **AsyncContentLoader**, **ContentGroupBuilder**, **ContentLoadGroup**

**Layout:** Pantalla de carga simulada + resultados.

**Pantalla de carga:**
- `ProgressBar` de carga total (0–100%)
- `Label` reactivo "Cargando: {asset actual}..."
- Lista de assets cargados (checkmarks)
- `Label` reactivo "Tiempo total: {t:F2}s"

**Tras completar la carga:**
- Los assets cargados se muestran (texturas como miniaturas, fuentes como texto de muestra)
- `Button` "Recargar grupo A" → descarga y recarga solo el grupo A
- `Button` "Recargar grupo B" → ídem para grupo B
- `Label` "Grupo A: texturas | Grupo B: fuentes"

**Notas:**
- `ContentGroupBuilder` define dos grupos: A (3 texturas) y B (1 fuente + 1 audio).
- `AsyncContentLoader.LoadGroupAsync(group, progress)` con callback de progreso.
- La `ProgressBar` se actualiza desde el callback de progreso en el hilo principal.
- Usar `Task` + `await` en `PostInitialize` (async) para la carga inicial.

---

## Lighting 2D (39)

### Scene 39 — `LightingScene.cs` [TODO]

> Sistemas: **LightingWorld**, **AmbientLight**, **PointLight2D**, **SpotLight2D**,
> **DirectionalLight2D**, **LightBehaviour**, **LightingRenderPipeline**

**Layout:** Mundo oscuro con luces dinámicas + panel UI.

**Mundo:**
- Fondo oscuro con sprites/objetos de colores que reaccionan a la iluminación
- 3 luces de colores que se pueden mover con click+drag:
  - `PointLight2D` roja (radio 200 px)
  - `SpotLight2D` azul (ángulo 45°, rango 300 px)
  - `DirectionalLight2D` blanca (global)
- `AmbientLight` configurable desde el panel

**Panel UI:**
- `Label` "Lighting 2D Demo"
- `Slider` "Luz ambiental" 0–1 → `AmbientLight.Intensity`
- `ColorPickerRGB` "Color ambiental" → `AmbientLight.Color`
- `Slider` "PointLight radio" 50–400 → `PointLight2D.Radius`
- `Slider` "SpotLight ángulo" 10–120° → `SpotLight2D.Angle`
- `Checkbox` "Activar PointLight / SpotLight / DirectionalLight"
- `Label` "Click + drag: mover luces"

**Notas:**
- `LightingRenderPipeline` gestiona el render de todas las luces via GPU.
- Las luces se registran en `LightingWorld.AddLight(light)`.
- El pipeline usa `RenderTargetManager` internamente.
- Requiere que los shaders de iluminación estén compilados con MGCB.

---

## Networking (40)

### Scene 40 — `NetworkingScene.cs` [TODO]

> Sistemas: **NetworkServer**, **NetworkClient**, **NetworkIdentity**, **NetworkManagerBehaviour**,
> **NetworkReplicator**, **NetworkTransformSync**, **NetFields** (NetInt, NetFloat, NetVector2),
> **NetSyncAttribute**, **NetworkPhysicsSync**

**Layout:** Dos paneles lado a lado — servidor y cliente — simulando una sesión local loopback.

**Panel Servidor (izquierda):**
- `Label` "Servidor"
- `Button` "Iniciar servidor (localhost:7777)" → `NetworkServer.Start(7777)`
- `Button` "Detener servidor"
- `Label` reactivo "Estado: {Offline/Running}"
- `Label` reactivo "Clientes conectados: {n}"
- `Label` reactivo "Mensajes enviados: {n}"
- `Label` reactivo "Entidad replicada pos: {x:F1},{y:F1}"

**Panel Cliente (derecha):**
- `Label` "Cliente"
- `Button` "Conectar a localhost:7777" → `NetworkClient.Connect("127.0.0.1", 7777)`
- `Button` "Desconectar"
- `Label` reactivo "Estado: {Offline/Connected}"
- `Label` reactivo "Ping: {ms} ms"
- `Label` reactivo "Mensajes recibidos: {n}"
- `Label` reactivo "Entidad recibida pos: {x:F1},{y:F1}"

**Entidad replicada (centro pantalla):**
- Cuadrado azul controlado desde el lado servidor (WASD)
- El cliente ve la posición sincronizada en tiempo real
- `Label` "Mover con WASD (lado servidor)"

**Notas:**
- `NetworkManagerBehaviour` adjunto a una `GameEntity` global, gestiona servidor y cliente.
- `NetworkTransformSync` en la entidad replicada sincroniza `Transform.Position`.
- `NetSyncAttribute` decora los `NetField` que se replican automáticamente.
- Loopback en la misma máquina; servidor y cliente en el mismo proceso.
- Los `NetField` (NetInt para score, NetVector2 para posición) se actualizan en `Update` del servidor.

---

## Platform (41)

### Scene 41 — `PlatformScene.cs` [OK]

> Sistemas: **PlatformManager**, **PlatformType**

**Layout:** Panel informativo centrado.

| Elemento | Descripción |
|----------|-------------|
| `Label` "Platform Demo" | Header |
| `Label` "Plataforma detectada: {type}" | `PlatformManager.Current` |
| `Label` "¿Windows?: {bool}" | `PlatformManager.IsWindows` |
| `Label` "¿Linux?: {bool}" | `PlatformManager.IsLinux` |
| `Label` "¿macOS?: {bool}" | `PlatformManager.IsMacOS` |
| `Label` "¿Mobile?: {bool}" | `PlatformManager.IsMobile` |
| `Label` "¿Console?: {bool}" | `PlatformManager.IsConsole` |
| `Label` "Path de datos: {path}" | `PlatformManager.StoragePath` |
| `Label` "Versión OS: {ver}" | `PlatformManager.OsVersion` |
| `Panel` | Info adicional específica de plataforma (renderers, drivers) |

**Notas:**
- Escena puramente informativa, sin interacción más allá del botón `← Menú`.
- Si hay datos de capacidades gráficas disponibles (`GraphicsDevice.Adapter`), mostrarlos también.
