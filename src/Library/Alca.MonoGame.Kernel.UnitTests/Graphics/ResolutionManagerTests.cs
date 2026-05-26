using Alca.MonoGame.Kernel.Graphics;

namespace Alca.MonoGame.Kernel.UnitTests.Graphics;

public sealed class ResolutionManagerTests
{
    private static ResolutionManager Make(int vw = 1920, int vh = 1080) => new(vw, vh);

    [Fact]
    public void VirtualDimensions_DefaultTo1920x1080()
    {
        ResolutionManager rm = Make();
        Assert.Equal(1920, rm.VirtualWidth);
        Assert.Equal(1080, rm.VirtualHeight);
    }

    [Fact]
    public void VirtualDimensions_Custom_Roundtrip()
    {
        ResolutionManager rm = Make(800, 600);
        Assert.Equal(800, rm.VirtualWidth);
        Assert.Equal(600, rm.VirtualHeight);
    }

    [Fact]
    public void Update_ExactMatch_ScaleMatrixIsIdentityScale()
    {
        ResolutionManager rm = Make(1920, 1080);
        rm.Update(1920, 1080);
        Matrix m = rm.ScaleMatrix;
        Assert.Equal(1f, m.M11, 4);
        Assert.Equal(1f, m.M22, 4);
    }

    [Fact]
    public void Update_WiderScreen_ScaleMatrix_HasNonUniformScale()
    {
        // 2560×1080 vs virtual 1920×1080: scaleX != scaleY
        ResolutionManager rm = Make(1920, 1080);
        rm.Update(2560, 1080);

        float expectedScaleX = 2560f / 1920f;
        float expectedScaleY = 1080f / 1080f;
        Assert.Equal(expectedScaleX, rm.ScaleMatrix.M11, 4);
        Assert.Equal(expectedScaleY, rm.ScaleMatrix.M22, 4);
    }

    [Fact]
    public void Update_WiderScreen_WorldScaleMatrix_IsUniform()
    {
        // 2560×1080 vs virtual 1920×1080: uniform scale = min(scaleX, scaleY) = 1
        ResolutionManager rm = Make(1920, 1080);
        rm.Update(2560, 1080);

        float scale = MathF.Min(2560f / 1920f, 1080f / 1080f);
        Assert.Equal(scale, rm.WorldScaleMatrix.M11, 4);
        Assert.Equal(scale, rm.WorldScaleMatrix.M22, 4);
    }

    [Fact]
    public void Update_ScaleMatrix_DiffersFromWorldScaleMatrix_WhenAspectMismatch()
    {
        ResolutionManager rm = Make(1920, 1080);
        rm.Update(2560, 1080);

        Assert.NotEqual(rm.ScaleMatrix.M11, rm.WorldScaleMatrix.M11, 4);
    }

    [Fact]
    public void Update_ExactMatch_ScaleMatricesAreEqual()
    {
        ResolutionManager rm = Make(1920, 1080);
        rm.Update(1920, 1080);

        Assert.Equal(rm.ScaleMatrix.M11, rm.WorldScaleMatrix.M11, 4);
        Assert.Equal(rm.ScaleMatrix.M22, rm.WorldScaleMatrix.M22, 4);
    }

    [Fact]
    public void Update_LetterboxViewport_CenteredOnWiderScreen()
    {
        // 2560×1080 virtual 1920×1080: scale=1, viewport 1920×1080, offsetX=(2560-1920)/2=320
        ResolutionManager rm = Make(1920, 1080);
        rm.Update(2560, 1080);

        Assert.Equal(320, rm.LetterboxViewport.X);
        Assert.Equal(0,   rm.LetterboxViewport.Y);
        Assert.Equal(1920, rm.LetterboxViewport.Width);
        Assert.Equal(1080, rm.LetterboxViewport.Height);
    }

    [Fact]
    public void Update_LetterboxViewport_CenteredOnTallerScreen()
    {
        // 1920×1440 virtual 1920×1080: scaleY=1440/1080=1.333, scaleX=1, scale=1
        // viewport 1920×1080, offsetY=(1440-1080)/2=180
        ResolutionManager rm = Make(1920, 1080);
        rm.Update(1920, 1440);

        Assert.Equal(0,   rm.LetterboxViewport.X);
        Assert.Equal(180, rm.LetterboxViewport.Y);
        Assert.Equal(1920, rm.LetterboxViewport.Width);
        Assert.Equal(1080, rm.LetterboxViewport.Height);
    }

    [Fact]
    public void ScreenToVirtual_LetterboxCenter_MapsToVirtualCenter()
    {
        // 2560×1080, virtual 1920×1080: letterbox X=320, width=1920
        ResolutionManager rm = Make(1920, 1080);
        rm.Update(2560, 1080);

        Vector2 screenCenter = new(320 + 1920 / 2f, 1080 / 2f);
        Vector2 virtualPos = rm.ScreenToVirtual(screenCenter);

        Assert.Equal(960f, virtualPos.X, 1f);
        Assert.Equal(540f, virtualPos.Y, 1f);
    }

    [Fact]
    public void ScreenToVirtual_LetterboxTopLeft_MapsToVirtualOrigin()
    {
        ResolutionManager rm = Make(1920, 1080);
        rm.Update(2560, 1080);

        // Top-left of the letterbox area = (320, 0)
        Vector2 virtualPos = rm.ScreenToVirtual(new Vector2(320f, 0f));

        Assert.Equal(0f, virtualPos.X, 1f);
        Assert.Equal(0f, virtualPos.Y, 1f);
    }

    [Fact]
    public void Update_HalfSize_ScalesCorrectly()
    {
        ResolutionManager rm = Make(1920, 1080);
        rm.Update(960, 540);

        Assert.Equal(0.5f, rm.WorldScaleMatrix.M11, 4);
        Assert.Equal(0.5f, rm.WorldScaleMatrix.M22, 4);
        Assert.Equal(0f, rm.LetterboxViewport.X);
        Assert.Equal(0f, rm.LetterboxViewport.Y);
        Assert.Equal(960,  rm.LetterboxViewport.Width);
        Assert.Equal(540,  rm.LetterboxViewport.Height);
    }
}
