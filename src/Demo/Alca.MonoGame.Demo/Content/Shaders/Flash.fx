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

float4 FlashColor     = float4(1, 1, 1, 1);
float  FlashIntensity = 0.0;

struct VertexShaderOutput
{
    float4 Position          : SV_POSITION;
    float4 Color             : COLOR0;
    float2 TextureCoordinates: TEXCOORD0;
};

float4 MainPS(VertexShaderOutput input) : COLOR
{
    float4 texColor = tex2D(SpriteTextureSampler, input.TextureCoordinates);
    float4 result   = texColor * input.Color;
    // Blend toward flash color based on intensity, preserve alpha
    result.rgb = lerp(result.rgb, FlashColor.rgb, FlashIntensity * FlashColor.a);
    return result;
}

technique SpriteDrawing
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}
