namespace Alca.MonoGame.Kernel.Graphics.Shaders;

/// <summary>
/// A <see cref="Material"/> that applies parameters from a <see cref="MaterialDescriptor"/> at runtime.
/// Shader parameter references are cached in the constructor; texture assets are resolved via
/// <see cref="ContentManager"/> at construction time.
/// </summary>
public sealed class DynamicMaterial : Material
{
    private readonly MaterialDescriptor _descriptor;
    private readonly Dictionary<string, EffectParameter> _paramCache;
    private readonly Dictionary<string, Texture2D> _textureCache;

    private readonly Dictionary<string, float[]> _floatOverrides = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Texture2D> _textureOverrides = new(StringComparer.Ordinal);

    /// <summary>Gets the descriptor that defines this material's properties.</summary>
    public MaterialDescriptor Descriptor => _descriptor;

    /// <summary>
    /// Initializes a <see cref="DynamicMaterial"/> from a <paramref name="descriptor"/>.
    /// All shader parameters are cached and all referenced textures are loaded synchronously.
    /// </summary>
    /// <param name="effect">The compiled MonoGame <see cref="Effect"/>.</param>
    /// <param name="descriptor">The serialized material data.</param>
    /// <param name="content">The content manager used to load texture assets.</param>
    public DynamicMaterial(Effect effect, MaterialDescriptor descriptor, ContentManager content)
        : base(effect)
    {
        _descriptor   = descriptor;
        _paramCache   = new Dictionary<string, EffectParameter>(StringComparer.Ordinal);
        _textureCache = new Dictionary<string, Texture2D>(StringComparer.Ordinal);

        foreach (EffectParameter param in effect.Parameters)
            _paramCache[param.Name] = param;

        foreach (MaterialProperty prop in descriptor.Properties.Values)
        {
            if (prop.Type == MaterialPropertyType.Texture2D && prop.TexturePath is not null)
                _textureCache[prop.Name] = content.Load<Texture2D>(prop.TexturePath);
        }
    }

    /// <inheritdoc/>
    public override void Apply()
    {
        foreach (MaterialProperty prop in _descriptor.Properties.Values)
        {
            if (!_paramCache.TryGetValue(prop.Name, out EffectParameter? param)) continue;

            // Runtime overrides take priority over descriptor defaults
            if (_floatOverrides.TryGetValue(prop.Name, out float[]? overrideData))
            {
                ApplyFloatData(param, prop.Type, overrideData);
                continue;
            }

            if (_textureOverrides.TryGetValue(prop.Name, out Texture2D? overrideTex))
            {
                param.SetValue(overrideTex);
                continue;
            }

            switch (prop.Type)
            {
                case MaterialPropertyType.Float when prop.Data is { Length: >= 1 }:
                    param.SetValue(prop.Data[0]);
                    break;
                case MaterialPropertyType.Vector2 when prop.Data is { Length: >= 2 }:
                    param.SetValue(new Microsoft.Xna.Framework.Vector2(prop.Data[0], prop.Data[1]));
                    break;
                case MaterialPropertyType.Vector3 when prop.Data is { Length: >= 3 }:
                    param.SetValue(new Microsoft.Xna.Framework.Vector3(prop.Data[0], prop.Data[1], prop.Data[2]));
                    break;
                case MaterialPropertyType.Vector4 when prop.Data is { Length: >= 4 }:
                    param.SetValue(new Microsoft.Xna.Framework.Vector4(prop.Data[0], prop.Data[1], prop.Data[2], prop.Data[3]));
                    break;
                case MaterialPropertyType.Color when prop.Data is { Length: >= 4 }:
                    param.SetValue(new Microsoft.Xna.Framework.Vector4(prop.Data[0], prop.Data[1], prop.Data[2], prop.Data[3]));
                    break;
                case MaterialPropertyType.Texture2D:
                    if (_textureCache.TryGetValue(prop.Name, out Texture2D? tex))
                        param.SetValue(tex);
                    break;
            }
        }

        Effect.CurrentTechnique.Passes[0].Apply();
    }

    /// <summary>Overrides a scalar float parameter at runtime without modifying the descriptor.</summary>
    public void SetFloat(string name, float value)
    {
        _floatOverrides[name] = [value];
    }

    /// <summary>Overrides a color parameter at runtime without modifying the descriptor.</summary>
    public void SetColor(string name, Color value)
    {
        _floatOverrides[name] = [value.R / 255f, value.G / 255f, value.B / 255f, value.A / 255f];
    }

    /// <summary>Overrides a texture parameter at runtime without modifying the descriptor.</summary>
    public void SetTexture(string name, Texture2D texture)
    {
        _textureOverrides[name] = texture;
    }

    /// <summary>Removes a previously set runtime override, reverting to the descriptor value.</summary>
    public void ClearOverride(string name)
    {
        _floatOverrides.Remove(name);
        _textureOverrides.Remove(name);
    }

    private static void ApplyFloatData(EffectParameter param, MaterialPropertyType type, float[] data)
    {
        switch (type)
        {
            case MaterialPropertyType.Float when data.Length >= 1:
                param.SetValue(data[0]);
                break;
            case MaterialPropertyType.Vector2 when data.Length >= 2:
                param.SetValue(new Microsoft.Xna.Framework.Vector2(data[0], data[1]));
                break;
            case MaterialPropertyType.Vector3 when data.Length >= 3:
                param.SetValue(new Microsoft.Xna.Framework.Vector3(data[0], data[1], data[2]));
                break;
            case MaterialPropertyType.Vector4 or MaterialPropertyType.Color when data.Length >= 4:
                param.SetValue(new Microsoft.Xna.Framework.Vector4(data[0], data[1], data[2], data[3]));
                break;
        }
    }
}
