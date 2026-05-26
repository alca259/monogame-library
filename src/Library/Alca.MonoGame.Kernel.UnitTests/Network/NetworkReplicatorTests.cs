using Alca.MonoGame.Kernel.ECS;
using Alca.MonoGame.Kernel.Network;
using Alca.MonoGame.Kernel.Network.NetFields;
using Alca.MonoGame.Kernel.Network.NetSync;

namespace Alca.MonoGame.Kernel.UnitTests.Network;

public sealed class NetworkReplicatorTests
{
    // ── Helpers ────────────────────────────────────────────────────────────────

    private static GameEntity MakeEntityWithIdentity()
    {
        var world = new GameWorld();
        var entity = world.CreateEntity("test");
        entity.Add(new NetworkIdentity());
        return entity;
    }

    private sealed class FloatBehaviour : GameBehaviour
    {
        [NetSync]
        public float Speed { get; set; } = 0f;
    }

    private sealed class Vector2Behaviour : GameBehaviour
    {
        [NetSync]
        public Vector2 Position { get; set; } = Vector2.Zero;
    }

    private sealed class UnsupportedBehaviour : GameBehaviour
    {
        [NetSync]
        public object? Data { get; set; }
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Awake_WhenNetworkIdentityMissing_Throws()
    {
        var world = new GameWorld();
        var entity = world.CreateEntity("test");

        Assert.Throws<InvalidOperationException>(() => entity.Add(new NetworkReplicator()));
    }

    [Fact]
    public void Awake_ScansBehavioursForNetSyncAttributes()
    {
        var entity = MakeEntityWithIdentity();
        var behaviour = new FloatBehaviour();
        entity.Add(behaviour);
        entity.Add(new NetworkReplicator());

        var identity = entity.GetComponent<NetworkIdentity>()!;
        Assert.True(identity.GetFields().Length > 0);
    }

    [Fact]
    public void Awake_CreatesCorrectNetFieldType_ForFloat()
    {
        var entity = MakeEntityWithIdentity();
        entity.Add(new FloatBehaviour());
        entity.Add(new NetworkReplicator());

        var identity = entity.GetComponent<NetworkIdentity>()!;
        NetField[] fields = identity.GetFields().ToArray();

        Assert.Single(fields);
        Assert.IsType<NetFloat>(fields[0]);
    }

    [Fact]
    public void Awake_CreatesCorrectNetFieldType_ForVector2()
    {
        var entity = MakeEntityWithIdentity();
        entity.Add(new Vector2Behaviour());
        entity.Add(new NetworkReplicator());

        var identity = entity.GetComponent<NetworkIdentity>()!;
        NetField[] fields = identity.GetFields().ToArray();

        Assert.Single(fields);
        Assert.IsType<NetVector2>(fields[0]);
    }

    [Fact]
    public void Awake_UnsupportedType_ThrowsNotSupportedException()
    {
        var entity = MakeEntityWithIdentity();
        entity.Add(new UnsupportedBehaviour());

        Assert.Throws<NotSupportedException>(() => entity.Add(new NetworkReplicator()));
    }

    [Fact]
    public void Update_WhenPropertyChanges_MarksDirty()
    {
        var entity = MakeEntityWithIdentity();
        var behaviour = new FloatBehaviour { Speed = 0f };
        entity.Add(behaviour);
        entity.Add(new NetworkReplicator());

        behaviour.Speed = 42f;
        var replicator = entity.GetComponent<NetworkReplicator>()!;
        replicator.Update(new GameTime(TimeSpan.FromSeconds(0.016), TimeSpan.FromSeconds(0.016)));

        var identity = entity.GetComponent<NetworkIdentity>()!;
        Assert.True(identity.GetFields()[0].IsDirty);
    }
}
