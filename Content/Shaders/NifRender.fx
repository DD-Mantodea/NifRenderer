//------------------------------------------------------
//--                                                  --
//--		   www.riemers.net                    --
//--   		    Basic shaders                     --
//--		Use/modify as you like                --
//--                                                  --
//------------------------------------------------------

struct VertexToPixel
{
    float4 Position   	: POSITION;    
    float4 Color		: COLOR0;
    float LightingFactor: TEXCOORD0;
    float2 TextureCoords: TEXCOORD1;
};

struct BackgroundToPixel
{
    float4 Position   	: POSITION;
    float4 Color		: COLOR0;
    float2 NdcPosition  : TEXCOORD0;
};

struct AxisToPixel
{
    float4 Position   	: POSITION;
    float4 Color		: COLOR0;
};

struct PixelToFrame
{
    float4 Color : COLOR0;
};

//------- Constants --------
float4x4 xView;
float4x4 xProjection;
float4x4 xWorld;
float xAlphaTest;

float3 xLightDirection;
float xAmbient;
bool xEnableLighting;

//------- Outline constants --------
float2 xOutlineTexel;
float xOutlineWidth;
float4 xOutlineColor;

float4 xRoundSize;
float4 xRoundParams;

float2 xPointSize;
float2 xPointScale;


//------- Texture Samplers --------

Texture xTexture;
sampler TextureSampler = sampler_state { texture = <xTexture>; magfilter = LINEAR; minfilter = LINEAR; mipfilter=LINEAR; AddressU = mirror; AddressV = mirror;};

sampler OutlineSampler = sampler_state { texture = <xTexture>; magfilter = POINT; minfilter = POINT; mipfilter = NONE; AddressU = clamp; AddressV = clamp;};

//------- Technique: Pretransformed --------

float RoundedDistance(float2 p, float2 halfSize, float radius)
{
	float2 d = abs(p) - (halfSize - radius);

	float2 outside = max(d, 0.0f);

	return length(outside) + min(max(d.x, d.y), 0.0f) - radius;
}

BackgroundToPixel PretransformedVS( float4 inPos : POSITION, float4 inColor: COLOR)
{	
	BackgroundToPixel Output = (BackgroundToPixel)0;
	
	Output.Position = inPos;
	Output.Color = inColor;
	Output.NdcPosition = inPos.xy;
    
	return Output;    
}

PixelToFrame PretransformedPS(BackgroundToPixel PSIn) 
{
	PixelToFrame Output = (PixelToFrame)0;

	if (xRoundParams.x > 0.0f)
	{
		float2 nc = PSIn.NdcPosition;

		float2 vs;

		vs.x = (nc.x + 1.0f) * 0.5f * xRoundSize.x;

		vs.y = (1.0f - nc.y) * 0.5f * xRoundSize.y;

		clip(-RoundedDistance(vs - (xRoundSize.xy * 0.5f), xRoundSize.xy * 0.5f, xRoundParams.x));
	}

	Output.Color = PSIn.Color;

	return Output;
}

technique Pretransformed
{
	pass Pass0
	{   
#if SM4
		VertexShader = compile vs_4_0_level_9_3 PretransformedVS();
		PixelShader  = compile ps_4_0_level_9_3 PretransformedPS();
#else
		VertexShader = compile vs_1_1 PretransformedVS();
		PixelShader  = compile ps_2_0 PretransformedPS();
#endif				
	}
}

//------- Technique: Colored --------

VertexToPixel ColoredVS( float4 inPos : POSITION, float3 inNormal: NORMAL, float4 inColor: COLOR)
{	
	VertexToPixel Output = (VertexToPixel)0;
	float4x4 preViewProjection = mul (xView, xProjection);
	float4x4 preWorldViewProjection = mul (xWorld, preViewProjection);
    
	Output.Position = mul(inPos, preWorldViewProjection);
	Output.Color = inColor;
	
	float3 Normal = normalize(mul(normalize(inNormal), (float3x3)xWorld));	
	Output.LightingFactor = 1;
	if (xEnableLighting)
		Output.LightingFactor = dot(Normal, -xLightDirection);
    
	return Output;    
}

PixelToFrame ColoredPS(VertexToPixel PSIn) 
{
	PixelToFrame Output = (PixelToFrame)0;		
    
	Output.Color = PSIn.Color;
	Output.Color.rgb *= saturate(PSIn.LightingFactor) + xAmbient;

	return Output;
}

technique Colored
{
	pass Pass0
	{   
#if SM4
		VertexShader = compile vs_4_0_level_9_3 ColoredVS();
		PixelShader  = compile ps_4_0_level_9_3 ColoredPS();
#else
		VertexShader = compile vs_1_1 ColoredVS();
		PixelShader  = compile ps_2_0 ColoredPS();
#endif				
	}
}

//------- Technique: ColoredNoShading --------

VertexToPixel ColoredNoShadingVS( float4 inPos : POSITION, float4 inColor: COLOR)
{	
	VertexToPixel Output = (VertexToPixel)0;
	float4x4 preViewProjection = mul (xView, xProjection);
	float4x4 preWorldViewProjection = mul (xWorld, preViewProjection);
    
	Output.Position = mul(inPos, preWorldViewProjection);
	Output.Color = inColor;
	Output.LightingFactor = 1.0f;
	Output.TextureCoords = float2(0.0f, 0.0f);
    
	return Output;    
}

//------- Technique: Axis --------

AxisToPixel AxisVS( float4 inPos : POSITION, float4 inColor: COLOR)
{	
	AxisToPixel Output = (AxisToPixel)0;

	Output.Position = float4(inPos.x, inPos.y, inPos.z, 1.0f);
	Output.Color = inColor;

	return Output;    
}

PixelToFrame AxisPS(AxisToPixel PSIn)
{
	PixelToFrame Output = (PixelToFrame)0;

	Output.Color = PSIn.Color;

	return Output;
}

technique Axis
{
	pass Pass0
	{
#if SM4
		VertexShader = compile vs_4_0_level_9_3 AxisVS();
		PixelShader  = compile ps_4_0_level_9_3 AxisPS();
#else
		VertexShader = compile vs_1_1 AxisVS();
		PixelShader  = compile ps_2_0 AxisPS();
#endif
	}
}

PixelToFrame ColoredNoShadingPS(VertexToPixel PSIn) 
{
	PixelToFrame Output = (PixelToFrame)0;		

	Output.Color = PSIn.Color;

	return Output;
}

technique ColoredNoShading
{
	pass Pass0
	{   
#if SM4
		VertexShader = compile vs_4_0_level_9_3 ColoredNoShadingVS();
		PixelShader  = compile ps_4_0_level_9_3 ColoredNoShadingPS();
#else
		VertexShader = compile vs_1_1 ColoredNoShadingVS();
		PixelShader  = compile ps_2_0 ColoredNoShadingPS();
#endif				
	}
}

//------- Technique: EditLine --------

float3 SrgbToLinear(float3 c)
{
	return (c <= 0.04045) ? c / 12.92 : pow((c + 0.055) / 1.055, 2.4);
}

float3 LinearToSrgb(float3 c)
{
	return (c <= 0.0031308) ? c * 12.92 : 1.055 * pow(c, 1.0 / 2.4) - 0.055;
}

VertexToPixel EditLineVS( float4 inPos : POSITION, float4 inColor: COLOR)
{
	VertexToPixel Output = (VertexToPixel)0;
	float4x4 preViewProjection = mul (xView, xProjection);
	float4x4 preWorldViewProjection = mul (xWorld, preViewProjection);

	Output.Position = mul(inPos, preWorldViewProjection);
	Output.Position.z -= 0.00002f * Output.Position.w;
	Output.Color = float4(SrgbToLinear(inColor.rgb), inColor.a);
	Output.LightingFactor = 1.0f;
	Output.TextureCoords = float2(0.0f, 0.0f);

	return Output;
}

PixelToFrame EditLinePS(VertexToPixel PSIn)
{
	PixelToFrame Output = (PixelToFrame)0;

	Output.Color = float4(LinearToSrgb(PSIn.Color.rgb), PSIn.Color.a);

	return Output;
}

technique EditLine
{
	pass Pass0
	{
#if SM4
		VertexShader = compile vs_4_0_level_9_3 EditLineVS();
		PixelShader  = compile ps_4_0_level_9_3 EditLinePS();
#else
		VertexShader = compile vs_1_1 EditLineVS();
		PixelShader  = compile ps_2_0 EditLinePS();
#endif
	}
}

//------- Technique: RoundPoint --------

struct PointToPixel
{
	float4 Position : POSITION;
	float4 Color : COLOR0;
	float2 TexCoord : TEXCOORD0;
};

PointToPixel RoundPointVS( float4 inPos : POSITION, float4 inColor : COLOR0, float2 inTexCoord : TEXCOORD0)
{
	PointToPixel Output = (PointToPixel)0;

	float4x4 preViewProjection = mul (xView, xProjection);
	float4x4 preWorldViewProjection = mul (xWorld, preViewProjection);

	float4 clip = mul(inPos, preWorldViewProjection);

	float2 corner = (inTexCoord - 0.5f) * xPointSize;

	clip.xy += corner * xPointScale * clip.w;

	clip.z -= 0.00002f * clip.w;

	Output.Position = clip;
	Output.Color = inColor;
	Output.TexCoord = inTexCoord;

	return Output;
}

float4 RoundPointPS(PointToPixel PSIn) : COLOR0
{
	float2 centered = PSIn.TexCoord - float2(0.5, 0.5);

	clip(0.25 - dot(centered, centered));

	return PSIn.Color;
}

technique RoundPoint
{
	pass Pass0
	{
#if SM4
		VertexShader = compile vs_4_0_level_9_3 RoundPointVS();
		PixelShader  = compile ps_4_0_level_9_3 RoundPointPS();
#else
		VertexShader = compile vs_1_1 RoundPointVS();
		PixelShader  = compile ps_2_0 RoundPointPS();
#endif
	}
}

//------- Technique: Textured --------

VertexToPixel TexturedVS( float4 inPos : POSITION, float3 inNormal: NORMAL, float2 inTexCoords: TEXCOORD0)
{	
	VertexToPixel Output = (VertexToPixel)0;
	float4x4 preViewProjection = mul (xView, xProjection);
	float4x4 preWorldViewProjection = mul (xWorld, preViewProjection);
    
	Output.Position = mul(inPos, preWorldViewProjection);	
	Output.TextureCoords = inTexCoords;
	
	float3 Normal = normalize(mul(normalize(inNormal), (float3x3)xWorld));	
	Output.LightingFactor = 1;
	if (xEnableLighting)
		Output.LightingFactor = dot(Normal, -xLightDirection);
    
	return Output;    
}

PixelToFrame TexturedPS(VertexToPixel PSIn) 
{
	PixelToFrame Output = (PixelToFrame)0;		
	
	float4 sampled = tex2D(TextureSampler, PSIn.TextureCoords);

	if (xAlphaTest > 0.0f)
		clip(sampled.a - xAlphaTest);

	Output.Color = sampled;
	Output.Color.rgb *= saturate(PSIn.LightingFactor) + xAmbient;

	return Output;
}

technique Textured
{
	pass Pass0
	{   
#if SM4
		VertexShader = compile vs_4_0_level_9_3 TexturedVS();
		PixelShader  = compile ps_4_0_level_9_3 TexturedPS();
#else
		VertexShader = compile vs_1_1 TexturedVS();
		PixelShader  = compile ps_2_0 TexturedPS();
#endif				
	}
}

//------- Technique: TexturedNoShading --------
VertexToPixel TexturedNoShadingVS( float4 inPos : POSITION, float2 inTexCoords: TEXCOORD0)
{	
	VertexToPixel Output = (VertexToPixel)0;
	float4x4 preViewProjection = mul (xView, xProjection);
	float4x4 preWorldViewProjection = mul (xWorld, preViewProjection);
    
	Output.Position = mul(inPos, preWorldViewProjection);	
	Output.TextureCoords = inTexCoords;
    
	return Output;    
}

PixelToFrame TexturedNoShadingPS(VertexToPixel PSIn) 
{
	PixelToFrame Output = (PixelToFrame)0;		
	
	Output.Color = tex2D(TextureSampler, PSIn.TextureCoords);

	return Output;
}

technique TexturedNoShading
{
	pass Pass0
	{   
#if SM4
		VertexShader = compile vs_4_0_level_9_3 TexturedNoShadingVS();
		PixelShader  = compile ps_4_0_level_9_3 TexturedNoShadingPS();
#else
		VertexShader = compile vs_1_1 TexturedNoShadingVS();
		PixelShader  = compile ps_2_0 TexturedNoShadingPS();
#endif		
	}
}

//------- Technique: OutlineID --------

VertexToPixel OutlineIDVS( float4 inPos : POSITION)
{	
	VertexToPixel Output = (VertexToPixel)0;
	float4x4 preViewProjection = mul (xView, xProjection);
	float4x4 preWorldViewProjection = mul (xWorld, preViewProjection);

	Output.Position = mul(inPos, preWorldViewProjection);

	Output.Color = 1;

	return Output;    
}

PixelToFrame OutlineIDPS(VertexToPixel PSIn) 
{
	PixelToFrame Output = (PixelToFrame)0;		
    
	Output.Color = 1;

	return Output;
}

technique OutlineID
{
	pass Pass0
	{   
#if SM4
		VertexShader = compile vs_4_0_level_9_3 OutlineIDVS();
		PixelShader  = compile ps_4_0_level_9_3 OutlineIDPS();
#else
		VertexShader = compile vs_1_1 OutlineIDVS();
		PixelShader  = compile ps_2_0 OutlineIDPS();
#endif				
	}
}

//------- Technique: OutlineDetect --------

struct OutlineQuadToPixel
{
	float4 Position : POSITION;
	float2 TexCoord : TEXCOORD0;
};

OutlineQuadToPixel OutlineDetectVS( float4 inPos : POSITION, float2 inTexCoord : TEXCOORD0)
{
	OutlineQuadToPixel Output = (OutlineQuadToPixel)0;

	Output.Position = float4(inPos.x, inPos.y, 0.5, 1);
	Output.TexCoord = inTexCoord;

	return Output;
}

float4 OutlineDetectPS(OutlineQuadToPixel PSIn) : COLOR0
{
	float self = tex2D(OutlineSampler, PSIn.TexCoord).a;

	float covered = 0;

	float radius = xOutlineWidth;

	[unroll(9)]
	for (int y = -4; y <= 4; y++)
	{
		[unroll(9)]
		for (int x = -4; x <= 4; x++)
		{
			if (x * x + y * y > 16)
				continue;

			if (abs(x) > radius || abs(y) > radius)
				continue;

			float sampleValue = tex2D(OutlineSampler, PSIn.TexCoord + float2(x, y) * xOutlineTexel).a;

			covered = max(covered, sampleValue);
		}
	}

	float edge = saturate(covered - self);

	return float4(xOutlineColor.rgb, xOutlineColor.a * edge);
}

technique OutlineDetect
{
	pass Pass0
	{   
#if SM4
		VertexShader = compile vs_4_0_level_9_3 OutlineDetectVS();
		PixelShader  = compile ps_4_0_level_9_3 OutlineDetectPS();
#else
		VertexShader = compile vs_1_1 OutlineDetectVS();
		PixelShader  = compile ps_3_0 OutlineDetectPS();
#endif				
	}
}



