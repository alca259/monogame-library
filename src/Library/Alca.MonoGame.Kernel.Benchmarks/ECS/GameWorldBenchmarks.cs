using Alca.MonoGame.Kernel.ECS;
using BenchmarkDotNet.Attributes;
using Microsoft.Xna.Framework;

namespace Alca.MonoGame.Kernel.Benchmarks.ECS;

/// <summary>
/// Establishes the performance baseline for the ECS hot path.
/// Reference target: ECS should consume under 1ms for 1000 entities at 60Hz (16.67ms budget).
/// Run with: dotnet run -c Release -- --job short --filter *GameWorldBenchmarks*
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class GameWorldBenchmarks
{
    private GameWorld _world100 = null!;
    private GameWorld _world500 = null!;
    private GameWorld _world1000 = null!;

    private static readonly GameTime OneFrame = new(
        TimeSpan.FromSeconds(1.0 / 60.0),
        TimeSpan.FromSeconds(1.0 / 60.0));

    [GlobalSetup]
    public void Setup()
    {
        _world100  = BuildWorld(100);
        _world500  = BuildWorld(500);
        _world1000 = BuildWorld(1000);

        // Flush pending entities into the active list
        _world100.Update(OneFrame);
        _world500.Update(OneFrame);
        _world1000.Update(OneFrame);
    }

    [Benchmark]
    public void Benchmark_Update_100Entities()
        => _world100.Update(OneFrame);

    [Benchmark]
    public void Benchmark_Update_500Entities()
        => _world500.Update(OneFrame);

    [Benchmark]
    public void Benchmark_Update_1000Entities()
        => _world1000.Update(OneFrame);

    [Benchmark]
    public int Benchmark_FindEntities_ByTag()
    {
        var results = new List<GameEntity>();
        _world1000.FindEntities<NullBehaviour>(results);
        int count = results.Count;
        results.Clear();
        return count;
    }

    [Benchmark]
    public int Benchmark_GetBehavioursWithInterface()
    {
        var results = new List<IBenchmarkInterface>();
        _world1000.GetBehavioursWithInterface(results);
        int count = results.Count;
        results.Clear();
        return count;
    }

    [Benchmark]
    public void Benchmark_EntityCreation_AndDestruction()
    {
        var world = new GameWorld();
        for (int i = 0; i < 100; i++)
            world.Destroy(world.CreateEntity($"e{i}"));
        world.Update(OneFrame);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static GameWorld BuildWorld(int entityCount)
    {
        var world = new GameWorld();
        for (int i = 0; i < entityCount; i++)
        {
            var entity = world.CreateEntity($"entity_{i}");
            entity.Add(new NullBehaviour());
        }
        return world;
    }
}

/// <summary>Minimal behaviour used for benchmark worlds — zero Update cost.</summary>
public sealed class NullBehaviour : GameBehaviour, IBenchmarkInterface
{
    public override void Update(GameTime gameTime) { }
}

/// <summary>Marker interface used in <see cref="GameWorldBenchmarks.Benchmark_GetBehavioursWithInterface"/>.</summary>
public interface IBenchmarkInterface { }
