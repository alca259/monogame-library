# Phase 18 — ECS Performance Baseline

**Date:** 2026-05-26  
**Host:** Windows 10 Pro 10.0.19045 / .NET 10.0  
**Reference budget:** < 1ms for 1000 entities at 60Hz (16.67ms total frame budget)

## Benchmark configuration

```
BenchmarkDotNet v0.14.*
Runtime = .NET 10.0
Job = Short
```

## Results summary

| Method | Entities | Mean | Alloc |
|---|---|---|---|
| `Benchmark_Update_100Entities` | 100 | ~0.04ms | 0 B |
| `Benchmark_Update_500Entities` | 500 | ~0.20ms | 0 B |
| `Benchmark_Update_1000Entities` | 1000 | ~0.40ms | 0 B |
| `Benchmark_FindEntities_ByTag` | 1000 | ~0.10ms | 0 B |
| `Benchmark_GetBehavioursWithInterface` | 1000 | ~0.15ms | 0 B |
| `Benchmark_EntityCreation_AndDestruction` | 100 create+destroy | ~0.05ms | — |

*Note: values above are design-time estimates; run `dotnet run -c Release` in the Benchmarks project for actual measurements.*

## Hot-path improvements applied (Phase 18.2)

| Change | File | Justification |
|---|---|---|
| `_toDestroy: List → HashSet` | `GameWorld.cs` | O(1) duplicate check vs O(n) `Contains()` |
| `FindEntities<T>(List<T>)` zero-alloc overload | `GameWorld.cs` | Eliminates enumerator allocation in hot queries |
| `FindComponents<T>(List<T>)` zero-alloc overload | `GameWorld.cs` | Eliminates enumerator allocation in hot queries |
| `EntityCount` property | `GameWorld.cs` | Direct `_entities.Count` without LINQ |
| `GetAllComponents() → IReadOnlyList<GameBehaviour>` | `GameEntity.cs` | Returns pre-allocated list, no dictionary enumerator |

## Notes

- `NullBehaviour.Update()` is a no-op; real-world costs will be higher proportional to behaviour complexity.
- Run benchmarks with `dotnet run -c Release -- --job short --filter *GameWorldBenchmarks*` from the `Alca.MonoGame.Kernel.Benchmarks` project.
