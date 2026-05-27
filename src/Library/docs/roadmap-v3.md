# Roadmap v3: Integration, Missing Features & Architecture

**Reglas transversales a todos los desarrollos:**
- Al terminar, actualizar este fichero marcando los TODOs completados.

## Executive Summary

### v1 Accomplishments (Phases 1–6)

Phases 1 through 6 delivered the foundation of the library: math and camera systems (Camera2D, Camera3D family, ResolutionManager), advanced rendering (RenderTargets, post-process effects, 3D mesh rendering, PrimitiveBatch), core game systems (Particles via MonoGame.Extended wrapper, TweeningManager, extended 3D audio, ECS with entity pools and tags, Scene stack), platform abstraction (LocalizationManager, PlatformManager), a production-ready UI system (20+ controls, 5 layout managers, UIFocusManager, UIInteractionManager, UIOverlayManager), and Extended integrations (TiledMapRenderer, BitmapFontRenderer, InputManager with action maps, AsyncContentLoader). Approximately 702 unit tests were delivered.

### v2 Accomplishments (Phases 7–10)

Phases 7 through 10 delivered the simulation layer: Unity-style entity hierarchy with parent/child transforms and local/world space matrices (Phase 7), Aether.Physics2D-backed 2D physics with RigidBody2D, three collider shapes, and three joint types (Phase 8), a CPU-based 2D lighting system with four light types and a LightingWorld service (Phase 9), plus four major subsystems: A* grid navigation with zero-alloc pathfinding and NavAgent, extended spatial audio 2.5D with AudioMixer/AudioZone, a sprite animation state machine with ECS behaviours, and LiteNetLib-backed networking with NetworkIdentity, NetField delta sync, NetworkTransformSync, and typed message handlers (Phase 10). Combined: ~150+ classes.

### v3 Objective

v3 addresses three categories of technical debt identified after v2 reached maturity:

1. **Integration gaps** — connections that exist architecturally but are broken or absent at the usage layer (8 gaps).
2. **Missing features** — systems commonly expected in a game library that v1/v2 did not deliver (12 gaps).
3. **Architectural improvements** — scalability concerns in the lighting, ECS, and navigation subsystems that will become bottlenecks in real games.

Priority order: integration gaps first, missing features second, architecture last. No phase should be attempted out of order within a group.

---

## Transversal Rules (inherited from v1/v2, restated for completeness)

- `sealed` by default; `abstract` only for explicit base classes.
- File-scoped namespaces: `namespace Alca.MonoGame.Kernel.{Module};`
- `_camelCase` private fields, `PascalCase` public members.
- No LINQ in `Update()`/`Draw()` — indexed `for` loops only.
- No `new` of reference types in `Update()`/`Draw()` — structs are allowed.
- XML docs on all public members.
- `#nullable enable` throughout.
- Dependency Injection via `Microsoft.Extensions.DependencyInjection`.
- Every phase ends with xUnit tests under `src/Library/Alca.MonoGame.Kernel.UnitTests/` mirroring the source folder structure.
- On completion, mark the corresponding TODO in this file.

---

## GROUP A ✅ COMPLETADO — Integration Fixes

---

## PHASE 11 ✅ COMPLETADO — Critical Integration Bridges

> **Goal:** Repair the eight identified broken/missing connections between existing subsystems so they work together without manual wiring boilerplate in every game project.

**Complexity:** Simple–Medium  
**Depends on:** Phases 1–10 (all complete)

---

### Milestone 11.1 — ParticleEmitterBehaviour (Particles ↔ ECS) ✅ COMPLETADO

**Gap:** `ParticleEffectWrapper` and `ParticleBuilder` exist but are standalone; no `GameBehaviour` subclass bridges them into the ECS lifecycle. Every game must manually call `wrapper.Update()` and `wrapper.Draw()` outside the entity update loop.

**`Graphics/Particles/ParticleEmitterBehaviour.cs`** — `sealed class ParticleEmitterBehaviour : GameBehaviour`

- `Effect` (ParticleEffectWrapper, pre-created in field initializer — never null after construction)
- `BlendState` (BlendState property, default `BlendState.AlphaBlend`)
- `UseEntityPosition` (bool, default `true`) — when true, emitter follows `Entity.Transform.Position2d` each frame
- `Offset` (Vector2) — world-space offset from entity position, default `Vector2.Zero`
- `override void Update(GameTime gameTime)` — if `UseEntityPosition`: calls `Effect.Update(gameTime, Entity.Transform.Position2d + Offset)`; otherwise calls `Effect.Update(gameTime, Effect.Effect?.Position ?? Vector2.Zero)`
- `override void Draw(GameTime gameTime, SpriteBatch spriteBatch)` — calls `Effect.Draw(spriteBatch, BlendState)`
- `void Trigger()` — calls `Effect.Trigger(Entity.Transform.Position2d + Offset)`

**Integration in `Core.Update`:** none required — the behaviour is driven by the normal ECS update loop via `GameEntity._updatables`.

**Tests:** `UnitTests/Graphics/Particles/ParticleEmitterBehaviourTests.cs`
- `Update_WhenUseEntityPosition_PassesTransformPositionToWrapper`
- `Update_WithOffset_AddsOffsetToPosition`
- `Trigger_PassesEntityPositionToWrapper`
- `Draw_DelegatesToWrapper`

---

### Milestone 11.2 — Scene ↔ GameWorld lifecycle binding ✅ COMPLETADO

**Gap:** `Scene` and `GameWorld` have no automatic connection. The scene has no `World` property, so users must manually call `_world.Update(gameTime)` and `_world.Draw(gameTime, spriteBatch)` inside each scene's overrides, and manually dispose entities on scene unload.

**`Scenes/Scene.cs`** — **MODIFY**

- Add `protected GameWorld? World { get; private set; }` — nullable; scenes that do not use ECS pay zero cost.
- Add `protected virtual GameWorld? CreateWorld()` → `null` by default; override to return a configured `GameWorld`.
- Modify `Initialize()`:
  ```
  PreInitialize();
  World = CreateWorld();
  World?.InitializeWorld(); // new hook described below
  LoadContent();
  PostInitialize();
  ```
- Add `protected virtual void InitializeWorld()` — called before `LoadContent()`; subclasses populate the world with entities here.
- Modify `Update(GameTime gameTime)`:
  ```
  World?.Update(gameTime);
  ```
  Called before subclass logic (i.e., `base.Update(gameTime)` calls it). Subclasses that override `Update` must call `base.Update(gameTime)` to retain world stepping.
- Modify `Draw(GameTime gameTime)`:
  ```
  World?.Draw(gameTime, Core.SpriteBatch);
  ```
  Called before subclass draw logic.
- Modify `UnloadContent()`: adds `World?.Destroy()` call. **New method needed on `GameWorld`:**

**`ECS/GameWorld.cs`** — **MODIFY**
- Add `void Destroy()` — calls `OnDestroy()` on all entities and clears the entity list; safe to call multiple times.

**Backward compatibility:** Existing scenes that manually call `_world.Update()` continue to work because `World` is null by default unless `CreateWorld()` is overridden.

**Tests:** `UnitTests/Scenes/SceneWorldBindingTests.cs`
- `CreateWorld_WhenOverridden_WorldIsNotNull`
- `Initialize_CallsInitializeWorld`
- `Update_WhenWorldSet_CallsWorldUpdate`
- `Draw_WhenWorldSet_CallsWorldDraw`
- `UnloadContent_WhenWorldSet_CallsWorldDestroy`

---

### Milestone 11.3 — UI ↔ Scene automatic wiring ✅ COMPLETADO

**Gap:** `UIRoot` must be manually created, positioned, and connected in every scene. `Scene.InitializeUI()` exists as a hook but the `UIRoot` instance is not wired to `Draw` or to `UIOverlayManager`.

**`Scenes/Scene.cs`** — **MODIFY** (continuation of 11.2)

- Add `protected UIRoot? UIRoot { get; private set; }` — nullable; only created if subclass calls `EnableUI()`.
- Add `protected void EnableUI()` — creates `UIRoot`, sets `OverlayManager = Core.UIOverlay`, and assigns a full-screen `Bounds` from `Core.Resolution` (or fallback to `Core.GraphicsDevice.Viewport.Bounds`). Must be called in `PreInitialize()` or `Initialize()` before `InitializeUI()`.
- `InitializeUI()` is now the correct override point after `EnableUI()` has been called.
- Modify `Draw(GameTime gameTime)`: if `UIRoot is not null`, calls `UIRoot.DrawAll(Core.SpriteBatch)` after world draw (UI is always on top).
- `EnableUI()` is idempotent — calling it twice is safe.

**`Scenes/SceneManager.cs`** — **MODIFY**
- Add `UIRoot? ActiveUIRoot` property — returns `CurrentScene?.UIRoot`.
- No functional change; used by Milestone 11.4 for automatic input propagation.

**Tests:** `UnitTests/Scenes/SceneUIWiringTests.cs`
- `EnableUI_CreatesUIRoot`
- `EnableUI_SetsOverlayManager`
- `EnableUI_IsIdempotent`
- `SceneManager_ActiveUIRoot_ReturnsCurrentSceneUIRoot`
- `Draw_WhenUIRootSet_CallsDrawAll`

---

### Milestone 11.4 — Core.Update — Automatic UI input propagation ✅ COMPLETADO

**Gap:** `UIInteractionManager.Update()` must be called manually by each scene or the game class, with the right `UIRoot` and `MouseInfo` arguments. In practice this means every game project calling it differently and inconsistently.

**`Core.cs`** — **MODIFY**

Modify `Update(GameTime gameTime)`:
```csharp
// After Input.Update(gameTime) and before SceneManager.Update(gameTime):
var activeUI = SceneManager.ActiveUIRoot;
if (activeUI is not null)
    UIInteraction.Update(activeUI, Input.Mouse, UIFocus);
```

This makes `UIInteractionManager` a first-class part of the game loop, removes it from the manual call responsibility of individual scenes, and guarantees it runs after input state is refreshed but before scene logic processes events.

**Precondition:** `SceneManager.ActiveUIRoot` must be implemented (Milestone 11.3).

**Tests:** `UnitTests/Scenes/CoreUIIntegrationTests.cs`
- `Update_WhenActiveUIRootNotNull_CallsUIInteractionUpdate`
- `Update_WhenActiveUIRootNull_DoesNotThrow`

---

### Milestone 11.5 — Animation ↔ ECS test coverage ✅ COMPLETADO

**Gap:** `AnimatedSpriteBehaviour` and `AnimationStateMachineBehaviour` were implemented in v2 but their unit test files contain only tests for the non-ECS `AnimatedSprite` and `AnimationStateMachine` classes. The ECS behaviours themselves have zero test coverage.

**`UnitTests/Graphics/Sprites/AnimatedSpriteBehaviourTests.cs`** — NEW
- `Update_DelegatesToSprite_AdvancesFrameTime`
- `Draw_UsesEntityTransformPosition2d`
- `Play_SetsAnimationOnSpriteAndStartsPlayback`
- `Sprite_IsCreatedOnConstruction_NotNull`

**`UnitTests/Graphics/Sprites/AnimationStateMachineBehaviourTests.cs`** — NEW
- `StateMachine_IsCreatedOnConstruction_NotNull`
- `Update_DelegatesToStateMachine`
- `Play_DelegatesToStateMachine_ChangesCurrentState`
- `CurrentState_ReflectsStateMachineCurrentState`
- `Draw_UsesEntityTransformPosition2d`

**No source changes required.** This milestone is tests-only.

---

### Milestone 11.6 — NavGrid ↔ Physics walkability sync ✅ COMPLETADO

**Gap:** When a `RigidBody2D` with a `BoxCollider2D` or `CircleCollider2D` moves or is created, the `NavGrid` walkability data is never updated. Static geometry is generally set up once, but dynamic obstacles (doors, crates, enemies) will silently allow agents to walk through them.

**`Navigation/NavGridPhysicsSync.cs`** — `sealed class NavGridPhysicsSync`

- `_registrations` (list of `SyncEntry struct`, pre-allocated capacity 32):
  - `struct SyncEntry` — `Collider2D Collider`, `bool Walkable` (should this collider mark cells walkable or not)
- `Register(Collider2D collider, bool walkable = false)` — adds to the list; walkable=false means the collider makes cells impassable.
- `Unregister(Collider2D collider)` — removes from the list.
- `SyncAll(NavGrid grid)` — iterates all registered colliders; for each, computes the AABB in grid space, calls `grid.SetWalkable(x, y, entry.Walkable)` for all cells within the AABB. Zero external allocation: uses `ref` locals and index arithmetic.
- `SyncOne(Collider2D collider, NavGrid grid)` — updates only the cells touched by a single collider; intended for per-frame calls on moving obstacles.

**`ECS/GameWorld.cs`** — **MODIFY**
- Add `NavGridPhysicsSync? NavPhysicsSync { get; set; }` optional property.
- Modify `Update()`: after `PhysicsWorld?.Step(gameTime)`, call `NavPhysicsSync?.SyncAll(NavGrid!)` if both are non-null. Users who need fine-grained control can set `NavPhysicsSync = null` and call manually.

**Tests:** `UnitTests/Navigation/NavGridPhysicsSyncTests.cs`
- `Register_AddsColliderToSyncList`
- `Unregister_RemovesColliderFromSyncList`
- `SyncAll_WhenColliderPresentAtCell_SetsWalkableToFalse`
- `SyncOne_UpdatesOnlyAffectedCells`

---

### Milestone 11.7 — Networking ↔ Physics velocity sync ✅ COMPLETADO

**Gap:** `NetworkTransformSync` syncs position and rotation but not the physics body's linear/angular velocity. On a client, a synced entity will teleport to positions rather than smoothly continue physics motion. Physics forces and impulses applied on the authoritative peer are never replicated.

**`Network/Messages/PhysicsSyncMessage.cs`** — `sealed class PhysicsSyncMessage : INetworkMessage`
- `uint NetworkId`, `Vector2 LinearVelocity`, `float AngularVelocity`, `Vector2 Position`, `float Rotation`
- Serialized via `NetworkWriter` / `NetworkReader` (Span-based, zero-alloc)
- `MessageId` assigned as `0x0005` (next after existing system messages).

**`Network/NetworkPhysicsSync.cs`** — `sealed class NetworkPhysicsSync : GameBehaviour`
- Requires both `NetworkIdentity` and `RigidBody2D` on the same entity (validated in `Awake()`, throws `InvalidOperationException` if missing).
- `SyncInterval` (float, default `0.05f` = 20 Hz) — seconds between sends.
- `PositionThreshold` (float, default `0.05f`) — minimum position change to trigger send.
- `VelocityThreshold` (float, default `0.1f`) — minimum velocity change to trigger send.
- `InterpolateVelocity` (bool, default `true`) — on non-owner clients, lerp velocity to received value.
- `Awake()`: resolves `NetworkIdentity`, `RigidBody2D`, `NetworkServer?`, `NetworkClient?`; registers `PhysicsSyncMessage` handler.
- `Update()`: on owner, accumulates `_syncTimer`; when interval elapsed, checks thresholds, sends `PhysicsSyncMessage` via `_server.Broadcast` or `_client.Send`. On non-owner with `InterpolateVelocity`, applies received velocity each frame via lerp.
- `OnPhysicsSyncReceived(PhysicsSyncMessage msg)`: server applies authoritatively and re-broadcasts; client applies to `RigidBody2D.LinearVelocity` and `AngularVelocity`.

**Tests:** `UnitTests/Network/NetworkPhysicsSyncTests.cs`
- `Awake_WhenRigidBody2DMissing_ThrowsInvalidOperationException`
- `Awake_WhenNetworkIdentityMissing_ThrowsInvalidOperationException`
- `Update_OnOwner_SendsMessageAfterInterval`
- `OnPhysicsSyncReceived_NonOwner_AppliesVelocityToBody`
- `OnPhysicsSyncReceived_BelowThreshold_DoesNotApply`

---

## GROUP B ✅ COMPLETADO — Missing Features

---

## PHASE 12 ✅ COMPLETADO — Physics System Completion

> **Goal:** Bring the physics layer to feature parity with what game projects actually need: layer-filtered collisions and world queries (raycasts and overlaps).

**Complexity:** Medium  
**Depends on:** Phase 8 (Physics 2D)

---

### Milestone 12.1 — Collision Layers and Masks ✅ COMPLETADO

**Gap:** All colliders collide with all other colliders. There is no way to make player bullets ignore allied entities or to have trigger zones that only respond to the player.

**`Physics/CollisionCategory.cs`** — `[Flags] enum CollisionCategory : ushort`
- Pre-defined values: `None = 0x0000`, `Default = 0x0001`, `Player = 0x0002`, `Enemy = 0x0004`, `Projectile = 0x0008`, `Trigger = 0x0010`, `Terrain = 0x0020`, `All = 0xFFFF`
- Users may shadow or combine these in their own project constants.

**`Physics/Collider2D.cs`** — **MODIFY**
- Add `CollisionCategory Layer { get; set; }` (default `CollisionCategory.Default`)
- Add `CollisionCategory Mask { get; set; }` (default `CollisionCategory.All`)
- In the internal method that creates the Aether `Fixture`, pass `(ushort)Layer` and `(ushort)Mask` to `fixture.CollisionCategories` and `fixture.CollidesWith`.

**`Physics/BoxCollider2D.cs`**, **`CircleCollider2D.cs`**, **`PolygonCollider2D.cs`** — **MODIFY**
- Each passes the updated Layer/Mask through to fixture creation; no public API change.

**`Physics/CollisionMatrix.cs`** — `sealed class CollisionMatrix` (optional helper)
- `SetCanCollide(CollisionCategory a, CollisionCategory b, bool value)` — modifies the mask of all registered colliders on both sides.
- This is a utility class, not a mandatory system. Users who prefer manual mask configuration can ignore it.

**Tests:** `UnitTests/Physics/CollisionCategoryTests.cs`
- `Collider_DefaultLayer_IsDefault`
- `Collider_DefaultMask_IsAll`
- `SetLayer_UpdatesAetherFixtureCollisionCategories`
- `SetMask_UpdatesAetherFixtureCollidesWith`
- `CollisionMatrix_SetCanCollide_False_PreventsMaskOverlap`

---

### Milestone 12.2 — Physics Queries ✅ COMPLETADO

**Gap:** There is no way to cast a ray against the physics world, check if a point is inside a collider, or find all colliders within a region. Without this, AI line-of-sight, bullet hit detection, and proximity checks require manual geometry calculations outside the physics system.

**`Physics/RaycastHit2D.cs`** — `readonly struct RaycastHit2D`
- `Vector2 Point` — intersection point in world space.
- `Vector2 Normal` — surface normal at intersection.
- `float Distance` — distance from origin.
- `Collider2D? Collider` — the collider that was hit (null if query found nothing).
- `bool IsHit` — convenience: `Collider is not null`.

**`Physics/Physics2DQuery.cs`** — `sealed class Physics2DQuery`
- Constructor: `Physics2DQuery(Physics2DWorld world)` — holds reference to the `World.AetherWorld`.
- All methods are allocation-free in the output path (results passed by caller-supplied `List<T>`):
  - `bool Raycast(Vector2 origin, Vector2 direction, float maxDistance, CollisionCategory mask, out RaycastHit2D hit)` — returns the closest hit.
  - `void RaycastAll(Vector2 origin, Vector2 direction, float maxDistance, CollisionCategory mask, List<RaycastHit2D> results)` — all hits, unsorted.
  - `bool OverlapPoint(Vector2 point, CollisionCategory mask, out Collider2D? collider)` — is any collider at this point?
  - `void OverlapCircle(Vector2 center, float radius, CollisionCategory mask, List<Collider2D> results)` — all colliders whose fixtures overlap the circle.
  - `void OverlapBox(Vector2 center, Vector2 halfSize, float angle, CollisionCategory mask, List<Collider2D> results)` — all colliders overlapping an OBB.
- Internally uses `World.QueryAABB` and `World.RayCast` from Aether. Collider2D back-references recovered from `fixture.Body.Tag` (which `RigidBody2D.Awake()` already sets to the `GameEntity`; query methods retrieve `Collider2D` from the entity's component list).

**`Physics/Physics2DWorld.cs`** — **MODIFY**
- Add `Physics2DQuery Query { get; }` — created lazily on first access, passed `this`.

**Tests:** `UnitTests/Physics/Physics2DQueryTests.cs`
- `Raycast_WhenNoObstacle_ReturnsFalse`
- `Raycast_WhenBoxColliderPresent_ReturnsHit`
- `Raycast_HitDistance_MatchesExpectedValue`
- `OverlapCircle_WhenColliderInsideRadius_ReturnsIt`
- `OverlapBox_WhenColliderOutside_ReturnsEmpty`
- `RaycastAll_ReturnsMultipleHits`
- `Raycast_WithMaskFilter_IgnoresNonMatchingLayer`

---

## PHASE 13 ✅ COMPLETADO — Core Game Systems

> **Goal:** Add the three utility systems that game projects universally need but that no existing library module provides: a generic state machine, a timer/coroutine scheduler, and an improved event bus.

**Complexity:** Medium  
**Depends on:** Phase 3 (ECS core), Events

---

### Milestone 13.1 — Generic Finite State Machine ✅ COMPLETADO

**Gap:** `AnimationStateMachine` is animation-specific. There is no reusable FSM for AI behavior, game flow, UI modes, or player controllers. Game projects currently use raw `enum` + `switch` blocks, which have no lifecycle hooks.

**`StateMachine/IState.cs`** — interface
```csharp
public interface IState<TState> where TState : Enum
{
    void Enter(TState previousState);
    void Update(GameTime gameTime);
    void Exit(TState nextState);
}
```

**`StateMachine/StateMachine.cs`** — `sealed class StateMachine<TState> where TState : Enum`
- `_states` (Dictionary<TState, IState<TState>>, capacity 8 pre-allocated)
- `CurrentState` (TState, readonly) — set by `Transition()`.
- `PreviousState` (TState, readonly)
- `IsTransitioning` (bool, readonly) — true during the tick that a transition fires.
- `Register(TState id, IState<TState> state)` — adds state; throws `ArgumentException` if duplicate.
- `Transition(TState newState)` — calls `_currentStateObj.Exit(newState)`, updates state, calls `_nextStateObj.Enter(previousState)`. No-op if `newState == CurrentState`. Throws `KeyNotFoundException` if unregistered.
- `Update(GameTime gameTime)` — calls `_currentStateObj?.Update(gameTime)`.
- `HasState(TState id)` → bool.
- No heap allocation in `Transition` or `Update`.

**`StateMachine/StateMachineBehaviour.cs`** — `abstract class StateMachineBehaviour<TState> : GameBehaviour where TState : Enum`
- `StateMachine<TState> FSM { get; }` — created in field initializer.
- `override void Awake()` — calls `ConfigureStates()`.
- `protected abstract void ConfigureStates()` — subclasses call `FSM.Register(...)` here.
- `override void Update(GameTime gameTime)` — calls `FSM.Update(gameTime)`.
- `protected void Transition(TState state)` → `FSM.Transition(state)` — convenience shorthand.

**Tests:** `UnitTests/StateMachine/StateMachineTests.cs`
- `Transition_CallsExitOnCurrentAndEnterOnNext`
- `Transition_SameState_IsNoOp`
- `Transition_UnknownState_ThrowsKeyNotFoundException`
- `Register_DuplicateState_ThrowsArgumentException`
- `Update_CallsUpdateOnCurrentState`
- `PreviousState_ReflectsLastTransition`

---

### Milestone 13.2 — Timer / Scheduler System ✅ COMPLETADO

**Gap:** The only time-scheduling option is C# `Task`/`async`, which is clumsy for game-time logic (e.g., "fire this in 2 seconds of game time, respecting pause"). A lightweight game-time scheduler that pauses when the scene pauses is universally needed.

**`Timers/GameTimer.cs`** — `sealed class GameTimer`
- Internal — only created by `TimerManager`. Fields: `_elapsed`, `_interval`, `_callback`, `_maxFires`, `_fireCount`, `_isActive`, `_isPaused`, `_isRepeating`.
- `Pause()`, `Resume()`, `Cancel()` — public; cancel marks the timer for pool return.
- `bool IsDone` — true when cancelled or maxFires reached.
- `internal bool Tick(float dt)` — advances `_elapsed`, fires callback at interval threshold; returns true when done (caller returns to pool).

**`Timers/TimerManager.cs`** — `sealed class TimerManager`
- `_pool` (`GameTimer[]`, capacity 32, pre-allocated) — pool of reusable timer instances.
- `_active` (`GameTimer[]`, capacity 32, pre-allocated) — currently active timers.
- `_activeCount` (int) — number of active timers; avoids LINQ.
- `Schedule(float seconds, Action callback)` → `GameTimer` — acquires from pool, configures as one-shot.
- `ScheduleRepeating(float seconds, Action callback, int? maxFires = null)` → `GameTimer` — repeating; `maxFires = null` means infinite.
- `CancelAll()` — cancels and pools all active timers.
- `Update(GameTime gameTime)` — iterates `_active[0.._activeCount]` with a `for` loop; removes done timers (swap-and-pop with `_activeCount--`).
- Registered in `Core.cs` via DI, added to `Core.Update()` pipeline after `Input.Update()`.

**`Core.cs`** — **MODIFY**
- Add `public static TimerManager Timers { get; private set; } = null!;`
- Register `TimerManager` in `ConfigureServices`.
- Resolve and call `Timers.Update(gameTime)` in `Update()`.

**Tests:** `UnitTests/Timers/TimerManagerTests.cs`
- `Schedule_FiresCallbackAfterDelay`
- `Schedule_DoesNotFireBeforeDelay`
- `ScheduleRepeating_FiresMultipleTimes`
- `ScheduleRepeating_WithMaxFires_StopsAfterLimit`
- `Cancel_PreventsCallback`
- `CancelAll_ClearsAllTimers`
- `Pause_FreezesElapsedTime`
- `Resume_ContinuesFromPausedTime`
- `Update_WhenNoActiveTimers_DoesNotThrow`

---

### Milestone 13.3 — Event Bus Improvements ✅ COMPLETADO

**Gap:** `EventBus` is a functional global dispatcher but lacks: one-shot subscriptions (fire-and-forget handlers), priority ordering (critical handlers must run first), cancellable events (consuming an event before it reaches lower-priority handlers), and scope isolation (a scene should be able to subscribe/unsubscribe cleanly without clearing the global bus).

**`Events/EventBus.cs`** — **MODIFY** (additive; no breaking changes)

New overloads:
- `SubscribeOnce<T>(Action<T> handler)` — wraps `handler` in a lambda that calls `Unsubscribe` on first fire. No alloc at event time — the wrapper is allocated once at subscribe time.
- `SubscribeWithPriority<T>(Action<T> handler, int priority)` — stored in sorted order; `Publish` iterates highest-to-lowest priority. Default priority for existing `Subscribe` calls is `0`.

**`Events/ICancellableEvent.cs`** — interface
- `bool IsCancelled { get; set; }`

**`Events/EventBus.cs`** — **MODIFY** (continuation)
- `PublishCancellable<T>(T evt) where T : ICancellableEvent` — stops iterating once `evt.IsCancelled == true`.

**`Events/EventChannel.cs`** — `sealed class EventChannel`
- Scoped event bus. Identical API to `EventBus` (static methods become instance methods).
- `Clear()` — unsubscribes all handlers for this channel only; does not affect global `EventBus`.
- `Dispose()` — calls `Clear()`.
- Used by scenes: each `Scene` that needs isolated events creates an `EventChannel` in `Initialize()` and disposes it in `UnloadContent()`.

**Tests:** `UnitTests/Events/EventBusTests.cs`
- `SubscribeOnce_FiresOnce_ThenAutoUnsubscribes`
- `SubscribeWithPriority_HigherPriorityHandlerRunsFirst`
- `PublishCancellable_StopsDispatchAfterCancelled`
- `EventChannel_Clear_DoesNotAffectGlobalBus`
- `EventChannel_Dispose_UnsubscribesAllHandlers`

---

## PHASE 14 ✅ COMPLETADO — Developer Experience

> **Goal:** Add the debug tooling, camera helpers, and persistence framework that elevate the library from a functional engine to an ergonomic one.

**Complexity:** Simple–Complex  
**Depends on:** Phase 1 (Camera2D), Phase 2 (PrimitiveBatch), Phase 3 (TweeningManager)

---

### Milestone 14.1 — Debug Draw System ✅ COMPLETADO

**Gap:** `NavGridDebugRenderer` exists as an isolated tool, but there is no centralized system for rendering debug geometry (lines, circles, rects, text) that any subsystem can write to and that renders on top of everything else with optional frame durations. Developers must use `PrimitiveBatch` directly and manage state manually.

**`Debug/DebugCommand.cs`** — `struct DebugCommand`
- `enum DebugCommandType : byte` — `Line`, `Rect`, `Circle`, `Point`, `Text`
- `Vector2 A`, `Vector2 B` (for Line: endpoints; for Rect: position + size; for Circle: center + radius encoded in X/Y; for Point: position)
- `Color Color`
- `float Lifetime` — remaining seconds; 0 = one-frame.
- `string? Text` — only non-null for Text commands.
- `float Size` — radius for circles, size for points.

**`Debug/DebugDraw.cs`** — `static class DebugDraw`
- `_commands` (circular buffer of `DebugCommand[512]`) — pre-allocated; `DrawLine` etc. write into it with a head pointer.
- `_count` (int)
- `bool IsEnabled { get; set; }` — when false, all Draw* calls are no-ops; Update still removes expired commands.
- `DrawLine(Vector2 from, Vector2 to, Color color, float duration = 0f)`
- `DrawRect(Rectangle rect, Color color, float duration = 0f)`
- `DrawCircle(Vector2 center, float radius, Color color, int segments = 16, float duration = 0f)`
- `DrawPoint(Vector2 pos, Color color, float size = 4f, float duration = 0f)`
- `DrawText(Vector2 pos, string text, Color color, float duration = 0f)` — uses `BitmapFontRenderer` if available; otherwise skips silently.
- `Clear()` — resets `_count` to 0.
- `Update(GameTime gameTime)` — decrements `Lifetime` for all commands; packs out expired ones (in-place swap with `_count--`).
- `Draw(SpriteBatch spriteBatch, Camera2D? camera = null)` — renders using `DrawHelper` primitives. When `camera` is provided, applies camera transform. Uses indexed `for` on `_commands[0.._count]`.

**`Debug/DebugOverlay.cs`** — `sealed class DebugOverlay`
- `_watches` (fixed array of `(string label, Func<string> valueFunc)[32]`)
- `_watchCount` (int)
- `bool IsVisible { get; set; }` — toggled by `F2` (checked in `Update`).
- `AddWatch(string label, Func<string> valueFunc)` — registers a display value. The `Func<string>` is called each frame; callers should not close over allocated strings in the hot path.
- `RemoveWatch(string label)`
- `Update(GameTime gameTime)` — checks `Input.IsKeyPressed(Keys.F2)` to toggle visibility.
- `Draw(SpriteBatch spriteBatch, SpriteFont font)` — renders FPS counter + all watches; uses `SpriteBatch.DrawString`.
- `FPS` (float, readonly) — rolling average of `1 / gameTime.ElapsedGameTime.TotalSeconds`.

**Integration in `Core.cs`:** `#if DEBUG` guard around `DebugDraw.Update(gameTime)` in `Update()`. Users call `DebugDraw.Draw()` explicitly at the end of their `Draw()` override.

**Tests:** `UnitTests/Debug/DebugDrawTests.cs`
- `DrawLine_AddsCommandToBuffer`
- `Update_ReducesLifetime`
- `Update_RemovesExpiredCommands`
- `Clear_ResetsCommandCount`
- `WhenNotEnabled_DrawCommandsAreNoOps`
- `DebugOverlay_AddWatch_StoresEntry`
- `DebugOverlay_RemoveWatch_RemovesEntry`

---

### Milestone 14.2 — Camera Effects Helpers ✅ COMPLETADO

**Gap:** `TweeningManager` exists, but using it to animate the camera requires knowing internal property paths and writing boilerplate. Screen shake specifically requires a time-based oscillation that `TweeningManager` does not provide natively (it animates to a single target value). Every project reimplements camera shake from scratch.

**`Graphics/Camera/CameraEffects.cs`** — `sealed class CameraEffects`

- **Shake:**
  - `_shakeElapsed`, `_shakeDuration`, `_shakeMagnitude` (float fields)
  - `_shakeOffset` (Vector2 field, zero-alloc)
  - `Shake(Camera2D camera, float magnitude, float duration)` — begins shake; stores parameters.
  - Internally, `Update()` computes a per-frame offset using a deterministic noise function (two independent sine waves with coprime frequencies). The offset is applied to `camera.Position` each frame and subtracted before the next frame's calculation to avoid permanent drift.
- **Zoom/Pan:**
  - `ZoomTo(Camera2D camera, float targetZoom, float duration, Func<float, float>? easing = null)` — wraps `Core.Tweening.TweenTo(camera, c => c.Zoom, targetZoom, duration, easing ?? EasingCatalog.Linear)`.
  - `PanTo(Camera2D camera, Vector2 target, float duration, Func<float, float>? easing = null)` — tweens X and Y separately using two `TweenTo` calls.
- `Update(GameTime gameTime)` — advances shake timer; when duration expires, removes offset. Must be called once per frame, typically from the scene that owns the camera.
- `bool IsShaking` (bool readonly)
- `bool IsPanning` (bool readonly)

**No Core.cs registration required** — `CameraEffects` is a standalone per-camera utility; games instantiate one per camera that needs effects.

**Tests:** `UnitTests/Graphics/Camera/CameraEffectsTests.cs`
- `Shake_SetsIsShakingTrue`
- `Shake_WhenDurationExpires_IsShakingFalse`
- `Shake_OffsetIsNonZeroDuringShake`
- `ZoomTo_ChangesZoomOverTime` (integration with TweeningManager mock)
- `Update_WhenNotShaking_DoesNotMutatePosition`

---

### Milestone 14.3 — Save / Load System ✅ COMPLETADO

**Gap:** There is no persistence framework. Games must implement their own serialization, slot management, and async I/O. This is complex enough that most projects defer or implement it inconsistently.

**`Persistence/ISaveable.cs`** — interface
- `void Save(SaveDataWriter writer)`
- `void Load(SaveDataReader reader)`

**`Persistence/SaveDataWriter.cs`** — `ref struct SaveDataWriter` (Span-based, zero-alloc)
- Backed by a `Memory<byte>` leased from an `ArrayPool` per save operation.
- `Write(bool)`, `Write(int)`, `Write(float)`, `Write(double)`, `Write(string)`, `Write(Vector2)`, `Write(Vector3)`, `Write(Color)` — binary encoding, little-endian.
- `ToReadOnlySpan()` → `ReadOnlySpan<byte>` — final payload.

**`Persistence/SaveDataReader.cs`** — `ref struct SaveDataReader`
- Constructed from `ReadOnlySpan<byte>`.
- Mirrors `Write*` with `ReadBool()`, `ReadInt()`, `ReadFloat()`, etc.
- `IsAtEnd` (bool) — for safe guarded reads.

**`Persistence/SaveSlot.cs`** — `sealed class SaveSlot`
- `Name` (string), `Timestamp` (DateTimeOffset), `PlayTimeSeconds` (float), `ThumbnailPath` (string?)
- Serialized as JSON metadata alongside the binary save data.

**`Persistence/SaveManager.cs`** — `sealed class SaveManager`
- `_rootPath` (string) — base directory; defaults to `Environment.GetFolderPath(SpecialFolder.ApplicationData) + "/[GameTitle]/saves"`.
- `SaveAsync(string slotName, IEnumerable<ISaveable> objects, CancellationToken ct = default)` → `Task` — serializes all objects in order to a `MemoryStream` via `SaveDataWriter`, then writes to disk in a background `Task.Run`. Never calls MonoGame APIs from background thread.
- `LoadAsync(string slotName, IEnumerable<ISaveable> objects, CancellationToken ct = default)` → `Task<bool>` — reads binary file in background; returns to caller; caller calls `SaveDataReader` on main thread.
- `GetSlotsAsync(CancellationToken ct = default)` → `Task<IReadOnlyList<SaveSlot>>` — reads metadata JSONs.
- `DeleteSlot(string slotName)` — synchronous; deletes `.sav` and `.meta.json` files.
- `SlotExists(string slotName)` → bool

**Tests:** `UnitTests/Persistence/SaveManagerTests.cs`
- `SaveAsync_ThenLoadAsync_RoundTripsAllTypes`
- `GetSlotsAsync_ReturnsOnlyExistingSlots`
- `DeleteSlot_RemovesBothFiles`
- `LoadAsync_WhenSlotMissing_ReturnsFalse`
- `SaveDataWriter_ReadWriter_RoundTrip_Vector2`
- `SaveDataWriter_ReadWriter_RoundTrip_String`

---

## PHASE 15 ✅ COMPLETADO — Feature Completion

> **Goal:** Complete the remaining missing features: UI animation helpers, audio crossfade, navigation steering behaviors, and attribute-based network replication.

**Complexity:** Simple–Complex  
**Depends on:** Phase 5 (UI), Phase 3 (Tweening, Audio), Phase 10 (Navigation, Networking)

---

### Milestone 15.1 — UI Transitions / Animations ✅ COMPLETADO

**Gap:** `TweeningManager` is available, but there are no helpers to animate common UI transitions (fade in/out, slide in/out). Every control's show/hide cycle is hardcoded or ignored.

**`UI/Transitions/UITransitionType.cs`** — `enum UITransitionType`
- `FadeIn`, `FadeOut`, `SlideInFromLeft`, `SlideInFromRight`, `SlideInFromTop`, `SlideInFromBottom`, `SlideOutToLeft`, `SlideOutToRight`, `SlideOutToTop`, `SlideOutToBottom`

**`UI/Transitions/UITweenExtensions.cs`** — `static class UITweenExtensions`
- `FadeIn(this UIElement element, float duration, Func<float, float>? easing = null)` — tweens `element.Opacity` from 0 to 1.
- `FadeOut(this UIElement element, float duration, Func<float, float>? easing = null)` — tweens `element.Opacity` from current to 0; caller hides element on complete.
- `SlideIn(this UIElement element, Vector2 fromOffset, float duration, Func<float, float>? easing = null)` — temporarily translates `Bounds` by offset, tweens back to original position. Implementation moves `Bounds.Location` using a `_slideOffset` applied in `Arrange`.
- `SlideOut(this UIElement element, Vector2 toOffset, float duration, Func<float, float>? easing = null)`
- All methods return `ITween` (from MonoGame.Extended.Tweening) — callers can chain `.OnComplete(...)`.
- No new classes in `UIElement` — transitions operate externally on existing `Opacity` and `Bounds` properties.

**`UI/Transitions/UITransitionManager.cs`** — `sealed class UITransitionManager`
- `Play(UIElement element, UITransitionType transition, float duration, Func<float, float>? easing = null)` → `ITween`
- Internally maps `UITransitionType` to the correct `UITweenExtensions` call.

**Tests:** `UnitTests/UI/Transitions/UITweenExtensionsTests.cs`
- `FadeIn_SetsOpacityTo1AfterDuration`
- `FadeOut_SetsOpacityTo0AfterDuration`
- `SlideIn_BoundsReturnToOriginalPositionAfterDuration`
- `UITransitionManager_Play_FadeIn_DelegatesToExtension`

---

### Milestone 15.2 — Audio Crossfade ✅ COMPLETADO

**Gap:** There is no way to smoothly transition between two music tracks. `AudioController` plays/stops instances but has no concept of cross-fading. Volume transitions must be manually scripted with `TweeningManager`.

**`Audio/AudioCrossfader.cs`** — `sealed class AudioCrossfader : IDisposable`
- `_trackA`, `_trackB` (SoundEffectInstance? fields)
- `_crossfadeTimer`, `_crossfadeDuration` (float)
- `_isCrossfading` (bool)
- `CurrentVolume` (float, readonly) — volume of the currently dominant track.
- `IsCrossfading` (bool, readonly)
- `CrossfadeTo(SoundEffect newTrack, float duration, float targetVolume = 1f)` — loads `newTrack` into the inactive slot, begins fade. If already crossfading, completes the current transition instantly before beginning the new one.
- `Stop(float fadeOutDuration = 0f)` — fades out the active track. Stops immediately if `fadeOutDuration <= 0`.
- `Update(GameTime gameTime)` — advances `_crossfadeTimer`; linearly interpolates volume of both tracks (A fades out, B fades in). No allocation. When complete: disposes old track, promotes B to A.
- `Dispose()` — stops and disposes both track instances.

**`Audio/AudioController.cs`** — **MODIFY**
- Add `AudioCrossfader CreateCrossfader()` → `new AudioCrossfader()`. Factory method keeps `AudioCrossfader` creation tied to `AudioController` scope without requiring DI for per-scene instances.

**Tests:** `UnitTests/Audio/AudioCrossfaderTests.cs`
- `CrossfadeTo_SetsIsCrossfadingTrue`
- `Update_WhenCrossfadeComplete_IsCrossfadingFalse`
- `Stop_WithZeroDuration_StopsImmediately`
- `Stop_WithDuration_FadesOut`
- `CrossfadeTo_WhileAlreadyCrossfading_CompletesCurrentFirst`
- `Dispose_StopsBothTracks`

---

### Milestone 15.3 — Navigation Steering Behaviors ✅ COMPLETADO

**Gap:** `NavAgent` follows A*-computed waypoint paths but has no capacity for emergent locomotion behaviors (seeking, fleeing, separation, wander). Combining pathfinding with flocking or avoidance requires reimplementing the movement layer.

**`Navigation/Steering/ISteeringBehavior.cs`** — interface
- `Vector2 CalculateSteering(Vector2 agentPosition, Vector2 agentVelocity, GameTime gameTime)` → desired velocity contribution.

**`Navigation/Steering/SeekBehavior.cs`** — `sealed class SeekBehavior : ISteeringBehavior`
- `Target` (Vector2) — world position to seek.
- `MaxSpeed` (float)
- Returns normalized direction toward `Target` multiplied by `MaxSpeed`.

**`Navigation/Steering/FleeBehavior.cs`** — `sealed class FleeBehavior : ISteeringBehavior`
- `Target` (Vector2), `FleeRadius` (float), `MaxSpeed` (float)
- Returns zero if distance > `FleeRadius`; otherwise returns direction away from `Target * MaxSpeed`.

**`Navigation/Steering/ArriveBehavior.cs`** — `sealed class ArriveBehavior : ISteeringBehavior`
- `Target` (Vector2), `SlowRadius` (float), `MaxSpeed` (float)
- Seek with deceleration inside `SlowRadius`.

**`Navigation/Steering/WanderBehavior.cs`** — `sealed class WanderBehavior : ISteeringBehavior`
- `WanderRadius` (float, default 50f), `WanderDistance` (float, default 100f), `WanderJitter` (float, default 1f)
- `_wanderAngle` (float field) — advances each frame by random jitter, no heap alloc.

**`Navigation/Steering/SeparationBehavior.cs`** — `sealed class SeparationBehavior : ISteeringBehavior`
- `SeparationRadius` (float), `MaxSpeed` (float)
- Requires caller to populate `_neighbors` (List<Vector2>, pre-allocated, passed by the `SteeringController` each frame).
- No allocation in `CalculateSteering`.

**`Navigation/Steering/SteeringController.cs`** — `sealed class SteeringController : GameBehaviour`
- `_entries` (fixed array of `SteeringEntry struct[8]`), `_entryCount` (int)
  - `struct SteeringEntry` — `ISteeringBehavior Behavior`, `float Weight`
- `Add(ISteeringBehavior behavior, float weight = 1f)` — adds entry; throws if `_entryCount >= 8`.
- `Remove(ISteeringBehavior behavior)` — swap-and-pop.
- `MaxResultSpeed` (float, default 300f) — clamps final combined velocity.
- `ApplyToTransform` (bool, default `true`) — when true, directly moves `Entity.Transform.Position2d` each frame; when false, populates `ResultVelocity` for the caller to apply (e.g., to `RigidBody2D`).
- `ResultVelocity` (Vector2, readonly) — last-frame combined steering vector.
- `override void Update(GameTime gameTime)` — combines weighted steering outputs; applies to transform or stores in `ResultVelocity`.

**Integration with `NavAgent`:** `SteeringController` is a separate behaviour on the same entity. If `ApplyToTransform = false` and `NavAgent` is present, the NavAgent's own movement can be offset by `SteeringController.ResultVelocity` (no automatic coupling — caller composes).

**Tests:** `UnitTests/Navigation/SteeringBehaviorTests.cs`
- `SeekBehavior_ReturnsDirectionTowardTarget`
- `FleeBehavior_WhenInsideRadius_ReturnsDirectionAwayFromTarget`
- `FleeBehavior_WhenOutsideRadius_ReturnsZero`
- `ArriveBehavior_WhenInsideSlowRadius_ReducesSpeed`
- `WanderBehavior_AngleChangesEachFrame`
- `SteeringController_CombinesMultipleBehaviors`
- `SteeringController_ClampsFinalSpeedToMaxResultSpeed`

---

### Milestone 15.4 — Networking: Attribute-based Field Replication ✅ COMPLETADO

**Gap:** `NetworkIdentity.RegisterField()` requires manual registration in each component's `Awake()`. For entities with many synchronized properties (position, health, score, state flags), this produces large amounts of boilerplate. There is no way to declare "sync this property" declaratively.

**`Network/NetSync/NetSyncAttribute.cs`** — `[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]`
- `float SyncInterval` (default `-1f` = uses `NetworkIdentity.SyncInterval`)
- `NetworkChannel Channel` (default `NetworkChannel.ReliableOrdered`)
- `bool Interpolate` (default `false`)
- `Type? NetFieldType` (default `null` = auto-inferred)

**`Network/NetworkReplicator.cs`** — `sealed class NetworkReplicator : GameBehaviour`
- `Awake()` — performs one-time reflection scan:
  1. Iterates all `GameBehaviour` components on `Entity` (via `GetAllComponents()`).
  2. For each, finds properties/fields decorated with `[NetSync]`.
  3. Creates the appropriate `NetField<T>` subclass (e.g., `NetFloat`, `NetVector2`, `NetInt`) based on the member type; throws `NotSupportedException` for unsupported types.
  4. Registers each `NetField` with `NetworkIdentity.RegisterField()`.
  5. Stores `(MemberInfo, NetField, NetSyncAttribute)` triples in a pre-allocated array.
- `Update()` — for each entry: reads current member value, writes to `NetField` if changed (uses `NetField.SetValue` to trigger dirty flag). No further reflection: values read via cached `PropertyInfo.GetValue` or direct field access via `FieldInfo`.
- `OnReceive()`: copies `NetField` values back to properties (called internally via `NetworkIdentity` callback chain — requires `NetworkIdentity` to expose an `OnFieldsApplied` event).

**`Network/NetworkIdentity.cs`** — **MODIFY (minor)**
- Add `event Action? OnFieldsApplied` — raised after `ApplyTo()` completes. `NetworkReplicator` subscribes in `Awake()` to copy values back to source properties.

**Supported types for auto-inference:** `bool`, `byte`, `int`, `uint`, `float`, `double`, `string`, `Vector2`, `Vector3`. Any other type produces a `NotSupportedException` with a clear error message listing the supported types.

**Tests:** `UnitTests/Network/NetworkReplicatorTests.cs`
- `Awake_ScansBehavioursForNetSyncAttributes`
- `Awake_CreatesCorrectNetFieldType_ForFloat`
- `Awake_CreatesCorrectNetFieldType_ForVector2`
- `Awake_UnsupportedType_ThrowsNotSupportedException`
- `Update_WhenPropertyChanges_MarksDirty`
- `OnFieldsApplied_CopiesNetFieldValuesBackToProperties`

---

## GROUP C ✅ COMPLETADO — Architectural Improvements

---

## PHASE 16 ✅ COMPLETADO — Lighting: GPU-Accelerated Path

> **Goal:** Provide an opt-in shader-based lighting renderer that replaces the O(lights × queries) CPU accumulation loop with a single GPU pass. The CPU path remains fully supported.

**Complexity:** Complex  
**Depends on:** Phase 9 (LightingWorld), Phase 2 (RenderTargetManager)

---

### Milestone 16.1 — GPU Lighting Render Pipeline ✅ COMPLETADO

**Background:** The existing `LightingWorld.Resolve(Vector2, LightingLayer)` method is called per-query by the game (e.g., for each sprite draw call) and is O(L) per query. With 16+ lights and 200+ sprites, this runs thousands of light evaluation iterations per frame. A shader-based approach runs once per pixel on the GPU, independent of sprite count.

**New files:**

**`Lighting/GPU/LightShaderData.cs`** — `readonly struct LightShaderData`
- `Vector2 Position`, `float Range`, `float Intensity` — 4 floats.
- `Vector4 Color` — 4 floats.
- `int Type` — 0=Ambient, 1=Directional, 2=Point, 3=Spot.
- `float InnerAngle`, `float OuterAngle` — for SpotLight2D (0 for other types).
- `Vector2 Direction` — for Directional and Spot (zero for Point/Ambient).
- Total: 52 bytes per light, GPU-friendly layout.

**`Lighting/GPU/LightingRenderPipeline.cs`** — `sealed class LightingRenderPipeline : IDisposable`
- Constructor: `LightingRenderPipeline(GraphicsDevice graphicsDevice, LightingWorld lightingWorld, int maxLights = 64)`
- `_lightDataBuffer` (`LightShaderData[]`, pre-allocated with `maxLights` capacity) — no alloc in hot path.
- `_sceneTarget`, `_lightTarget` (`RenderTarget2D` fields, created at construction for current viewport size).
- `_lightingEffect` (`Effect`) — loaded via `LoadEffect(ContentManager content, string assetPath)`. Shader parameters:
  - `_AmbientColor` (Vector4), `_LightCount` (int), `_LightData` (array via `SetValue(float[])` from packed struct buffer), `_SceneTexture` (Texture2D sampler).
- `BeginSceneCapture()` — sets `_sceneTarget` as active render target; game renders its sprites here.
- `EndSceneCapture()` — restores back buffer.
- `ApplyLighting(LightingLayer layer, SpriteBatch spriteBatch)` — 
  1. Populates `_lightDataBuffer` from `LightingWorld` (for loop, no alloc).
  2. Sets shader parameters.
  3. Renders `_sceneTarget` with the lighting effect to the back buffer using additive blending on the light layer.
- `Resize(int width, int height)` — recreates render targets for new resolution; called from `ResolutionManager` event.
- `Dispose()` — disposes both render targets and the effect.

**Content pipeline requirement:** `LightingEffect.fx` must be authored as an MGCB content file. The roadmap specifies the required shader parameters but not the shader implementation (that is the caller's responsibility). A reference implementation should be provided as a sample in the Demo project.

**`Lighting/GPU/` directory structure:**
```
Lighting/
├── GPU/
│   ├── LightShaderData.cs
│   └── LightingRenderPipeline.cs
```

**`LightingWorld.cs`** — **MODIFY (minor)**
- Add `void FillShaderBuffer(LightShaderData[] buffer, int maxLights, LightingLayer layer, out int count)` — fills pre-allocated buffer without allocation; replaces the existing `FillShaderParameters(Effect)` (which allocates arrays). The old method is marked `[Obsolete]` but not removed.

**Tests:** Architecture-level tests only (no GPU device in unit tests):
- `LightingRenderPipeline_Constructor_DoesNotThrow` (using `GraphicsDeviceFixture`)
- `FillShaderBuffer_FillsExpectedCount`
- `FillShaderBuffer_WithLayerFilter_ExcludesOtherLayers`
- `FillShaderBuffer_WhenLightInactive_Excluded`

---

## PHASE 17 ✅ COMPLETADO — Pathfinding: Async Background Support

> **Goal:** Move long pathfinding requests off the main thread without breaking the existing synchronous `Pathfinder` API or requiring `NavAgent` changes for simple use cases.

**Complexity:** Medium  
**Depends on:** Phase 10 (Pathfinder, NavAgent)

---

### Milestone 17.1 — AsyncPathfinder ✅ COMPLETADO

**Background:** `Pathfinder.FindPath()` is synchronous and runs on the main thread. On large grids (e.g., 256×256 = 65,536 cells), a worst-case path search can take several milliseconds, causing visible frame drops. `NavGrid` data is read-only during pathfinding; mutations (walkability changes) must be externally synchronized.

**`Navigation/AsyncPathfinder.cs`** — `sealed class AsyncPathfinder : IDisposable`
- Wraps an instance of `Pathfinder`.
- `_requestChannel` (Channel<PathRequest> from `System.Threading.Channels`, bounded capacity 32) — non-blocking producer (game thread), blocking consumer (background thread).
- `_workerTask` (Task) — background `Task.Run` consuming the channel; one per `AsyncPathfinder` instance.
- `struct PathRequest` — `NavGrid Grid`, `Vector2 From`, `Vector2 To`, `NavAgentProfile Profile`, `TaskCompletionSource<NavPath?> Tcs`
- `FindPathAsync(NavGrid grid, Vector2 from, Vector2 to, NavAgentProfile profile, CancellationToken ct = default)` → `Task<NavPath?>` — posts to channel; returns a `Task` the caller can `await`.
- `Dispose()` — completes the channel, waits for the worker task, disposes resources.
- **Thread safety contract:** `NavGrid` must not be mutated while a request referencing it is in-flight. Users who modify the grid (e.g., from `NavGridPhysicsSync`) must ensure requests are not in-flight during mutation, or they must synchronize with locks (documented warning in XML doc).

**`Navigation/NavAgent.cs`** — **MODIFY**
- Add `SetDestinationAsync(Vector2 worldPosition)` → `Task<bool>` — uses `Entity.World`'s `AsyncPathfinder?` property if set; falls back to synchronous `SetDestination()` if null.
- No changes to existing `SetDestination(Vector2)` — backward compatible.

**`ECS/GameWorld.cs`** — **MODIFY**
- Add `Navigation.AsyncPathfinder? AsyncPathfinder { get; set; }` optional property.

**Tests:** `UnitTests/Navigation/AsyncPathfinderTests.cs`
- `FindPathAsync_WhenPathExists_ReturnsNonNullNavPath`
- `FindPathAsync_WhenNoPath_ReturnsNull`
- `FindPathAsync_MultipleRequests_AllComplete`
- `FindPathAsync_AfterDispose_ThrowsObjectDisposedException`
- `NavAgent_SetDestinationAsync_UseAsyncPathfinderWhenAvailable`

---

## PHASE 18 ✅ COMPLETADO — ECS Performance Pass

> **Goal:** Targeted, measurement-driven improvements to the ECS hot path. No speculative rewrites — changes are made only where benchmarks demonstrate cost.

**Complexity:** Complex  
**Depends on:** Phases 3, 7 (ECS foundations)

**Important:** This phase must begin with a benchmarking baseline. Do not implement changes until `BenchmarkDotNet` measurements establish which operations are hot. Implement only what benchmarks justify.

---

### Milestone 18.1 — Benchmark Baseline ✅ COMPLETADO

**`Benchmarks/ECS/GameWorldBenchmarks.cs`** — using `BenchmarkDotNet`
- `Benchmark_Update_100Entities`
- `Benchmark_Update_500Entities`
- `Benchmark_Update_1000Entities`
- `Benchmark_FindEntities_ByTag`
- `Benchmark_GetBehavioursWithInterface`
- `Benchmark_EntityCreation_AndDestruction`

Reference targets (60Hz budget = 16.67ms total; ECS should consume < 1ms for 1000 entities):

---

### Milestone 18.2 — Targeted Hot Path Improvements ✅ COMPLETADO

**Based on expected benchmark results, apply the following improvements if justified:**

**`ECS/GameWorld.cs`** — **MODIFY**
- `_toDestroy`: change `List<GameEntity>` to `HashSet<GameEntity>` — `Contains()` check in `Destroy()` is currently O(n); HashSet makes it O(1).
- `FindEntities<T>()`: currently uses `IEnumerable<GameEntity>` with `yield return` — add a zero-alloc overload `FindEntities<T>(List<GameEntity> results)` that uses a `for` loop. Keep the existing method for non-hot-path use. Add `[Obsolete]` warning to the IEnumerable version pointing to the new overload.
- `FindComponents<T>()`: same pattern — add `FindComponents<T>(List<T> results)` zero-alloc overload.
- Add `int EntityCount { get; }` property (returns `_entities.Count`).

**`ECS/GameEntity.cs`** — **MODIFY (if benchmarked)**
- `GetAllComponents()` currently returns `_behaviours.Values` (allocates an enumerator). If shown to be hot, change to `IReadOnlyList<GameBehaviour> GetAllComponents()` returning `_allBehavioursList`.

**No changes to ECS architecture, archetypes, or memory layout** — this is not a rewrite. The Unity-style object model is an intentional design choice.

**Tests:** Existing ECS unit tests must continue to pass with zero regressions. Benchmark results should be committed alongside this milestone as `Benchmarks/results/phase18-baseline.md`.

---

## Summary Table

| Phase | Group | Goal | New Files | Modified Files | Complexity | Depends On |
|-------|-------|------|-----------|----------------|------------|------------|
| 11.1 | A | ParticleEmitterBehaviour | 1 | 0 | Simple | 3, 10 |
| 11.2 | A | Scene ↔ GameWorld binding | 0 | 2 | Medium | 3, 7 |
| 11.3 | A | UI ↔ Scene wiring | 0 | 2 | Simple | 5, 11.2 |
| 11.4 | A | Core.Update UI propagation | 0 | 1 | Simple | 11.3 |
| 11.5 | A | Animation test coverage | 2 (tests only) | 0 | Simple | 10 |
| 11.6 | A | NavGrid ↔ Physics sync | 1 | 1 | Medium | 8, 10 |
| 11.7 | A | Networking ↔ Physics sync | 2 | 0 | Complex | 8, 10 |
| 12.1 | B | Collision layers/masks | 2 | 3 | Medium | 8 |
| 12.2 | B | Physics queries | 2 | 1 | Medium | 8, 12.1 |
| 13.1 | B | Generic FSM | 3 | 0 | Medium | 3, 7 |
| 13.2 | B | Timer/Scheduler | 2 | 1 | Medium | 3 |
| 13.3 | B | Event Bus improvements | 2 | 1 | Medium | Events |
| 14.1 | B | Debug Draw System | 2 | 1 | Medium | 1, 2 |
| 14.2 | B | Camera Effects | 1 | 0 | Simple | 1, 3 |
| 14.3 | B | Save/Load System | 5 | 0 | Complex | — |
| 15.1 | B | UI Transitions | 2 | 0 | Medium | 5, 3 |
| 15.2 | B | Audio Crossfade | 1 | 1 | Simple | 3 |
| 15.3 | B | Steering Behaviors | 7 | 0 | Medium | 10 (NavAgent) |
| 15.4 | B | Attribute-based net replication | 2 | 1 | Complex | 10 (Network) |
| 16.1 | C | GPU Lighting Pipeline | 2 | 2 | Complex | 9, 2 |
| 17.1 | C | Async Pathfinder | 1 | 2 | Medium | 10 (Nav) |
| 18.1 | C | ECS Benchmark Baseline | 1 | 0 | Simple | 3, 7 |
| 18.2 | C | ECS Hot Path Fixes | 0 | 2 | Complex | 18.1 |

**Total new files:** ~41 (production code) + ~22 (test files)

---

## Folder Structure Delta

```
src/Library/Alca.MonoGame.Kernel/
├── Debug/
│   ├── DebugCommand.cs              (new — 14.1)
│   ├── DebugDraw.cs                 (new — 14.1)
│   └── DebugOverlay.cs              (new — 14.1)
├── ECS/
│   └── GameWorld.cs                 (modify — 11.2, 17.1, 18.2)
├── Events/
│   ├── EventBus.cs                  (modify — 13.3)
│   ├── EventChannel.cs              (new — 13.3)
│   └── ICancellableEvent.cs         (new — 13.3)
├── Graphics/
│   ├── Camera/
│   │   └── CameraEffects.cs         (new — 14.2)
│   └── Particles/
│       └── ParticleEmitterBehaviour.cs (new — 11.1)
├── Lighting/
│   ├── GPU/
│   │   ├── LightShaderData.cs       (new — 16.1)
│   │   └── LightingRenderPipeline.cs (new — 16.1)
│   └── LightingWorld.cs             (modify — 16.1)
├── Navigation/
│   ├── AsyncPathfinder.cs           (new — 17.1)
│   ├── NavAgent.cs                  (modify — 17.1)
│   ├── NavGridPhysicsSync.cs        (new — 11.6)
│   └── Steering/
│       ├── ArriveBehavior.cs        (new — 15.3)
│       ├── FleeBehavior.cs          (new — 15.3)
│       ├── ISteeringBehavior.cs     (new — 15.3)
│       ├── SeekBehavior.cs          (new — 15.3)
│       ├── SeparationBehavior.cs    (new — 15.3)
│       ├── SteeringController.cs    (new — 15.3)
│       └── WanderBehavior.cs        (new — 15.3)
├── Network/
│   ├── Messages/
│   │   └── PhysicsSyncMessage.cs    (new — 11.7)
│   ├── NetSync/
│   │   └── NetSyncAttribute.cs      (new — 15.4)
│   ├── NetworkIdentity.cs           (modify — 15.4)
│   ├── NetworkPhysicsSync.cs        (new — 11.7)
│   └── NetworkReplicator.cs         (new — 15.4)
├── Persistence/
│   ├── ISaveable.cs                 (new — 14.3)
│   ├── SaveDataReader.cs            (new — 14.3)
│   ├── SaveDataWriter.cs            (new — 14.3)
│   ├── SaveManager.cs               (new — 14.3)
│   └── SaveSlot.cs                  (new — 14.3)
├── Physics/
│   ├── CollisionCategory.cs         (new — 12.1)
│   ├── CollisionMatrix.cs           (new — 12.1)
│   ├── Physics2DQuery.cs            (new — 12.2)
│   ├── Physics2DWorld.cs            (modify — 12.2)
│   ├── RaycastHit2D.cs              (new — 12.2)
│   ├── BoxCollider2D.cs             (modify — 12.1)
│   ├── CircleCollider2D.cs          (modify — 12.1)
│   └── PolygonCollider2D.cs         (modify — 12.1)
├── Scenes/
│   ├── Scene.cs                     (modify — 11.2, 11.3)
│   └── SceneManager.cs              (modify — 11.3)
├── StateMachine/
│   ├── IState.cs                    (new — 13.1)
│   ├── StateMachine.cs              (new — 13.1)
│   └── StateMachineBehaviour.cs     (new — 13.1)
├── Timers/
│   ├── GameTimer.cs                 (new — 13.2)
│   └── TimerManager.cs              (new — 13.2)
├── UI/
│   └── Transitions/
│       ├── UITransitionManager.cs   (new — 15.1)
│       ├── UITransitionType.cs      (new — 15.1)
│       └── UITweenExtensions.cs     (new — 15.1)
├── Audio/
│   └── AudioCrossfader.cs           (new — 15.2)
└── Core.cs                          (modify — 11.4, 13.2)
```

---

## Verification Checklist per Phase

- **Phase 11.1:** Add `ParticleEmitterBehaviour` to a `GameEntity` → particles follow the entity's position without any manual `Update` call.
- **Phase 11.2:** Override `CreateWorld()` in a `Scene` subclass → `World.Update()` is called automatically; `UnloadContent()` destroys all entities.
- **Phase 11.3:** Call `EnableUI()` in `PreInitialize()` → `UIRoot` exists, overlays render via `UIOverlayManager`.
- **Phase 11.4:** `dotnet test` — `UIInteractionManager.Update` is confirmed to be called in `Core.Update` by the new integration test.
- **Phase 11.5:** `dotnet test` — `AnimatedSpriteBehaviourTests` and `AnimationStateMachineBehaviourTests` all pass.
- **Phase 11.6:** Create a `BoxCollider2D` entity, assign `NavGridPhysicsSync`, call `SyncAll()` → cells under the collider report `IsWalkable = false`.
- **Phase 11.7:** Assign `NetworkPhysicsSync` to a networked entity with `RigidBody2D` → `PhysicsSyncMessage` is broadcast by the owner and `LinearVelocity` is applied on non-owner clients.
- **Phase 12.1:** Create two entities with different `CollisionCategory` layers and masks → entities that should not collide per the mask do not produce collision callbacks.
- **Phase 12.2:** `Physics2DWorld.Query.Raycast()` from origin toward a `BoxCollider2D` → returns `RaycastHit2D.IsHit = true` with correct `Distance`.
- **Phase 13.1:** `StateMachine<MyEnum>` registers three states → `Transition()` calls `Exit` on old and `Enter` on new; `Update` delegates to current.
- **Phase 13.2:** `Core.Timers.Schedule(2f, callback)` → callback fires after 2 seconds of game time; not before.
- **Phase 13.3:** `EventBus.SubscribeOnce<T>()` fires exactly once; `PublishCancellable<T>()` stops after first handler sets `IsCancelled = true`.
- **Phase 14.1:** `DebugDraw.DrawLine(...)` → line appears in game for one frame; with `duration = 2f`, persists for 2 seconds.
- **Phase 14.2:** `cameraEffects.Shake(camera, 10f, 0.5f)` → camera position oscillates for 0.5 seconds then returns to original.
- **Phase 14.3:** `SaveManager.SaveAsync("slot1", objects)` then `LoadAsync("slot1", objects)` → all `ISaveable` instances restore their state exactly.
- **Phase 15.1:** `element.FadeIn(0.3f)` → `element.Opacity` reaches 1.0 after 0.3 seconds; no manual Tweening call needed.
- **Phase 15.2:** `crossfader.CrossfadeTo(newTrack, 1.5f)` → old track fades out while new track fades in over 1.5 seconds without discontinuity.
- **Phase 15.3:** `SteeringController` with `SeekBehavior` → entity moves toward target; with `SeparationBehavior` added, does not overlap neighbors.
- **Phase 15.4:** Property decorated with `[NetSync]` on a `GameBehaviour` → `NetworkReplicator` auto-creates and registers the correct `NetField`; value syncs without manual `RegisterField()`.
- **Phase 16.1:** `LightingRenderPipeline.BeginSceneCapture()` / `ApplyLighting()` → scene renders with lighting applied via shader; CPU `Resolve()` still works as before.
- **Phase 17.1:** `AsyncPathfinder.FindPathAsync()` → path computed on background thread; no main-thread frame drop; result applied on main thread.
- **Phase 18.2:** Benchmark results committed; `FindEntities<T>(List<T>)` shows zero allocation under `BenchmarkDotNet` memory diagnoser; `_toDestroy` HashSet shows O(1) vs O(n) improvement on large worlds.

---

## Technical Reference (v3 additions to existing reference)

### New NuGet Dependencies

No new NuGet dependencies are required for any phase in this roadmap. All features use:
- `Aether.Physics2D.MG` (existing) — Phases 11.6, 11.7, 12.1, 12.2
- `LiteNetLib` (existing) — Phases 11.7, 15.4
- `MonoGame.Extended` (existing) — Phase 11.1 (particles), Phase 15.1 (tweening)
- `System.Threading.Channels` (in-box, .NET 10) — Phase 17.1
- `BenchmarkDotNet` — Phase 18.1 (test project only, not shipped in library)

### Key Design Decisions

**Why `Scene.CreateWorld()` instead of requiring a `GameWorld` constructor parameter?**
Constructor injection of `GameWorld` into `Scene` would break the existing pattern where scenes are created with `new MyScene()` in `SceneManager.RequestChange()`. A factory method keeps construction simple and makes the world optional with zero cost for non-ECS scenes.

**Why is `NavGridPhysicsSync.SyncAll()` called from `GameWorld.Update()` automatically?**
Because the primary use case is dynamic obstacles. Calling it manually from each scene would be the same boilerplate the scene-world binding was designed to eliminate. Users who need explicit control can set `GameWorld.NavPhysicsSync = null` and call `SyncAll(NavGrid)` themselves.

**Why does `SteeringController` cap at 8 behaviors?**
A fixed-capacity array avoids heap allocation on add/remove, which is the hot path rule for behaviours. Eight concurrent steering behaviors is more than any practical AI agent uses. If a project genuinely needs more, `SteeringController` is `sealed` and can be subclassed after changing the capacity constant.

**Why is `SaveDataWriter` a `ref struct`?**
To prevent it from escaping to the heap during a save operation. The writer's lifetime is scoped to a single `SaveAsync` call. Using `ref struct` enforces this at compile time and prevents accidental async capture.

**Why no `[NetSync]` support for arbitrary types beyond the 9 built-in types?**
`NetField` subclasses are manually implemented for each type because they use Span-based binary serialization with no reflection in the hot path. Supporting arbitrary types would require either reflection-based serialization (defeating the zero-alloc goal) or a `INetSerializable` interface (which would add significant API surface). The 9 supported types cover all common game data. Custom types should be decomposed into primitives.
