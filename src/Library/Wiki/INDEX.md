# Índice de la Wiki — Alca.MonoGame.Kernel

Índice completo de todos los módulos, clases e interfaces documentadas.

---

## 01 · Core

| Clase | Descripción | Enlace |
|---|---|---|
| `Core` | Clase base del juego; hereda de `Game`, gestiona DI y ciclo de vida | [01-core/core.md](01-core/core.md) |

---

## 02 · ECS (Entity-Component-System)

| Clase | Descripción | Enlace |
|---|---|---|
| Visión general ECS | Diagrama de relaciones y flujo de datos | [02-ecs/overview.md](02-ecs/overview.md) |
| `GameEntity` | Contenedor de comportamientos; equivale al GameObject de Unity | [02-ecs/game-entity.md](02-ecs/game-entity.md) |
| `GameBehaviour` | Clase base para componentes; ciclo Awake→Start→Update→Draw→OnDestroy | [02-ecs/game-behaviour.md](02-ecs/game-behaviour.md) |
| `GameWorld` | Propietario de entidades; integra Physics, Lighting, Audio, Navigation, Network | [02-ecs/game-world.md](02-ecs/game-world.md) |
| `TransformBehaviour` | Posición, rotación y escala; espacios local y world; matrices de transformación | [02-ecs/transform.md](02-ecs/transform.md) |
| `SpriteRendererBehaviour` | Renderiza una `Texture2D` centrada en la posición world de la entidad | [02-ecs/game-entity.md](02-ecs/game-entity.md) |
| `GameEntityPool<T>` | Pool de entidades reutilizables para objetos frecuentes (proyectiles, partículas) | [02-ecs/entity-pool.md](02-ecs/entity-pool.md) |

---

## 03 · Scenes (Gestión de Escenas)

| Clase | Descripción | Enlace |
|---|---|---|
| Visión general Scenes | Ciclo de vida de una escena y su relación con GameWorld / UIRoot | [03-scenes/overview.md](03-scenes/overview.md) |
| `Scene` | Clase base abstracta; hooks PreInitialize→CreateWorld→LoadContent→InitializeUI | [03-scenes/scene.md](03-scenes/scene.md) |
| `SceneManager` | Transiciones con fade, pila de overlays (máx. 4) | [03-scenes/scene-manager.md](03-scenes/scene-manager.md) |
| `LoadingScene` | Escena de ejemplo para pantallas de carga | [03-scenes/scene.md](03-scenes/scene.md) |
| `ISceneTransition` | Interfaz para transiciones enchufables | [03-scenes/transitions.md](03-scenes/transitions.md) |
| `FadeTransition` | Fundido a color sólido (default retrocompatible) | [03-scenes/transitions.md](03-scenes/transitions.md) |
| `SlideTransition` | Cortina deslizante en 4 direcciones | [03-scenes/transitions.md](03-scenes/transitions.md) |
| `CircleWipeTransition` | Iris wipe radial | [03-scenes/transitions.md](03-scenes/transitions.md) |
| `DissolveTransition` | Dissolve con noise texture | [03-scenes/transitions.md](03-scenes/transitions.md) |

---

## 04 · Graphics (Gráficos)

### Cámaras

| Clase | Descripción | Enlace |
|---|---|---|
| `Camera2D` | Cámara ortográfica 2D; zoom, rotación, follow, clamp | [04-graphics/camera-2d.md](04-graphics/camera-2d.md) |
| `Camera3D` | Clase base para cámaras 3D; `View`, `Projection`, `BoundingFrustum` | [04-graphics/camera-3d.md](04-graphics/camera-3d.md) |
| `FirstPersonCamera3D` | Cámara FPS; control de yaw/pitch | [04-graphics/camera-3d.md](04-graphics/camera-3d.md) |
| `ThirdPersonCamera3D` | Cámara orbital alrededor de un objetivo | [04-graphics/camera-3d.md](04-graphics/camera-3d.md) |
| `TopDownCamera3D` | Vista cenital / isométrica | [04-graphics/camera-3d.md](04-graphics/camera-3d.md) |
| `FixedCamera3D` | Cámara estática en posición fija | [04-graphics/camera-3d.md](04-graphics/camera-3d.md) |
| `CameraEffects` | Efectos de cámara (screen shake, etc.) | [04-graphics/camera-2d.md](04-graphics/camera-2d.md) |
| `ResolutionManager` | Resolución virtual, letterboxing, `ScaleMatrix` | [04-graphics/resolution.md](04-graphics/resolution.md) |
| `IsometricCamera` | Cámara ortográfica con proyección isométrica 2:1 | [04-graphics/twopointfived.md](04-graphics/twopointfived.md) |
| `IsometricHelper` | Conversiones WorldToScreen/ScreenToWorld isométricas | [04-graphics/twopointfived.md](04-graphics/twopointfived.md) |

### Sprites y Animación

| Clase | Descripción | Enlace |
|---|---|---|
| `TextureRegion` | Región rectangular dentro de una textura; coordenadas UV | [04-graphics/sprites.md](04-graphics/sprites.md) |
| `TextureAtlas` | Colección de `TextureRegion` con lookup por nombre | [04-graphics/sprites.md](04-graphics/sprites.md) |
| `Sprite` | Unidad visual: región + color + rotación + escala + origin | [04-graphics/sprites.md](04-graphics/sprites.md) |
| `Animation` | Secuencia de frames con delay y loop | [04-graphics/animation.md](04-graphics/animation.md) |
| `AnimatedSprite` | `Sprite` con playback de `Animation`; Play/Pause/Stop, callback OnComplete | [04-graphics/animation.md](04-graphics/animation.md) |
| `AnimationStateMachine` | FSM de animaciones; transiciones entre estados | [04-graphics/animation.md](04-graphics/animation.md) |
| `AnimatedSpriteBehaviour` | `GameBehaviour` que controla un `AnimatedSprite` | [04-graphics/animation.md](04-graphics/animation.md) |
| `AnimationStateMachineBehaviour` | `GameBehaviour` que controla una `AnimationStateMachine` | [04-graphics/animation.md](04-graphics/animation.md) |
| `YSortRendererBehaviour` | Sprite con profundidad por posición Y (2.5D) | [04-graphics/twopointfived.md](04-graphics/twopointfived.md) |
| `BillboardSpriteBehaviour` | Sprite orientado hacia la cámara | [04-graphics/twopointfived.md](04-graphics/twopointfived.md) |
| `NormalMapSpriteMaterial` | Material con mapa de normales para iluminación 2D | [04-graphics/twopointfived.md](04-graphics/twopointfived.md) |

### Efectos y Shaders

| Clase | Descripción | Enlace |
|---|---|---|
| `RenderTargetManager` | Ping-pong de render targets para post-proceso | [04-graphics/post-processing.md](04-graphics/post-processing.md) |
| `PostProcessEffect` | Clase base para efectos de post-procesado | [04-graphics/post-processing.md](04-graphics/post-processing.md) |
| `Material` | Envuelve un `Effect` de MonoGame; aplica parámetros al GPU | [04-graphics/shaders.md](04-graphics/shaders.md) |
| `SpriteMaterial` | `Material` especializado para sprites 2D | [04-graphics/shaders.md](04-graphics/shaders.md) |
| `OutlineMaterial` | Contorno sólido alrededor del sprite | [04-graphics/shader-library.md](04-graphics/shader-library.md) |
| `FlashMaterial` | Hit flash de color configurable | [04-graphics/shader-library.md](04-graphics/shader-library.md) |
| `DissolveMaterial` | Disolución con textura de ruido y borde coloreado | [04-graphics/shader-library.md](04-graphics/shader-library.md) |
| `GlowMaterial` | Halo brillante alrededor del sprite | [04-graphics/shader-library.md](04-graphics/shader-library.md) |
| `SilhouetteMaterial` | Silueta sólida | [04-graphics/shader-library.md](04-graphics/shader-library.md) |
| `CRTPostEffect` | Scanlines + curvatura barril + viñeta estilo CRT | [04-graphics/shader-library.md](04-graphics/shader-library.md) |

### Partículas, Fuentes y Tiles

| Clase | Descripción | Enlace |
|---|---|---|
| `ParticleBuilder` | Fluent API para configurar efectos de partículas | [04-graphics/particles.md](04-graphics/particles.md) |
| `ParticleEffectWrapper` | Wrapper sobre MonoGame.Extended.Particles | [04-graphics/particles.md](04-graphics/particles.md) |
| `ParticleEmitterBehaviour` | `GameBehaviour` que impulsa partículas desde la transformación de la entidad | [04-graphics/particles.md](04-graphics/particles.md) |
| `BitmapFontRenderer` | Renderizado de fuentes bitmap | [04-graphics/sprites.md](04-graphics/sprites.md) |
| `TiledMapRenderer` | Renderiza mapas `.tmx` de Tiled | [04-graphics/tiled.md](04-graphics/tiled.md) |
| `TiledObjectLayer` | Acceso a capas de objetos de un mapa Tiled | [04-graphics/tiled.md](04-graphics/tiled.md) |

### Renderizado 3D

| Clase | Descripción | Enlace |
|---|---|---|
| `MeshRenderer` | Renderiza modelos 3D (`.xnb` / `Model`) | [04-graphics/rendering-3d.md](04-graphics/rendering-3d.md) |
| `PrimitiveBatch` | Renderizado de primitivos 3D (líneas, cajas) para debug | [04-graphics/rendering-3d.md](04-graphics/rendering-3d.md) |

---

## 05 · UI (Interfaz de Usuario)

### Elementos base

| Clase | Descripción | Enlace |
|---|---|---|
| `UIElement` | Clase base abstracta; Measure, Arrange, Update, Draw | [05-ui/elements.md](05-ui/elements.md) |
| `UIContainer` | Contenedor con colección de hijos; propaga ciclo | [05-ui/elements.md](05-ui/elements.md) |
| `UIRoot` | Raíz del árbol UI de una escena | [05-ui/elements.md](05-ui/elements.md) |
| `UIOverlayManager` | Gestiona elementos flotantes (dropdowns, tooltips) | [05-ui/interaction.md](05-ui/interaction.md) |

### Controles

| Clase | Descripción | Enlace |
|---|---|---|
| `Button` | Botón presionable con estados hover/press; evento `Clicked` | [05-ui/controls.md](05-ui/controls.md) |
| `Label` | Texto estático con alineación y word-wrap | [05-ui/controls.md](05-ui/controls.md) |
| `TextBox` | Entrada de texto de una línea; compatible con IME | [05-ui/controls.md](05-ui/controls.md) |
| `TextArea` | Entrada de texto multilínea | [05-ui/controls.md](05-ui/controls.md) |
| `PasswordBox` | Caja de contraseña con caracteres enmascarados | [05-ui/controls.md](05-ui/controls.md) |
| `NumericBox` | Entrada numérica con validación | [05-ui/controls.md](05-ui/controls.md) |
| `Slider` | Control deslizante; soporta teclado y gamepad | [05-ui/controls.md](05-ui/controls.md) |
| `ProgressBar` | Barra de progreso | [05-ui/controls.md](05-ui/controls.md) |
| `Checkbox` | Casilla de verificación; evento `CheckedChanged` | [05-ui/controls.md](05-ui/controls.md) |
| `RadioButton` / `RadioGroup` | Opciones excluyentes; evento `SelectionChanged` | [05-ui/controls.md](05-ui/controls.md) |
| `Dropdown` | Lista desplegable con flip-up inteligente | [05-ui/controls.md](05-ui/controls.md) |
| `ScrollView` | Vista con desplazamiento vertical/horizontal | [05-ui/controls.md](05-ui/controls.md) |
| `Panel` | Contenedor decorativo; soporte nine-slice | [05-ui/controls.md](05-ui/controls.md) |
| `ColorPickerRGB` | Selector de color RGB | [05-ui/controls.md](05-ui/controls.md) |
| `Tooltip` | Información emergente al hacer hover | [05-ui/controls.md](05-ui/controls.md) |
| `UISprite` | Sprite dentro del árbol UI | [05-ui/controls.md](05-ui/controls.md) |

### Layout

| Clase | Descripción | Enlace |
|---|---|---|
| `Canvas` | Posicionamiento absoluto con offsets | [05-ui/layout.md](05-ui/layout.md) |
| `StackPanel` | Apila hijos horizontal o verticalmente | [05-ui/layout.md](05-ui/layout.md) |
| `GridLayout` | Rejilla con Fixed/Auto/Star sizing y spanning | [05-ui/layout.md](05-ui/layout.md) |
| `AnchorLayout` | Ancla hijos a bordes/esquinas/centro | [05-ui/layout.md](05-ui/layout.md) |
| `FlowLayoutPanel` | Flujo automático con wrap | [05-ui/layout.md](05-ui/layout.md) |

### Interacción y Foco

| Clase | Descripción | Enlace |
|---|---|---|
| `IUIInteractable` | Interfaz para controles que reciben eventos de puntero | [05-ui/interaction.md](05-ui/interaction.md) |
| `UIInteractionManager` | Hit testing DFS; despacha eventos OnPointerEnter/Leave/Down/Up; `IsPointerOverUI` para bloquear input del juego | [05-ui/interaction.md](05-ui/interaction.md) |
| `IFocusable` | Interfaz para controles que pueden recibir foco | [05-ui/focus.md](05-ui/focus.md) |
| `UIFocusManager` | Navegación por teclado/gamepad; Tab, D-Pad, vecinos | [05-ui/focus.md](05-ui/focus.md) |
| `UITransitionManager` | Transiciones animadas entre paneles | [05-ui/transitions.md](05-ui/transitions.md) |

---

## 06 · Audio

| Clase | Descripción | Enlace |
|---|---|---|
| `AudioController` | Control central: PlaySoundEffect, PlaySong, mute, listener 3D | [06-audio/audio-controller.md](06-audio/audio-controller.md) |
| `AudioMixer` | Canales Master/Music/SFX/Ambient con volumen y mute | [06-audio/audio-mixer.md](06-audio/audio-mixer.md) |
| `AudioMixerChannel` | Canal individual del mezclador | [06-audio/audio-mixer.md](06-audio/audio-mixer.md) |
| `SoundEffectPool` | Pool round-robin para reproducción de alta frecuencia | [06-audio/spatial-audio.md](06-audio/spatial-audio.md) |
| `SpatialAudioSource` | `GameBehaviour` para fuentes de audio 3D posicional | [06-audio/spatial-audio.md](06-audio/spatial-audio.md) |
| `SpatialAudioListener` | Receptor de audio 3D (posición del oyente) | [06-audio/spatial-audio.md](06-audio/spatial-audio.md) |
| `AudioEmitter3D` | Emisor 3D de bajo nivel | [06-audio/spatial-audio.md](06-audio/spatial-audio.md) |
| `AudioListener3D` | Oyente 3D de bajo nivel | [06-audio/spatial-audio.md](06-audio/spatial-audio.md) |
| `AudioZone` | Zona de audio con efectos posicionales | [06-audio/spatial-audio.md](06-audio/spatial-audio.md) |
| `AudioCrossfader` | Transiciones suaves entre pistas de música | [06-audio/crossfade.md](06-audio/crossfade.md) |

---

## 07 · Input

| Clase | Descripción | Enlace |
|---|---|---|
| `InputManager` | Gestor central: teclado, ratón, 4 gamepads | [07-input/input-system.md](07-input/input-system.md) |
| `InputAction` | Acción lógica (p.ej. "Jump") mapeada a bindings | [07-input/input-system.md](07-input/input-system.md) |
| `InputActionMap` | Colección de acciones activa en un contexto | [07-input/input-system.md](07-input/input-system.md) |
| `InputBinding` | Enlace entre input físico y acción lógica | [07-input/input-system.md](07-input/input-system.md) |
| `InputSerializer` | Serialización de mapas de entrada a JSON | [07-input/input-system.md](07-input/input-system.md) |
| `KeyboardInfo` | Estado del teclado (actual y anterior) | [07-input/input-system.md](07-input/input-system.md) |
| `MouseInfo` | Estado del ratón (posición, botones) | [07-input/input-system.md](07-input/input-system.md) |
| `GamePadInfo` | Estado de un gamepad individual | [07-input/input-system.md](07-input/input-system.md) |

---

## 08 · Physics 2D

| Clase | Descripción | Enlace |
|---|---|---|
| `Physics2DWorld` | Mundo de simulación Aether.Physics2D; gravedad, iteraciones | [08-physics/physics-world.md](08-physics/physics-world.md) |
| `Physics2DQuery` | Raycasts y tests de solapamiento | [08-physics/queries.md](08-physics/queries.md) |
| `RigidBody2D` | Cuerpo rígido 2D; estático/dinámico, velocidades, damping | [08-physics/rigid-body.md](08-physics/rigid-body.md) |
| `BoxCollider2D` | Colisionador rectangular | [08-physics/colliders.md](08-physics/colliders.md) |
| `CircleCollider2D` | Colisionador circular | [08-physics/colliders.md](08-physics/colliders.md) |
| `PolygonCollider2D` | Colisionador poligonal convexo | [08-physics/colliders.md](08-physics/colliders.md) |
| `DistanceJoint2D` | Articulación de distancia fija | [08-physics/joints.md](08-physics/joints.md) |
| `HingeJoint2D` | Articulación de bisagra/pivote | [08-physics/joints.md](08-physics/joints.md) |
| `SpringJoint2D` | Articulación de resorte | [08-physics/joints.md](08-physics/joints.md) |
| `CollisionCategory` | Flags enum para máscaras de colisión | [08-physics/colliders.md](08-physics/colliders.md) |
| `CollisionMatrix` | Matriz que define qué categorías colisionan | [08-physics/colliders.md](08-physics/colliders.md) |
| `RaycastHit2D` | Resultado de un raycast | [08-physics/queries.md](08-physics/queries.md) |
| `TriggerWorld` | Servicio de detección de solapamiento sin física | [08-physics/trigger-volumes.md](08-physics/trigger-volumes.md) |
| `TriggerZone2D` | `GameBehaviour` de zona de activación AABB/Circle | [08-physics/trigger-volumes.md](08-physics/trigger-volumes.md) |
| `TriggerOverlapInfo` | `readonly struct` con Self y Other zone | [08-physics/trigger-volumes.md](08-physics/trigger-volumes.md) |
| `TriggerShapeType` | Enum `{ AABB, Circle }` | [08-physics/trigger-volumes.md](08-physics/trigger-volumes.md) |

---

## 09 · Lighting 2D

| Clase | Descripción | Enlace |
|---|---|---|
| `LightingWorld` | Servicio singleton; acumula y resuelve contribuciones de luz | [09-lighting/overview.md](09-lighting/overview.md) |
| `LightBehaviour` | `GameBehaviour` base para todos los tipos de luz | [09-lighting/light-types.md](09-lighting/light-types.md) |
| `AmbientLight` | Luz ambiental global | [09-lighting/light-types.md](09-lighting/light-types.md) |
| `DirectionalLight2D` | Luz direccional | [09-lighting/light-types.md](09-lighting/light-types.md) |
| `PointLight2D` | Luz puntual omnidireccional | [09-lighting/light-types.md](09-lighting/light-types.md) |
| `SpotLight2D` | Luz de foco con ángulo y rango | [09-lighting/light-types.md](09-lighting/light-types.md) |
| `LightingRenderPipeline` | Pipeline GPU de iluminación | [09-lighting/gpu-pipeline.md](09-lighting/gpu-pipeline.md) |
| `LightShaderData` | Datos de luz para el shader | [09-lighting/gpu-pipeline.md](09-lighting/gpu-pipeline.md) |
| `DayNightCycle` | Servicio de ciclo día/noche; actualiza LightingWorld | [09-lighting/day-night.md](09-lighting/day-night.md) |
| `DayNightProfile` | Perfil con 4 keyframes de iluminación | [09-lighting/day-night.md](09-lighting/day-night.md) |
| `DayNightKeyframe` | `readonly struct` con TimeOfDay, Color, Intensity, SunAngle | [09-lighting/day-night.md](09-lighting/day-night.md) |
| `TimeOfDay` | `readonly struct` en [0, 24) con helpers IsDaytime/Lerp | [09-lighting/day-night.md](09-lighting/day-night.md) |

---

## 10 · Navigation

| Clase | Descripción | Enlace |
|---|---|---|
| `NavGrid` | Rejilla de navegación; configuración de celdas transitables | [10-navigation/nav-grid.md](10-navigation/nav-grid.md) |
| `NavCell` | Celda individual; walkable, coste de movimiento | [10-navigation/nav-grid.md](10-navigation/nav-grid.md) |
| `NavAgentProfile` | Perfil de movimiento (velocidad, radio, capas transitables) | [10-navigation/nav-grid.md](10-navigation/nav-grid.md) |
| `Pathfinder` | A* síncrono zero-alloc | [10-navigation/nav-grid.md](10-navigation/nav-grid.md) |
| `AsyncPathfinder` | A* asíncrono en hilo de fondo | [10-navigation/nav-agent.md](10-navigation/nav-agent.md) |
| `NavPath` | Secuencia de waypoints resultado del pathfinding | [10-navigation/nav-agent.md](10-navigation/nav-agent.md) |
| `NavAgent` | `GameBehaviour` que mueve la entidad a lo largo de una ruta | [10-navigation/nav-agent.md](10-navigation/nav-agent.md) |
| `NavGridPhysicsSync` | Sincronización automática de NavGrid con Physics2DWorld | [10-navigation/nav-grid.md](10-navigation/nav-grid.md) |
| `SteeringController` | Combina múltiples comportamientos de steering | [10-navigation/steering.md](10-navigation/steering.md) |
| `SeekBehavior` | Busca activamente un objetivo | [10-navigation/steering.md](10-navigation/steering.md) |
| `FleeBehavior` | Huye de un objetivo | [10-navigation/steering.md](10-navigation/steering.md) |
| `ArriveBehavior` | Llega suavemente reduciendo velocidad | [10-navigation/steering.md](10-navigation/steering.md) |
| `WanderBehavior` | Movimiento errático de vagabundeo | [10-navigation/steering.md](10-navigation/steering.md) |
| `SeparationBehavior` | Mantiene distancia con agentes cercanos | [10-navigation/steering.md](10-navigation/steering.md) |

---

## 11 · Network

| Clase | Descripción | Enlace |
|---|---|---|
| `NetworkManagerBehaviour` | `GameBehaviour` que inicia sesión de red (Server/Client/Host) | [11-network/server-client.md](11-network/server-client.md) |
| `NetworkServer` | Servidor UDP (LiteNetLib) | [11-network/server-client.md](11-network/server-client.md) |
| `NetworkClient` | Cliente UDP (LiteNetLib) | [11-network/server-client.md](11-network/server-client.md) |
| `NetworkIdentity` | Identidad única para objetos replicados | [11-network/network-identity.md](11-network/network-identity.md) |
| `NetworkReplicator` | Sistema de replicación de estado | [11-network/network-identity.md](11-network/network-identity.md) |
| `NetworkTransformSync` | Sincronización de `TransformBehaviour` en red | [11-network/network-identity.md](11-network/network-identity.md) |
| `NetworkPhysicsSync` | Sincronización de `RigidBody2D` en red | [11-network/network-identity.md](11-network/network-identity.md) |
| `NetInt` / `NetFloat` / `NetVector2` / … | Campos sincronizados automáticamente | [11-network/net-fields.md](11-network/net-fields.md) |
| `NetSyncAttribute` | Atributo para marcar campos a sincronizar | [11-network/net-fields.md](11-network/net-fields.md) |
| `NetworkReader` / `NetworkWriter` | Serialización de mensajes de red | [11-network/server-client.md](11-network/server-client.md) |

---

## 12 · Módulos Auxiliares

| Clase | Descripción | Enlace |
|---|---|---|
| `SaveManager` | Slots de guardado con metadatos, async | [12-misc/persistence.md](12-misc/persistence.md) |
| `ISaveable` | Interfaz para objetos persistibles | [12-misc/persistence.md](12-misc/persistence.md) |
| `StateMachine<TState>` | FSM genérica zero-alloc con callbacks por estado | [12-misc/state-machine.md](12-misc/state-machine.md) |
| `StateMachineBehaviour` | `GameBehaviour` que encapsula una `StateMachine<TState>` | [12-misc/state-machine.md](12-misc/state-machine.md) |
| `TweeningManager` | Animaciones de propiedades; wrapper de MonoGame.Extended Tweener | [12-misc/tweening.md](12-misc/tweening.md) |
| `EasingCatalog` | Funciones de easing predefinidas | [12-misc/tweening.md](12-misc/tweening.md) |
| `EventBus` | Bus de eventos global estático; prioridades, cancelables | [12-misc/event-bus.md](12-misc/event-bus.md) |
| `MathUtils` | Distancias, ángulos, lerp, clamp | [12-misc/mathematics.md](12-misc/mathematics.md) |
| `BoundingHelpers` | Helpers de colisión y bounds | [12-misc/mathematics.md](12-misc/mathematics.md) |
| `GeometryUtility` | Utilidades geométricas (polígonos, SAT) | [12-misc/mathematics.md](12-misc/mathematics.md) |
| `Circle` | Estructura de círculo 2D | [12-misc/mathematics.md](12-misc/mathematics.md) |
| `LocalizationManager` | Multi-idioma con JSON; cambio de cultura en runtime | [12-misc/localization.md](12-misc/localization.md) |
| `PlatformManager` | Detección de plataforma, eventos de ciclo de vida | [12-misc/platform.md](12-misc/platform.md) |
| `DebugDraw` | Renderización de debug: líneas, rectángulos, texto | [12-misc/debug.md](12-misc/debug.md) |
| `DebugOverlay` | Overlay de información de juego | [12-misc/debug.md](12-misc/debug.md) |
| `AsyncContentLoader` | Carga de assets en background con progreso y cancelación | [12-misc/async-content.md](12-misc/async-content.md) |
| `TimerManager` | Scheduler de temporizadores únicos y repetidos; pool zero-alloc | [12-misc/timers.md](12-misc/timers.md) |
| `PerlinNoise` | Ruido Perlin 1D/2D seeded + fBm | [12-misc/procedural-noise.md](12-misc/procedural-noise.md) |
| `SimplexNoise` | Ruido Simplex 2D/3D seeded | [12-misc/procedural-noise.md](12-misc/procedural-noise.md) |
| `NoiseMap` | Generador de mapas de ruido con normalización | [12-misc/procedural-noise.md](12-misc/procedural-noise.md) |

---

## 13 · Weather (Sistema de Climatología)

| Clase | Descripción | Enlace |
|---|---|---|
| Visión general | Diagrama del sistema, distinción temperatura/clima, quickstart | [13-weather/overview.md](13-weather/overview.md) |
| `WeatherWorld` | Servicio central: catálogo, transiciones, temperatura, viento | [13-weather/weather-world.md](13-weather/weather-world.md) |
| `WeatherProfile` | `readonly struct` que describe un estado atmosférico completo | [13-weather/profiles.md](13-weather/profiles.md) |
| `WeatherTypeId` | Identificador extensible de clima (string-based) | [13-weather/profiles.md](13-weather/profiles.md) |
| `WeatherProfiles` | Catálogo de 10 perfiles predefinidos | [13-weather/profiles.md](13-weather/profiles.md) |
| `WindState` | `readonly struct` con dirección, velocidad y turbulencia de viento | [13-weather/weather-world.md](13-weather/weather-world.md) |
| `PrecipitationIntensity` | Enum de intensidad (`None`, `Low`, `Medium`, `High`, `VeryHigh`) | [13-weather/profiles.md](13-weather/profiles.md) |
| `LightningStrikeEvent` | `readonly struct` con datos de un impacto de rayo | [13-weather/lightning.md](13-weather/lightning.md) |
| `WeatherBehaviour` | `GameBehaviour` opt-in: recibe viento e impulsos de rayo | [13-weather/behaviour.md](13-weather/behaviour.md) |
| `WeatherParticleLayer` | Capa de partículas: lluvia, nieve, granizo, niebla, viento | [13-weather/particles.md](13-weather/particles.md) |
| `LightningController` | Temporizador de rayos, flash `PointLight2D`, impulso, audio | [13-weather/lightning.md](13-weather/lightning.md) |
| `WeatherAudioLayer` | Loops ambiente (lluvia/viento/trueno) + pool de truenos espaciales | [13-weather/audio.md](13-weather/audio.md) |

---

## 14 · Dialogue (Sistema de Diálogo)

| Clase | Descripción | Enlace |
|---|---|---|
| Visión general | Arquitectura del sistema, quickstart | [14-dialogue/overview.md](14-dialogue/overview.md) |
| `DialogueLine` | `readonly struct` con speaker, locKey, portrait, choices | [14-dialogue/script.md](14-dialogue/script.md) |
| `DialogueChoice` | `readonly struct` con locKey, nextLineIndex, condition | [14-dialogue/script.md](14-dialogue/script.md) |
| `DialogueCondition` | `readonly struct` para filtrar opciones | [14-dialogue/script.md](14-dialogue/script.md) |
| `DialogueScript` | Colección inmutable de líneas con Builder fluent | [14-dialogue/script.md](14-dialogue/script.md) |
| `DialogueManager` | Servicio: avanza script, eventos OnStarted/OnEnded | [14-dialogue/manager.md](14-dialogue/manager.md) |
| `TypewriterEffect` | Revelado carácter a carácter zero-alloc | [14-dialogue/typewriter.md](14-dialogue/typewriter.md) |
| `DialogueBoxBehaviour` | `GameBehaviour` que dibuja caja + texto del diálogo | [14-dialogue/choices.md](14-dialogue/choices.md) |
| `ChoicesPanelBehaviour` | `GameBehaviour` que muestra botones de elección | [14-dialogue/choices.md](14-dialogue/choices.md) |
