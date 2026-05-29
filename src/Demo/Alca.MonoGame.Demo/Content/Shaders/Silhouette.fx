#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

Texture2D SpriteTexture;
sampler2D SpriteTextureSampler = sampler_state
{
    Texture = <SpriteTexture>;
};

float4 SilhouetteColor = float4(0, 0, 0, 1);
float  AlphaThreshold  = 0.1;

struct VertexShaderOutput
{
    float4 Position          : SV_POSITION;
    float4 Color             : COLOR0;
    float2 TextureCoordinates: TEXCOORD0;
};

float4 MainPS(VertexShaderOutput input) : COLOR
{
    float4 texColor = tex2D(SpriteTextureSampler, input.TextureCoordinates);
    if (texColor.a < AlphaThreshold)
        return float4(0, 0, 0, 0);
    return float4(SilhouetteColor.rgb, SilhouetteColor.a * texColor.a * input.Color.a);
}

technique SpriteDrawing
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}
