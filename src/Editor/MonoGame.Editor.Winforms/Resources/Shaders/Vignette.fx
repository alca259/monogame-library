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

float Intensity = 1.0;

struct VertexShaderOutput
{
    float4 Position          : SV_POSITION;
    float4 Color             : COLOR0;
    float2 TextureCoordinates: TEXCOORD0;
};

float4 MainPS(VertexShaderOutput input) : COLOR
{
    float4 color = tex2D(SpriteTextureSampler, input.TextureCoordinates) * input.Color;
    float2 uv    = input.TextureCoordinates - 0.5;
    float  dist  = length(uv) * 1.4142;          // normalised 0..1 at corners
    float  vign  = 1.0 - dist * dist * Intensity;
    color.rgb   *= saturate(vign);
    return color;
}

technique SpriteDrawing
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}
