using Alca.MonoGame.Kernel.UI;

namespace Alca.MonoGame.Kernel.UnitTests.UI;

public sealed class NineSliceBorderDataTests
{
    #region Uniform factory

    [Fact]
    public void Uniform_SetsAllEdgesToSameValue()
    {
        NineSliceBorderData b = NineSliceBorderData.Uniform(12);
        Assert.Equal(12, b.Left);
        Assert.Equal(12, b.Right);
        Assert.Equal(12, b.Top);
        Assert.Equal(12, b.Bottom);
    }

    [Fact]
    public void Uniform_TileEdgesAndCenter_DefaultFalse()
    {
        NineSliceBorderData b = NineSliceBorderData.Uniform(4);
        Assert.False(b.TileEdges);
        Assert.False(b.TileCenter);
    }

    [Fact]
    public void Uniform_ZeroBorder_IsValid()
    {
        NineSliceBorderData b = NineSliceBorderData.Uniform(0);
        Assert.Equal(0, b.Left);
        Assert.Equal(0, b.Right);
        Assert.Equal(0, b.Top);
        Assert.Equal(0, b.Bottom);
    }

    #endregion

    #region Asymmetric borders

    [Fact]
    public void Init_AsymmetricBorders_PreservesIndividualValues()
    {
        NineSliceBorderData b = new() { Left = 2, Right = 5, Top = 8, Bottom = 11 };
        Assert.Equal(2,  b.Left);
        Assert.Equal(5,  b.Right);
        Assert.Equal(8,  b.Top);
        Assert.Equal(11, b.Bottom);
    }

    [Fact]
    public void Init_TileEdgesTrue_Preserved()
    {
        NineSliceBorderData b = new() { Left = 4, Right = 4, Top = 4, Bottom = 4, TileEdges = true };
        Assert.True(b.TileEdges);
        Assert.False(b.TileCenter);
    }

    [Fact]
    public void Init_TileCenterTrue_Preserved()
    {
        NineSliceBorderData b = new() { Left = 4, Right = 4, Top = 4, Bottom = 4, TileCenter = true };
        Assert.False(b.TileEdges);
        Assert.True(b.TileCenter);
    }

    [Fact]
    public void Init_BothTileFlags_CanBothBeTrue()
    {
        NineSliceBorderData b = new() { Left = 4, Right = 4, Top = 4, Bottom = 4, TileEdges = true, TileCenter = true };
        Assert.True(b.TileEdges);
        Assert.True(b.TileCenter);
    }

    #endregion

    #region Readonly struct immutability

    [Fact]
    public void Struct_IsImmutable_AfterInit()
    {
        NineSliceBorderData original = NineSliceBorderData.Uniform(8);
        NineSliceBorderData copy = original; // value copy
        Assert.Equal(original.Left,   copy.Left);
        Assert.Equal(original.Right,  copy.Right);
        Assert.Equal(original.Top,    copy.Top);
        Assert.Equal(original.Bottom, copy.Bottom);
    }

    #endregion
}
