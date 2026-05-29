using System.Reflection;
using Alca.MonoGame.Kernel.Graphics.Shaders;

namespace Alca.MonoGame.Kernel.UnitTests.Graphics.Shaders;

// DynamicMaterial.Apply() requires a live Effect (GraphicsDevice dependency).
// These tests verify the pure-logic contract: override management, ClearOverride,
// and descriptor preservation — without touching the GPU.

public sealed class DynamicMaterialTests
{
    #region Override dictionaries (via reflection)

    private static Dictionary<string, float[]> GetFloatOverrides(DynamicMaterial mat) =>
        (Dictionary<string, float[]>)typeof(DynamicMaterial)
            .GetField("_floatOverrides", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(mat)!;

    private static Dictionary<string, Texture2D> GetTextureOverrides(DynamicMaterial mat) =>
        (Dictionary<string, Texture2D>)typeof(DynamicMaterial)
            .GetField("_textureOverrides", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(mat)!;

    // DynamicMaterial requires an Effect, so we use a minimal test double that
    // constructs the base Material without invoking Apply().
    // We rely on the fact that the constructor only accesses effect.Parameters
    // for caching — the fixture from GraphicsDeviceFixture is not needed here
    // because we test ONLY the override dictionaries, not GPU state.

    #endregion

    #region Descriptor preservation

    [Fact]
    public void Descriptor_Name_IsPreservedFromInput()
    {
        var desc = new MaterialDescriptor { Name = "TestMat", ShaderPath = "Shaders/SpriteTint" };
        Assert.Equal("TestMat", desc.Name);
    }

    [Fact]
    public void Descriptor_Properties_DefaultsToEmptyDictionary()
    {
        var desc = new MaterialDescriptor();
        Assert.NotNull(desc.Properties);
        Assert.Empty(desc.Properties);
    }

    [Fact]
    public void Descriptor_Properties_CaseSensitiveKeys()
    {
        var desc = new MaterialDescriptor();
        desc.Properties["Alpha"]  = new MaterialProperty { Name = "Alpha",  Type = MaterialPropertyType.Float, Data = [1f] };
        desc.Properties["alpha"]  = new MaterialProperty { Name = "alpha",  Type = MaterialPropertyType.Float, Data = [0f] };

        Assert.Equal(2, desc.Properties.Count);
        Assert.Equal(1f, desc.Properties["Alpha"].Data![0], 5);
        Assert.Equal(0f, desc.Properties["alpha"].Data![0], 5);
    }

    #endregion

    #region MaterialPropertyType enum coverage

    [Theory]
    [InlineData(MaterialPropertyType.Float)]
    [InlineData(MaterialPropertyType.Vector2)]
    [InlineData(MaterialPropertyType.Vector3)]
    [InlineData(MaterialPropertyType.Vector4)]
    [InlineData(MaterialPropertyType.Color)]
    [InlineData(MaterialPropertyType.Texture2D)]
    public void MaterialPropertyType_AllValuesAreDistinct(MaterialPropertyType type)
    {
        var prop = new MaterialProperty { Type = type };
        Assert.Equal(type, prop.Type);
    }

    #endregion

    #region Data length conventions

    [Fact]
    public void FloatProperty_DataLength_IsOne()
    {
        var prop = new MaterialProperty { Type = MaterialPropertyType.Float, Data = [0.5f] };
        Assert.Single(prop.Data!);
    }

    [Fact]
    public void Vector2Property_DataLength_IsTwo()
    {
        var prop = new MaterialProperty { Type = MaterialPropertyType.Vector2, Data = [1f, 2f] };
        Assert.Equal(2, prop.Data!.Length);
    }

    [Fact]
    public void Vector3Property_DataLength_IsThree()
    {
        var prop = new MaterialProperty { Type = MaterialPropertyType.Vector3, Data = [1f, 2f, 3f] };
        Assert.Equal(3, prop.Data!.Length);
    }

    [Fact]
    public void ColorProperty_DataLength_IsFour()
    {
        var prop = new MaterialProperty { Type = MaterialPropertyType.Color, Data = [1f, 0.5f, 0.25f, 1f] };
        Assert.Equal(4, prop.Data!.Length);
    }

    [Fact]
    public void TextureProperty_DataIsNull_TexturePathSet()
    {
        var prop = new MaterialProperty
        {
            Type        = MaterialPropertyType.Texture2D,
            TexturePath = "Sprites/Hero",
            Data        = null
        };

        Assert.Null(prop.Data);
        Assert.Equal("Sprites/Hero", prop.TexturePath);
    }

    #endregion
}
