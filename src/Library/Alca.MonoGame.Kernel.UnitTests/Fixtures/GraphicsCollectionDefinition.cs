namespace Alca.MonoGame.Kernel.UnitTests.Fixtures;

/// <summary>Name constant for the GPU-backed xunit collection.</summary>
public static class GraphicsCollection
{
    public const string Name = "GraphicsDevice";
}

/// <summary>Declares the "GraphicsDevice" xunit collection, sharing one <see cref="GraphicsDeviceFixture"/> across all tests in that collection.</summary>
[CollectionDefinition(GraphicsCollection.Name)]
public sealed class GraphicsCollectionDefinition : ICollectionFixture<GraphicsDeviceFixture> { }
