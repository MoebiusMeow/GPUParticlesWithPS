float uTime;
float uAlpha;
float2 uResolution;
float uDirectionFactor;
float2 uDirection;

float4x4 uMVP;

texture2D uTex0;
sampler2D uImage0 = sampler_state
{
    Texture = <uTex0>;
    MinFilter = Linear;
    MagFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};

texture2D uTex1;
sampler2D uImage1 = sampler_state
{
    Texture = <uTex1>;
    MinFilter = Point;
    MagFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

// Particles

struct VertexIn
{
    float3 pos : POSITION0;
    float2 coords : TEXCOORD0;
    float id : POSITION1;
};

struct FragmentIn
{
    float4 pos : POSITION0;
    float2 coords : TEXCOORD0;
};

FragmentIn vertex(VertexIn input)
{
    FragmentIn output;
    float4 p = tex2Dlod(uImage1, float4(input.id, 0.5, 0.0, 0.0));
    output.pos = float4(input.pos.xy * 16 / uResolution + (p.xy - float2(0.5, 0.5)), 0.0, 1.0);
    output.coords = input.coords; 
    return output;
}

float4 frag(FragmentIn input) : COLOR0
{
    return tex2D(uImage0, input.coords);
}

// Compute

struct QuadVertexIn
{
    float3 pos : POSITION0;
    float2 coords : TEXCOORD0;
};

struct ComputeFragmentIn
{
    float4 pos : POSITION0;
    float2 coords : TEXCOORD0;
};

ComputeFragmentIn quadVertex(QuadVertexIn input)
{
    ComputeFragmentIn output;
    output.pos = float4(input.pos, 1.0);
    output.coords = input.coords; 
    return output;
}

float4 copyFrag(ComputeFragmentIn input) : COLOR0
{
    float4 ret = tex2D(uImage1, input.coords);
    return ret;
}

float4 computeFrag(ComputeFragmentIn input) : COLOR0
{
    float4 ret = tex2D(uImage1, input.coords);
    ret.x += uDirection.x * lerp(input.coords.x, 1, 0.001);
    ret.y += (ret.z - 0.5) * lerp(input.coords.x, 1, 0.001);
    ret.z += uDirection.y;
    ret.w = 1; 
    return ret;
}

technique Technique233
{
    pass Particles
    {
	    VertexShader = compile vs_3_0 vertex(); 
        PixelShader  = compile ps_3_0 frag(); 
    }

    pass Compute
    {
	    VertexShader = compile vs_3_0 quadVertex(); 
        PixelShader  = compile ps_3_0 computeFrag(); 
    }

    pass Copy
    {
	    VertexShader = compile vs_3_0 quadVertex(); 
        PixelShader  = compile ps_3_0 copyFrag(); 
    }
}

