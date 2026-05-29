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

Texture2D NormalMap;
sampler2D NormalMapSampler = sampler_state
{
    Texture   = <NormalMap>;
    AddressU  = Clamp;
    AddressV  = Clamp;
};

float  NormalStrength = 1.0;
float4 AmbientColor   = float4(0.2, 0.2, 0.2, 1.0);
float3 LightPosition  = float3(0.5, 0.5, 0.075);   // normalized [0,1] screen space
float4 LightColor     = float4(1, 1, 1, 1);
float  LightRadius    = 1.0;

struct VertexShaderOutput
{
    float4 Position          : SV_POSITION;
    float4 Color             : COLOR0;
    float2 TextureCoordinates: TEXCOORD0;
};

float4 MainPS(VertexShaderOutput input) : COLOR
{
    float4 diffuse = tex2D(SpriteTextureSampler, input.TextureCoordinates) * input.Color;
    if (diffuse.a < 0.01)
        return diffuse;

    // Decode normal from [0,1] → [-1,1]
    float3 normal  = tex2D(NormalMapSampler, input.TextureCoordinates).rgb;
    normal         = normalize(normal * 2.0 - 1.0) * float3(NormalStrength, NormalStrength, 1.0);
    normal         = normalize(normal);

    // Light direction (screen space, Z = depth offset)
    float3 lightDir = normalize(LightPosition - float3(input.TextureCoordinates, 0));
    float  diff     = max(dot(normal, lightDir), 0.0);
    float  atten    = 1.0 - saturate(length(LightPosition.xy - input.TextureCoordinates) / LightRadius);

    float4 lighting = AmbientColor + LightColor * diff * atten;
    return float4(diffuse.rgb * lighting.rgb, diffuse.a);
}

technique SpriteDrawing
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}
