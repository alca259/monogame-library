using Alca.MonoGame.Kernel.ECS;
using Alca.MonoGame.Kernel.Lighting;

namespace Alca.MonoGame.Kernel.UnitTests.Lighting;

public sealed class PointLight2DTests
{
    private static GameWorld CreateWorldWithLighting()
        => new GameWorld { LightingWorld = new LightingWorld() };

    // ── PointLight2D ──────────────────────────────────────────────────────────

    [Fact]
    public void Resolve_WithPointLight_AtCenter_ReturnsFullIntensity()
    {
        var world = CreateWorldWithLighting();
        world.LightingWorld!.AmbientColor = Color.Black;

        var entity = world.CreateEntity("light", Vector2.Zero);
        var light = entity.AddComponent<PointLight2DBehaviour>();
        light.Color = Color.White;
        light.Intensity = 1f;
        light.Range = 100f;

        Color result = world.LightingWorld!.Resolve(Vector2.Zero, LightingLayer.World);

        Assert.Equal(Color.White, result);
    }

    [Fact]
    public void Resolve_WithPointLight_BeyondRange_ReturnsAmbientOnly()
    {
        var world = CreateWorldWithLighting();
        world.LightingWorld!.AmbientColor = Color.Black;

        var entity = world.CreateEntity("light", Vector2.Zero);
        var light = entity.AddComponent<PointLight2DBehaviour>();
        light.Color = Color.White;
        light.Intensity = 1f;
        light.Range = 100f;

        Color result = world.LightingWorld!.Resolve(new Vector2(100f, 0f), LightingLayer.World);

        Assert.Equal(Color.Black, result);
    }

    [Fact]
    public void Resolve_WithMultipleLights_AccumulatesCorrectly()
    {
        var world = CreateWorldWithLighting();
        world.LightingWorld!.AmbientColor = Color.Black;

        var e1 = world.CreateEntity("l1", Vector2.Zero);
        var l1 = e1.AddComponent<PointLight2DBehaviour>();
        l1.Color = Color.White;
        l1.Intensity = 1f;
        l1.Range = 100f;

        var e2 = world.CreateEntity("l2", Vector2.Zero);
        var l2 = e2.AddComponent<PointLight2DBehaviour>();
        l2.Color = Color.White;
        l2.Intensity = 0.5f;
        l2.Range = 100f;

        Color result = world.LightingWorld!.Resolve(Vector2.Zero, LightingLayer.World);

        // First light brings Black → White (weight 1). Second blends White → White (weight 0.5). Result stays White.
        Assert.Equal(Color.White, result);
    }

    // ── Entity deactivation ───────────────────────────────────────────────────

    [Fact]
    public void LightBehaviour_WhenEntityDeactivated_DoesNotContribute()
    {
        var world = CreateWorldWithLighting();
        world.LightingWorld!.AmbientColor = Color.Black;

        var entity = world.CreateEntity("light", Vector2.Zero);
        var light = entity.AddComponent<PointLight2DBehaviour>();
        light.Color = Color.White;
        light.Intensity = 1f;
        light.Range = 100f;

        entity.SetActive(false);

        Color result = world.LightingWorld!.Resolve(Vector2.Zero, LightingLayer.World);

        Assert.Equal(Color.Black, result);
    }

    // ── LightingWorld registration ────────────────────────────────────────────

    [Fact]
    public void LightingWorld_Register_Unregister_UpdatesLightCount()
    {
        var world = CreateWorldWithLighting();
        var lightingWorld = world.LightingWorld!;

        Assert.Equal(0, lightingWorld.LightCount);

        var entity = world.CreateEntity("light");
        entity.AddComponent<PointLight2DBehaviour>();

        Assert.Equal(1, lightingWorld.LightCount);

        world.Destroy(entity);
        world.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1.0 / 60.0)));

        Assert.Equal(0, lightingWorld.LightCount);
    }

    // ── SpotLight2D ───────────────────────────────────────────────────────────

    [Fact]
    public void SpotLight2D_OutsideCone_ReturnsZeroContribution()
    {
        var world = CreateWorldWithLighting();
        world.LightingWorld!.AmbientColor = Color.Black;

        var entity = world.CreateEntity("spot", Vector2.Zero);
        var spot = entity.AddComponent<SpotLight2DBehaviour>();
        spot.Color = Color.White;
        spot.Intensity = 1f;
        spot.Range = 200f;
        spot.InnerAngle = 10f;
        spot.OuterAngle = 20f;
        spot.Direction = Vector2.UnitX; // pointing right

        // Test a point directly behind the light (180° — well outside any cone)
        Color result = world.LightingWorld!.Resolve(new Vector2(-50f, 0f), LightingLayer.World);

        Assert.Equal(Color.Black, result);
    }

    // ── AmbientLight ──────────────────────────────────────────────────────────

    [Fact]
    public void AmbientLight_ContributesUniformly_RegardlessOfPosition()
    {
        var world = CreateWorldWithLighting();
        world.LightingWorld!.AmbientColor = Color.Black;

        var entity = world.CreateEntity("ambient");
        var ambient = entity.AddComponent<AmbientLightBehaviour>();
        ambient.Color = Color.White;
        ambient.Intensity = 1f;

        Color near = world.LightingWorld!.Resolve(Vector2.Zero, LightingLayer.World);
        Color far = world.LightingWorld!.Resolve(new Vector2(10000f, 10000f), LightingLayer.World);

        Assert.Equal(Color.White, near);
        Assert.Equal(Color.White, far);
    }

    // ── Layer filtering ───────────────────────────────────────────────────────

    [Fact]
    public void Resolve_LightOnDifferentLayer_DoesNotContribute()
    {
        var world = CreateWorldWithLighting();
        world.LightingWorld!.AmbientColor = Color.Black;

        var entity = world.CreateEntity("light", Vector2.Zero);
        var light = entity.AddComponent<PointLight2DBehaviour>();
        light.Color = Color.White;
        light.Intensity = 1f;
        light.Range = 100f;
        light.LightingLayer = LightingLayer.UI;

        // Resolving World layer — light is on UI layer → no contribution
        Color result = world.LightingWorld!.Resolve(Vector2.Zero, LightingLayer.World);

        Assert.Equal(Color.Black, result);
    }

    // ── LightContribution struct ──────────────────────────────────────────────

    [Fact]
    public void LightContribution_Add_BlendFromBlackToWhite_ReturnsWhite()
    {
        var contrib = new LightContribution(Color.Black);
        contrib.Add(Color.White, 1f);
        Assert.Equal(Color.White, contrib.Accumulated);
    }

    [Fact]
    public void LightContribution_Add_WeightZero_DoesNotChangeAccumulated()
    {
        var contrib = new LightContribution(Color.Black);
        contrib.Add(Color.White, 0f);
        Assert.Equal(Color.Black, contrib.Accumulated);
    }

    // ── GetLightsInRange ──────────────────────────────────────────────────────

    [Fact]
    public void GetLightsInRange_ReturnsLightsWithinRadius()
    {
        var world = CreateWorldWithLighting();
        var lightingWorld = world.LightingWorld!;

        var near = world.CreateEntity("near", new Vector2(10f, 0f));
        near.AddComponent<PointLight2DBehaviour>().Range = 50f;

        var far = world.CreateEntity("far", new Vector2(500f, 0f));
        far.AddComponent<PointLight2DBehaviour>().Range = 50f;

        var results = new List<LightBehaviour>();
        lightingWorld.GetLightsInRange(Vector2.Zero, 100f, LightingLayer.World, results);

        Assert.Single(results);
    }
}
