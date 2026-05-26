namespace Alca.MonoGame.Kernel.UI;

/// <summary>Root node of the UI visual tree. Owns the SpriteBatch.Begin/End and draws overlays last.</summary>
public class UIRoot : UIContainer
{
    /// <summary>Optional overlay manager drawn on top of the main tree inside <see cref="DrawAll"/>.</summary>
    public UIOverlayManager? OverlayManager { get; set; }

    /// <summary>Opens a SpriteBatch pass, draws the full UI tree, then draws all active overlays.</summary>
    public void DrawAll(SpriteBatch spriteBatch)
    {
        spriteBatch.Begin(
            sortMode: SpriteSortMode.Deferred,
            blendState: BlendState.AlphaBlend,
            samplerState: SamplerState.PointClamp,
            depthStencilState: null,
            rasterizerState: null);
        Draw(spriteBatch);
        OverlayManager?.Draw(spriteBatch);
        spriteBatch.End();
    }
}
