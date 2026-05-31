// StandardEffect.fx
// PBR-lite (Blinn-Phong with metallic/smoothness workflow).
// Mirrors Unity's Standard Shader property set.
// Add this file to your MGCB with processor "Effect - MonoGame".

#if OPENGL
    #define SV_POSITION  POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// ─── Matrices ────────────────────────────────────────────────────────────────

float4x4 WorldViewProjection;
float4x4 World;
float3   CameraPosition = float3(0, 0, 1);

// ─── Lighting ────────────────────────────────────────────────────────────────

float3 LightDirection = float3(0.0, -0.7, 0.7); // normalised in PS
float3 LightColor     = float3(1.0, 1.0, 1.0);
float3 AmbientColor   = float3(0.2, 0.2, 0.2);

// ─── Main Maps ───────────────────────────────────────────────────────────────

Texture2D AlbedoTexture;
float4    AlbedoColor   = float4(1, 1, 1, 1);
float     HasAlbedoMap  = 0.0;

Texture2D MetallicTexture;  // R = metallic, A = smoothness
float     Metallic        = 0.0;
float     Smoothness      = 0.5;
float     HasMetallicMap  = 0.0;

Texture2D NormalTexture;
float     NormalScale   = 1.0;
float     HasNormalMap  = 0.0;

Texture2D HeightTexture;
float     HeightScale   = 0.02;
float     HasHeightMap  = 0.0;

Texture2D OcclusionTexture;
float     OcclusionStrength = 1.0;
float     HasOcclusionMap   = 0.0;

Texture2D EmissionTexture;
float4    EmissionColor     = float4(0, 0, 0, 0);
float     EmissionIntensity = 0.0;
float     HasEmissionMap    = 0.0;

Texture2D DetailMaskTexture;
float     HasDetailMask = 0.0;

// ─── UV Controls (Main) ──────────────────────────────────────────────────────

float2 Tiling = float2(1, 1);
float2 Offset = float2(0, 0);

// ─── Secondary Maps ──────────────────────────────────────────────────────────

Texture2D DetailAlbedoTexture;
float     HasDetailAlbedoMap  = 0.0;

Texture2D DetailNormalTexture;
float     DetailNormalScale   = 1.0;
float     HasDetailNormalMap  = 0.0;

float2 DetailTiling = float2(1, 1);
float2 DetailOffset = float2(0, 0);

// ─── Alpha ───────────────────────────────────────────────────────────────────

float AlphaCutoff = 0.5;

// ─── Samplers ────────────────────────────────────────────────────────────────

sampler2D AlbedoSampler = sampler_state
{
    Texture = <AlbedoTexture>;
    MinFilter = Linear; MagFilter = Linear; MipFilter = Linear;
    AddressU  = Wrap;   AddressV  = Wrap;
};

sampler2D MetallicSampler = sampler_state
{
    Texture = <MetallicTexture>;
    MinFilter = Linear; MagFilter = Linear; MipFilter = Linear;
    AddressU  = Wrap;   AddressV  = Wrap;
};

sampler2D NormalSampler = sampler_state
{
    Texture = <NormalTexture>;
    MinFilter = Linear; MagFilter = Linear; MipFilter = Linear;
    AddressU  = Wrap;   AddressV  = Wrap;
};

sampler2D OcclusionSampler = sampler_state
{
    Texture = <OcclusionTexture>;
    MinFilter = Linear; MagFilter = Linear; MipFilter = Linear;
    AddressU  = Wrap;   AddressV  = Wrap;
};

sampler2D EmissionSampler = sampler_state
{
    Texture = <EmissionTexture>;
    MinFilter = Linear; MagFilter = Linear; MipFilter = Linear;
    AddressU  = Wrap;   AddressV  = Wrap;
};

sampler2D DetailAlbedoSampler = sampler_state
{
    Texture = <DetailAlbedoTexture>;
    MinFilter = Linear; MagFilter = Linear; MipFilter = Linear;
    AddressU  = Wrap;   AddressV  = Wrap;
};

sampler2D DetailNormalSampler = sampler_state
{
    Texture = <DetailNormalTexture>;
    MinFilter = Linear; MagFilter = Linear; MipFilter = Linear;
    AddressU  = Wrap;   AddressV  = Wrap;
};

// ─── Structs ─────────────────────────────────────────────────────────────────

struct VSInput
{
    float4 Position : POSITION;
    float3 Normal   : NORMAL;
    float2 TexCoord : TEXCOORD0;
};

struct PSInput
{
    float4 Position    : SV_POSITION;
    float3 WorldNormal : TEXCOORD0;
    float2 TexCoord    : TEXCOORD1;
    float3 WorldPos    : TEXCOORD2;
    float3 Tangent     : TEXCOORD3;
    float3 Bitangent   : TEXCOORD4;
};

// ─── Vertex Shader ───────────────────────────────────────────────────────────

PSInput VS(VSInput input)
{
    PSInput output;
    output.Position    = mul(input.Position, WorldViewProjection);
    output.WorldPos    = mul(input.Position, World).xyz;
    output.TexCoord    = input.TexCoord * Tiling + Offset;

    float3 worldN = normalize(mul(input.Normal, (float3x3)World));
    output.WorldNormal = worldN;

    // Compute tangent frame from the normal (good approximation for sphere/procedural meshes).
    float3 up      = abs(worldN.y) < 0.999 ? float3(0, 1, 0) : float3(1, 0, 0);
    float3 tangent = normalize(cross(up, worldN));
    output.Tangent    = tangent;
    output.Bitangent  = cross(worldN, tangent);

    return output;
}

// ─── Helpers ─────────────────────────────────────────────────────────────────

float3 FresnelSchlick(float cosTheta, float3 F0)
{
    return F0 + (1.0 - F0) * pow(max(1.0 - cosTheta, 0.0001), 5.0);
}

// ─── Shared Pixel Computation ────────────────────────────────────────────────
// Returns RGBA where alpha is the source albedo alpha (callers may override it).

float4 ComputeShading(PSInput input)
{
    float2 uv       = input.TexCoord;
    float2 detailUV = uv * DetailTiling + DetailOffset;

    // ── Albedo ──────────────────────────────────────────────────────────────
    float4 albedoTex = tex2D(AlbedoSampler, uv);
    float4 albedo    = AlbedoColor * lerp(float4(1, 1, 1, 1), albedoTex, HasAlbedoMap);

    // ── Metallic / Smoothness ────────────────────────────────────────────────
    float4 metallicTex = tex2D(MetallicSampler, uv);
    float  metallic    = lerp(Metallic,   metallicTex.r, HasMetallicMap);
    float  smoothness  = lerp(Smoothness, metallicTex.a, HasMetallicMap);
    float  roughness   = max((1.0 - smoothness) * (1.0 - smoothness), 0.001);

    // ── Normal (perturb geometric normal via tangent-space map) ──────────────
    float3 worldN    = normalize(input.WorldNormal);
    float3 normalTex = tex2D(NormalSampler, uv).rgb * 2.0 - 1.0;
    float3 perturbedN = normalize(
        normalTex.x * NormalScale * input.Tangent   +
        normalTex.y * NormalScale * input.Bitangent +
        normalTex.z * worldN);
    worldN = normalize(lerp(worldN, perturbedN, HasNormalMap));

    // ── Detail Maps ─────────────────────────────────────────────────────────
    float3 detailAlbedoTex = tex2D(DetailAlbedoSampler, detailUV).rgb * 2.0;
    albedo.rgb *= lerp(float3(1, 1, 1), detailAlbedoTex, HasDetailAlbedoMap);

    float3 detailNormalTex = tex2D(DetailNormalSampler, detailUV).rgb * 2.0 - 1.0;
    float3 detailedN = normalize(worldN +
        detailNormalTex.x * DetailNormalScale * input.Tangent +
        detailNormalTex.y * DetailNormalScale * input.Bitangent);
    worldN = normalize(lerp(worldN, detailedN, HasDetailNormalMap));

    // ── Occlusion ───────────────────────────────────────────────────────────
    float occlusionTex = tex2D(OcclusionSampler, uv).r;
    float occlusion    = lerp(1.0, occlusionTex, HasOcclusionMap * OcclusionStrength);

    // ── Lighting (Blinn-Phong with metallic routing) ─────────────────────────
    float3 L   = normalize(-LightDirection);
    float3 V   = normalize(CameraPosition - input.WorldPos);
    float3 H   = normalize(L + V);
    float  NdL = max(dot(worldN, L), 0.0);
    float  NdH = max(dot(worldN, H), 0.0);
    float  NdV = max(dot(worldN, V), 0.0);

    float  shininess = 2.0 / (roughness * roughness) - 2.0;
    float3 F0        = lerp(float3(0.04, 0.04, 0.04), albedo.rgb, metallic);
    float3 F         = FresnelSchlick(NdV, F0);
    float3 kD        = (1.0 - F) * (1.0 - metallic);

    float3 diffuse  = kD * albedo.rgb * NdL * LightColor;
    float3 specular = F  * pow(NdH, max(shininess, 0.1)) * NdL * LightColor;
    float3 ambient  = AmbientColor * albedo.rgb * occlusion;

    // ── Emission ────────────────────────────────────────────────────────────
    float3 emissionTex = tex2D(EmissionSampler, uv).rgb;
    float3 emissionMap = lerp(float3(1, 1, 1), emissionTex, HasEmissionMap);
    float3 emission    = EmissionColor.rgb * emissionMap * EmissionIntensity;

    float3 color = ambient + diffuse + specular + emission;
    return float4(color, albedo.a);
}

// ─── Pixel Shaders ───────────────────────────────────────────────────────────

float4 PS_Opaque(PSInput input) : COLOR
{
    float4 c = ComputeShading(input);
    c.a = 1.0;
    return c;
}

float4 PS_Cutout(PSInput input) : COLOR
{
    float4 c = ComputeShading(input);
    if (c.a < AlphaCutoff) discard;
    c.a = 1.0;
    return c;
}

float4 PS_Transparent(PSInput input) : COLOR
{
    return ComputeShading(input);
}

// ─── Techniques ──────────────────────────────────────────────────────────────

technique Standard_Opaque
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL VS();
        PixelShader  = compile PS_SHADERMODEL PS_Opaque();
    }
}

technique Standard_Cutout
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL VS();
        PixelShader  = compile PS_SHADERMODEL PS_Cutout();
    }
}

technique Standard_Fade
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL VS();
        PixelShader  = compile PS_SHADERMODEL PS_Transparent();
    }
}

technique Standard_Transparent
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL VS();
        PixelShader  = compile PS_SHADERMODEL PS_Transparent();
    }
}
