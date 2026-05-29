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
    Texture   = <SpriteTexture>;
    AddressU  = Clamp;
    AddressV  = Clamp;
};

float  ScanlineIntensity  = 0.25;  // darkening strength per scanline
float  BarrelDistortion   = 0.1;   // barrel/pincushion curvature
float  VignetteRadius     = 0.75;  // vignette inner radius [0,1]
float2 Resolution         = float2(320.0, 240.0);

struct VertexShaderOutput
{
    float4 Position          : SV_POSITION;
    float4 Color             : COLOR0;
    float2 TextureCoordinates: TEXCOORD0;
};

float2 BarrelUV(float2 uv)
{
    float2 cc = uv - 0.5;
    float  r2 = dot(cc, cc);
    return uv + cc * r2 * BarrelDistortion;
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
    float2 uv = BarrelUV(input.TextureCoordinates);

    // Discard pixels outside barrel warp
    if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1)
        return float4(0, 0, 0, 1);

    float4 color = tex2D(SpriteTextureSampler, uv) * input.Color;

    // Scanlines
    float scanline = fmod(floor(uv.y * Resolution.y), 2.0);
    color.rgb *= 1.0 - scanline * ScanlineIntensity;

    // Vignette
    float2 vc    = uv - 0.5;
    float  vDist = length(vc);
    float  vign  = smoothstep(1.0, VignetteRadius, vDist * 2.0);
    color.rgb   *= vign;

    return color;
}

technique SpriteDrawing
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}
