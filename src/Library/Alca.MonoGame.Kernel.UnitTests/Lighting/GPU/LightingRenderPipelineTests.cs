using Alca.MonoGame.Kernel.ECS;
using Alca.MonoGame.Kernel.Lighting;
using Alca.MonoGame.Kernel.Lighting.GPU;
using Alca.MonoGame.Kernel.UnitTests.Fixtures;

namespace Alca.MonoGame.Kernel.UnitTests.Lighting.GPU;

[Collection(GraphicsCollection.Name)]
public sealed class LightingRenderPipelineTests
{
    private readonly GraphicsDeviceFixture _fx;

    public LightingRenderPipelineTests(GraphicsDeviceFixture fx) => _fx = fx;

    [Fact]
    public void LightingRenderPipeline_Constructor_DoesNotThrow()
    {
        var lightingWorld = new LightingWorld();
        using var pipeline = new LightingRenderPipeline(_fx.GraphicsDevice, lightingWorld);

        Assert.NotNull(pipeline);
    }
}

public sealed class FillShaderBufferTests
{
    private static GameWorld CreateWorldWithLighting()
        => new GameWorld { LightingWorld = new LightingWorld() };

    [Fact]
    public void FillShaderBuffer_FillsExpectedCount()
    {
        var world = CreateWorldWithLighting();

        var e1 = world.CreateEntity("p1", Vector2.Zero);
        var l1 = e1.AddComponent<PointLight2D>();
        l1.Range = 100f;
        l1.Intensity = 1f;

        var e2 = world.CreateEntity("p2", new Vector2(50f, 0f));
        var l2 = e2.AddComponent<PointLight2D>();
        l2.Range = 100f;
        l2.Intensity = 1f;

        LightShaderData[] buffer = new LightShaderData[64];
        world.LightingWorld!.FillShaderBuffer(buffer, 64, LightingLayer.World, out int count);

        Assert.Equal(2, count);
    }

    [Fact]
    public void FillShaderBuffer_WithLayerFilter_ExcludesOtherLayers()
    {
        var world = CreateWorldWithLighting();

        var eWorld = world.CreateEntity("world", Vector2.Zero);
        var lWorld = eWorld.AddComponent<PointLight2D>();
        lWorld.Range = 100f;
        lWorld.Intensity = 1f;
        lWorld.LightingLayer = LightingLayer.World;

        var eUI = world.CreateEntity("ui", Vector2.Zero);
        var lUI = eUI.AddComponent<PointLight2D>();
        lUI.Range = 100f;
        lUI.Intensity = 1f;
        lUI.LightingLayer = LightingLayer.UI;

        LightShaderData[] buffer = new LightShaderData[64];
        world.LightingWorld!.FillShaderBuffer(buffer, 64, LightingLayer.World, out int count);

        Assert.Equal(1, count);
        Assert.Equal(LightShaderData.TypePoint, buffer[0].Type);
    }

    [Fact]
    public void FillShaderBuffer_WhenLightInactive_Excluded()
    {
        var world = CreateWorldWithLighting();

        var entity = world.CreateEntity("light", Vector2.Zero);
        var light = entity.AddComponent<PointLight2D>();
        light.Range = 100f;
        light.Intensity = 1f;

        entity.SetActive(false);

        LightShaderData[] buffer = new LightShaderData[64];
        world.LightingWorld!.FillShaderBuffer(buffer, 64, LightingLayer.World, out int count);

        Assert.Equal(0, count);
    }
}
