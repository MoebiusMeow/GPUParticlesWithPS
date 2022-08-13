float uTime;
float uAlpha;

float2 uResolution;
float2 uWorldSize;
float2 uScreenPosition;
float2 uScale;

float2 uDirection;
float3 uColor;

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
	float2 offset = tex2Dlod(uImage1, float4(input.id, 0.0, 0.0, 0.0)).xy * uWorldSize - uScreenPosition;
	output.pos = float4((input.pos.xy * uScale + offset * float2(2, -2)) / uResolution + float2(-1, 1), 0.0, 1.0);
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

float4 fillFrag(ComputeFragmentIn input) : COLOR0
{
	return float4(uColor, 1);
}

technique Technique233
{
	pass Particles
	{
		VertexShader = compile vs_3_0 vertex(); 
		PixelShader  = compile ps_3_0 frag(); 
	}

	pass Copy
	{
		VertexShader = compile vs_3_0 quadVertex(); 
		PixelShader  = compile ps_3_0 copyFrag(); 
	}

	pass Fill
	{
		VertexShader = compile vs_3_0 quadVertex(); 
		PixelShader  = compile ps_3_0 fillFrag(); 
	}
}

