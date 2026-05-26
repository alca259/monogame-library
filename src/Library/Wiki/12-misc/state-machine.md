# Máquina de Estados

**Namespace:** `Alca.MonoGame.Kernel.StateMachine`

`StateMachine<TState>` es una FSM tipada sin allocations en las transiciones. `StateMachineBehaviour<TState>` la integra como `GameBehaviour`.

---

## IState\<TState\>

Contrato de cada estado. `TState` debe ser un `enum`.

```csharp
public interface IState<TState> where TState : Enum
{
    void Enter(TState previousState)
    void Update(GameTime gameTime)
    void Exit(TState nextState)
}
```

---

## StateMachine\<TState\>

### Propiedades

| Propiedad | Tipo | Descripción |
|---|---|---|
| `CurrentState` | `TState` | Estado activo actualmente |
| `PreviousState` | `TState` | Estado antes de la última transición |
| `IsTransitioning` | `bool` | `true` durante el tick en que ocurre la transición |

### Métodos

| Método | Descripción |
|---|---|
| `Register(id, state)` | Registra un estado con su identificador |
| `Transition(newState)` | Ejecuta Exit en el estado actual → Enter en el nuevo |
| `Update(gameTime)` | Llama a `Update` del estado actual |
| `HasState(id)` | Comprueba si el estado está registrado |

---

## StateMachineBehaviour\<TState\>

`GameBehaviour` abstracto que envuelve una `StateMachine`.

```csharp
public abstract class StateMachineBehaviour<TState> : GameBehaviour where TState : Enum
{
    public StateMachine<TState> FSM { get; }

    protected abstract void ConfigureStates()     // registra los estados
    protected void Transition(TState state)        // shorthand para FSM.Transition
}
```

`Awake` llama a `ConfigureStates` automáticamente.

---

## Ejemplo: FSM de enemigo

```csharp
public enum EnemyState { Idle, Patrol, Chase, Attack, Dead }

public sealed class EnemyFSM : StateMachineBehaviour<EnemyState>
{
    protected override void ConfigureStates()
    {
        FSM.Register(EnemyState.Idle,   new IdleState(this));
        FSM.Register(EnemyState.Patrol, new PatrolState(this));
        FSM.Register(EnemyState.Chase,  new ChaseState(this));
        FSM.Register(EnemyState.Attack, new AttackState(this));
        FSM.Register(EnemyState.Dead,   new DeadState(this));
        FSM.Transition(EnemyState.Patrol);
    }

    public void OnPlayerSpotted() => Transition(EnemyState.Chase);
    public void OnPlayerInRange() => Transition(EnemyState.Attack);
    public void OnDead()          => Transition(EnemyState.Dead);
}

public sealed class PatrolState : IState<EnemyState>
{
    private readonly EnemyFSM _fsm;
    public PatrolState(EnemyFSM fsm) => _fsm = fsm;

    public void Enter(EnemyState prev) { /* iniciar waypoints */ }

    public void Update(GameTime gt)
    {
        // ... mover al siguiente waypoint ...
        if (PlayerVisible())
            _fsm.OnPlayerSpotted();
    }

    public void Exit(EnemyState next) { /* detener movimiento */ }

    private bool PlayerVisible() => false; // implementar
}
```

---

## Notas

- `StateMachine` no genera garbage en `Transition` ni en `Update`.
- No uses LINQ en `Update` o `Enter`/`Exit` de los estados.
- Para estados sin lógica (estados terminales como `Dead`), implementa `IState` con métodos vacíos.

---

## Ver también

- [ECS GameBehaviour →](../02-ecs/game-behaviour.md)
- [Event Bus →](event-bus.md)
