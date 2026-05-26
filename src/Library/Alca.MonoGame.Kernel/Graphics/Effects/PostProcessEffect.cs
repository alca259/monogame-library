namespace Alca.MonoGame.Kernel.Graphics.Effects;

/// <summary>Base class for post-processing effects. Wraps a loaded <see cref="Effect"/> and sets its parameters before applying.</summary>
public abstract class PostProcessEffect
{
    /// <summary>Gets the underlying MonoGame <see cref="Effect"/>.</summary>
    public Effect Effect { get; }

    /// <summary>Initializes the post-process effect with the given loaded effect.</summary>
    protected PostProcessEffect(Effect effect)
    {
        Effect = effect;
    }

    /// <summary>Sets all shader parameters before rendering. Override to push custom uniforms to the GPU.</summary>
    public abstract void SetParameters();

    /// <summary>Calls <see cref="SetParameters"/> then delegates to <paramref name="rtm"/> to apply the effect.</summary>
    public void Apply(RenderTargetManager rtm, SpriteBatch spriteBatch)
    {
        SetParameters();
        rtm.Apply(Effect, spriteBatch);
    }
}
