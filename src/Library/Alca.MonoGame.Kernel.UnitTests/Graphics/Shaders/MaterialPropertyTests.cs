using System.Text.Json;
using Alca.MonoGame.Kernel.Graphics.Shaders;

namespace Alca.MonoGame.Kernel.UnitTests.Graphics.Shaders;

public sealed class MaterialPropertyTests
{
    #region Default values

    [Fact]
    public void NewProperty_HasEmptyName()
    {
        var prop = new MaterialProperty();
        Assert.Equal(string.Empty, prop.Name);
    }

    [Fact]
    public void NewProperty_DataIsNull()
    {
        var prop = new MaterialProperty();
        Assert.Null(prop.Data);
    }

    [Fact]
    public void NewProperty_TexturePathIsNull()
    {
        var prop = new MaterialProperty();
        Assert.Null(prop.TexturePath);
    }

    #endregion

    #region Serialization round-trip

    [Fact]
    public void Serialize_FloatProperty_RoundTrips()
    {
        var original = new MaterialProperty
        {
            Name = "_Alpha",
            Type = MaterialPropertyType.Float,
            Data = [0.75f]
        };

        string json   = JsonSerializer.Serialize(original);
        var restored  = JsonSerializer.Deserialize<MaterialProperty>(json)!;

        Assert.Equal(original.Name,    restored.Name);
        Assert.Equal(original.Type,    restored.Type);
        Assert.NotNull(restored.Data);
        Assert.Equal(0.75f, restored.Data![0], 5);
    }

    [Fact]
    public void Serialize_ColorProperty_PreservesAllChannels()
    {
        var original = new MaterialProperty
        {
            Name = "_Color",
            Type = MaterialPropertyType.Color,
            Data = [1f, 0.5f, 0.25f, 1f]
        };

        string json  = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<MaterialProperty>(json)!;

        Assert.Equal(4, restored.Data!.Length);
        Assert.Equal(1f,    restored.Data[0], 5);
        Assert.Equal(0.5f,  restored.Data[1], 5);
        Assert.Equal(0.25f, restored.Data[2], 5);
        Assert.Equal(1f,    restored.Data[3], 5);
    }

    [Fact]
    public void Serialize_TextureProperty_RoundTrips()
    {
        var original = new MaterialProperty
        {
            Name        = "_MainTex",
            Type        = MaterialPropertyType.Texture2D,
            TexturePath = "Sprites/Hero"
        };

        string json  = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<MaterialProperty>(json)!;

        Assert.Equal("_MainTex",       restored.Name);
        Assert.Equal(MaterialPropertyType.Texture2D, restored.Type);
        Assert.Equal("Sprites/Hero",   restored.TexturePath);
        Assert.Null(restored.Data);
    }

    #endregion

    #region MaterialDescriptor serialization

    [Fact]
    public void Serialize_MaterialDescriptor_RoundTrips()
    {
        var descriptor = new MaterialDescriptor
        {
            Name       = "HeroArmor",
            ShaderPath = "Shaders/SpriteTint",
            Properties =
            {
                ["Alpha"] = new MaterialProperty { Name = "Alpha", Type = MaterialPropertyType.Float, Data = [0.9f] },
                ["_Color"] = new MaterialProperty { Name = "_Color", Type = MaterialPropertyType.Color, Data = [1f, 0.8f, 0.8f, 1f] }
            }
        };

        string json  = JsonSerializer.Serialize(descriptor);
        var restored = JsonSerializer.Deserialize<MaterialDescriptor>(json)!;

        Assert.Equal("HeroArmor",         restored.Name);
        Assert.Equal("Shaders/SpriteTint", restored.ShaderPath);
        Assert.Equal(2, restored.Properties.Count);
        Assert.True(restored.Properties.ContainsKey("Alpha"));
        Assert.Equal(0.9f, restored.Properties["Alpha"].Data![0], 5);
    }

    [Fact]
    public void Serialize_EmptyDescriptor_RoundTrips()
    {
        var descriptor = new MaterialDescriptor { Name = "Empty", ShaderPath = "Shaders/Default" };
        string json  = JsonSerializer.Serialize(descriptor);
        var restored = JsonSerializer.Deserialize<MaterialDescriptor>(json)!;

        Assert.Equal("Empty", restored.Name);
        Assert.Empty(restored.Properties);
    }

    #endregion
}
