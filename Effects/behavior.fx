float uTime;
float uAlpha;
float uDeltaTime;

float2 uResolution;
float2 uWorldSize;
float2 uScreenPosition;
float2 uSize;

float2 uDirection;
float2 uTarget;

float3 uInitPos;
float3 uInitVel;

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

float4 computeFrag(ComputeFragmentIn input) : COLOR0
{
    float2 p = tex2D(uImage1, float2(input.coords.x, 0.25));
    float2 v = tex2D(uImage1, float2(input.coords.x, 0.75)) - 0.5;
    // 暴力判断这个像素代表位置还是速度
    if (input.coords.y < 0.5)
    {
        // 更新位置
        return float4(p + v * uDeltaTime, 0, 1);
    }
    else
    {
        // 更新速度
        float2 t = - p * uWorldSize + uTarget;
        v = v * uWorldSize + uDeltaTime * t * (input.coords.x + 0.0001);
        v *= 0.999;
        return float4(v / uWorldSize + 0.5, 0, 1);
    }
}

float4 initFrag(ComputeFragmentIn input) : COLOR0
{
    if (input.coords.y < 0.5)
    {
        return float4(uInitPos, 1);
    }
    else
    {
        return float4(uInitVel, 1);
    }
}

technique Technique233
{
    pass Compute
    {
        VertexShader = compile vs_3_0 quadVertex(); 
        PixelShader  = compile ps_3_0 computeFrag(); 
    }

    pass Init
    {
        VertexShader = compile vs_3_0 quadVertex(); 
        PixelShader  = compile ps_3_0 initFrag(); 
    }
}

