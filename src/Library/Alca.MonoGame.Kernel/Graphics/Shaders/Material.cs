namespace Alca.MonoGame.Kernel.Graphics.Shaders;

/// <summary>Base class for materials. Wraps a loaded <see cref="Effect"/> and exposes a typed parameter accessor.</summary>
public abstract class Material
{
    /// <summary>Gets the underlying MonoGame <see cref="Effect"/>.</summary>
    public Effect Effect { get; }

    /// <summary>Initializes the material with the given effect.</summary>
    protected Material(Effect effect)
    {
        Effect = effect;
    }

    /// <summary>Applies all shader parameters to the GPU. Call before rendering with this material.</summary>
    public abstract void Apply();

    /// <summary>Returns the <see cref="EffectParameter"/> for <paramref name="name"/>. Call the appropriate <c>GetValue*</c> method on the result.</summary>
    protected EffectParameter? GetParameter(string name) =>
        Effect.Parameters[name];
}
