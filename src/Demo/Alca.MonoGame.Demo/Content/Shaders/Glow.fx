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

float4 GlowColor     = float4(1, 1, 0, 1);
float  GlowIntensity = 1.0;
float  GlowRadius    = 4.0;
float2 TexelSize     = float2(1.0 / 64.0, 1.0 / 64.0);

struct VertexShaderOutput
{
    float4 Position          : SV_POSITION;
    float4 Color             : COLOR0;
    float2 TextureCoordinates: TEXCOORD0;
};

float4 MainPS(VertexShaderOutput input) : COLOR
{
    float4 texColor = tex2D(SpriteTextureSampler, input.TextureCoordinates);
    float2 uv       = input.TextureCoordinates;

    // Simple 4-sample box glow — sum alpha of neighbours
    float glowAlpha = 0.0;
    float2 off      = TexelSize * GlowRadius;
    glowAlpha += tex2D(SpriteTextureSampler, uv + float2( off.x,  0)).a;
    glowAlpha += tex2D(SpriteTextureSampler, uv + float2(-off.x,  0)).a;
    glowAlpha += tex2D(SpriteTextureSampler, uv + float2( 0,  off.y)).a;
    glowAlpha += tex2D(SpriteTextureSampler, uv + float2( 0, -off.y)).a;
    glowAlpha = saturate(glowAlpha * 0.25);

    float4 glow = GlowColor * glowAlpha * GlowIntensity;
    float4 base = texColor * input.Color;

    // Additive blend of glow under the original sprite
    return saturate(base + glow * (1.0 - base.a));
}

technique SpriteDrawing
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}
