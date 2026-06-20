using Alca.MonoGame.Kernel.UI.Controls.Display;
using Alca.MonoGame.Kernel.UI.Controls.Utilities;

namespace Alca.MonoGame.Kernel.UnitTests.UI.Controls;

public sealed class UISpriteTests
{
    #region Defaults

    [Fact]
    public void NewUISprite_HasExpectedDefaults()
    {
        var sprite = new UISprite();
        Assert.Null(sprite.Texture);
        Assert.Null(sprite.SourceRect);
        Assert.Equal(Color.White, sprite.Color);
        Assert.Equal(SpriteDrawMode.Stretch, sprite.DrawMode);
    }

    #endregion

    #region Measure

    [Fact]
    public void Measure_NullTexture_ReturnsZeroDesiredSize()
    {
        var sprite = new UISprite();
        sprite.Measure(new Vector2(400f, 300f));
        Assert.Equal(Vector2.Zero, sprite.DesiredSize);
    }

    #endregion

    #region Property setters

    [Fact]
    public void DrawMode_CanBeSetAndRead()
    {
        var sprite = new UISprite { DrawMode = SpriteDrawMode.Fit };
        Assert.Equal(SpriteDrawMode.Fit, sprite.DrawMode);
    }

    [Fact]
    public void SourceRect_CanBeSetAndRead()
    {
        var rect = new Rectangle(10, 10, 50, 50);
        var sprite = new UISprite { SourceRect = rect };
        Assert.Equal(rect, sprite.SourceRect);
    }

    [Fact]
    public void Color_CanBeSetAndRead()
    {
        var sprite = new UISprite { Color = Color.Red };
        Assert.Equal(Color.Red, sprite.Color);
    }

    #endregion

    #region Texture setter invalidation

    [Fact]
    public void Texture_SetToNull_NoChange_DoesNotInvalidate()
    {
        var sprite = new UISprite();
        sprite.Arrange(new Rectangle(0, 0, 100, 100));

        sprite.Texture = null; // already null
        Assert.False(sprite.IsLayoutDirty);
    }

    #endregion
}
