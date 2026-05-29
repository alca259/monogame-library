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

float4 OutlineColor    = float4(0, 0, 0, 1);
float  OutlineThickness = 1.0;
float  AlphaThreshold  = 0.1;
float2 TexelSize       = float2(1.0 / 64.0, 1.0 / 64.0);

struct VertexShaderOutput
{
    float4 Position          : SV_POSITION;
    float4 Color             : COLOR0;
    float2 TextureCoordinates: TEXCOORD0;
};

float4 MainPS(VertexShaderOutput input) : COLOR
{
    float4 texColor = tex2D(SpriteTextureSampler, input.TextureCoordinates);

    // If this pixel is transparent, sample 4 neighbours for outline
    if (texColor.a < AlphaThreshold)
    {
        float2 uv = input.TextureCoordinates;
        float2 off = TexelSize * OutlineThickness;

        float n = tex2D(SpriteTextureSampler, uv + float2(0,  -off.y)).a;
        float s = tex2D(SpriteTextureSampler, uv + float2(0,   off.y)).a;
        float e = tex2D(SpriteTextureSampler, uv + float2( off.x, 0)).a;
        float w = tex2D(SpriteTextureSampler, uv + float2(-off.x, 0)).a;

        float border = max(max(n, s), max(e, w));
        if (border >= AlphaThreshold)
            return OutlineColor;

        return float4(0, 0, 0, 0);
    }

    return texColor * input.Color;
}

technique SpriteDrawing
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}
