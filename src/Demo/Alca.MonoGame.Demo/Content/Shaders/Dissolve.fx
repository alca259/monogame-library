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

Texture2D NoiseTexture;
sampler2D NoiseTextureSampler = sampler_state
{
    Texture   = <NoiseTexture>;
    AddressU  = Wrap;
    AddressV  = Wrap;
};

float  Progress   = 0.0;   // 0 = fully visible, 1 = fully dissolved
float4 EdgeColor  = float4(1, 0.5, 0, 1);
float  EdgeWidth  = 0.05;

struct VertexShaderOutput
{
    float4 Position          : SV_POSITION;
    float4 Color             : COLOR0;
    float2 TextureCoordinates: TEXCOORD0;
};

float4 MainPS(VertexShaderOutput input) : COLOR
{
    float4 texColor  = tex2D(SpriteTextureSampler, input.TextureCoordinates);
    float  noise     = tex2D(NoiseTextureSampler,  input.TextureCoordinates).r;

    if (noise < Progress)
        discard;

    if (noise < Progress + EdgeWidth)
        return EdgeColor * texColor.a * input.Color.a;

    return texColor * input.Color;
}

technique SpriteDrawing
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}
