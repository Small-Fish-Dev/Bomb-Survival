
HEADER
{
	Description = "";
}

FEATURES
{
	#include "common/features.hlsl"
}

MODES
{
	VrForward();
	Depth(); 
	ToolsVis( S_MODE_TOOLS_VIS );
}

COMMON
{
	#ifndef S_ALPHA_TEST
	#define S_ALPHA_TEST 0
	#endif
	#ifndef S_TRANSLUCENT
	#define S_TRANSLUCENT 0
	#endif
	
	#include "common/shared.hlsl"
	#include "procedural.hlsl"

	#define S_UV2 1
	#define CUSTOM_MATERIAL_INPUTS
}

struct VertexInput
{
	#include "common/vertexinput.hlsl"
};

struct PixelInput
{
	#include "common/pixelinput.hlsl"
	float3 vPositionOs : TEXCOORD14;
};

VS
{
	#include "common/vertex.hlsl"

	PixelInput MainVs( VertexInput v )
	{
		PixelInput i = ProcessVertex( v );
		i.vPositionOs = v.vPositionOs.xyz;

		return FinalizeVertex( i );
	}
}

PS
{
	#include "common/pixel.hlsl"
	
	SamplerState g_sSampler0 < Filter( ANISO ); AddressU( WRAP ); AddressV( WRAP ); >;
	SamplerState g_sSampler1 < Filter( ANISO ); AddressU( CLAMP ); AddressV( CLAMP ); >;
	CreateInputTexture2D( Colour, Srgb, 8, "None", "_color", "Textures,3/,0/0", Default4( 0.00, 0.00, 0.00, 0.00 ) );
	CreateInputTexture2D( BlendMask, Linear, 8, "None", "_mask", ",0/,0/0", Default4( 0.00, 0.00, 0.00, 1.00 ) );
	CreateInputTexture2D( ScorchColour, Srgb, 8, "None", "_color", "Scorch,10/,0/1", Default4( 0.00, 0.00, 0.00, 0.00 ) );
	CreateInputTexture2D( ScorchBlendMask, Linear, 8, "None", "_mask", "Scorch,10/,0/6", Default4( 0.00, 0.00, 0.00, 1.00 ) );
	CreateInputTexture2D( Normal, Linear, 8, "NormalizeNormals", "_normal", "Textures,3/,0/0", Default4( 0.00, 0.00, 0.00, 0.00 ) );
	CreateInputTexture2D( ScorchNormal, Linear, 8, "NormalizeNormals", "_normal", "Scorch,10/,0/3", Default4( 0.00, 0.00, 0.00, 0.00 ) );
	CreateInputTexture2D( Rough, Linear, 8, "None", "_rough", "Textures,3/,0/0", Default4( 0.00, 0.00, 0.00, 0.00 ) );
	CreateInputTexture2D( ScorchRough, Linear, 8, "None", "_rough", "Scorch,10/,0/4", Default4( 0.00, 0.00, 0.00, 0.00 ) );
	CreateInputTexture2D( AO, Linear, 8, "None", "_ao", "Textures,3/,0/0", Default4( 0.00, 0.00, 0.00, 0.00 ) );
	CreateInputTexture2D( ScorchAO, Linear, 8, "None", "_ao", "Scorch,10/,0/5", Default4( 0.00, 0.00, 0.00, 0.00 ) );
	Texture2D g_tColour < Channel( RGBA, Box( Colour ), Srgb ); OutputFormat( DXT5 ); SrgbRead( True ); >;
	Texture2D g_tBlendMask < Channel( RGBA, Box( BlendMask ), Linear ); OutputFormat( DXT5 ); SrgbRead( False ); >;
	Texture2D g_tScorchColour < Channel( RGBA, Box( ScorchColour ), Srgb ); OutputFormat( DXT5 ); SrgbRead( True ); >;
	Texture2D g_tScorchLayer < Attribute( "ScorchLayer" ); >;
	Texture2D g_tScorchBlendMask < Channel( RGBA, Box( ScorchBlendMask ), Linear ); OutputFormat( DXT5 ); SrgbRead( False ); >;
	Texture2D g_tNormal < Channel( RGBA, Box( Normal ), Linear ); OutputFormat( DXT5 ); SrgbRead( False ); >;
	Texture2D g_tScorchNormal < Channel( RGBA, Box( ScorchNormal ), Linear ); OutputFormat( DXT5 ); SrgbRead( False ); >;
	Texture2D g_tRough < Channel( RGBA, Box( Rough ), Linear ); OutputFormat( DXT5 ); SrgbRead( False ); >;
	Texture2D g_tScorchRough < Channel( RGBA, Box( ScorchRough ), Linear ); OutputFormat( DXT5 ); SrgbRead( False ); >;
	Texture2D g_tAO < Channel( RGBA, Box( AO ), Linear ); OutputFormat( DXT5 ); SrgbRead( False ); >;
	Texture2D g_tScorchAO < Channel( RGBA, Box( ScorchAO ), Linear ); OutputFormat( DXT5 ); SrgbRead( False ); >;
	float g_flTiling < UiGroup( "Textures,0/,1/0" ); Default1( 1 ); Range1( 0, 5 ); >;
	float4 g_vTint_Colour < UiType( Color ); UiGroup( "Tint,0/,0/0" ); Default4( 0.41, 0.22, 0.11, 1.00 ); >;
	float g_flYPosition < UiGroup( "Position,0/Y,0/3" ); Default1( 64 ); Range1( 0, 1024 ); >;
	float g_flYSmoothing < UiGroup( "Position,0/Y,0/4" ); Default1( 75 ); Range1( 0, 1024 ); >;
	float g_flZPosition < UiGroup( "Position,0/Z,1/1" ); Default1( 0 ); Range1( 0, 2048 ); >;
	float g_flZSmoothing < UiGroup( "Position,0/Z,1/2" ); Default1( 250 ); Range1( 0, 2048 ); >;
	bool g_bTintDirectionToggle < UiGroup( "Tint,1/,0/0" ); Default( 0 ); >;
	float4 g_vScorchTint_Colour < UiType( Color ); UiGroup( "Scorch,10/,0/2" ); Default4( 0.13, 0.13, 0.12, 1.00 ); >;
	float4 g_vScorchLayer_Params < Attribute( "ScorchLayer_Params" ); >;
	float g_flScorchBlendDistance < UiGroup( "Scorch,10/,0/10" ); Default1( 32 ); Range1( 0, 256 ); >;
		
	float SoftLight_blend( float a, float b )
	{
	    if ( b <= 0.5f )
	        return 2.0f * a * b + a * a * ( 1.0f * 2.0f * b );
	    else 
	        return sqrt( a ) * ( 2.0f * b - 1.0f ) + 2.0f * a * (1.0f - b);
	}
	
	float3 SoftLight_blend( float3 a, float3 b )
	{
	    return float3(
	        SoftLight_blend( a.r, b.r ),
	        SoftLight_blend( a.g, b.g ),
	        SoftLight_blend( a.b, b.b )
		);
	}
	
	float4 SoftLight_blend( float4 a, float4 b, bool blendAlpha = false )
	{
	    return float4(
	        SoftLight_blend( a.rgb, b.rgb ).rgb,
	        blendAlpha ? SoftLight_blend( a.a, b.a ) : max( a.a, b.a )
	    );
	}
	
	float Overlay_blend( float a, float b )
	{
	    if ( a <= 0.5f )
	        return 2.0f * a * b;
	    else
	        return 1.0f - 2.0f * ( 1.0f - a ) * ( 1.0f - b );
	}
	
	float3 Overlay_blend( float3 a, float3 b )
	{
	    return float3(
	        Overlay_blend( a.r, b.r ),
	        Overlay_blend( a.g, b.g ),
	        Overlay_blend( a.b, b.b )
		);
	}
	
	float4 Overlay_blend( float4 a, float4 b, bool blendAlpha = false )
	{
	    return float4(
	        Overlay_blend( a.rgb, b.rgb ).rgb,
	        blendAlpha ? Overlay_blend( a.a, b.a ) : max( a.a, b.a )
	    );
	}
	
	float4 MainPs( PixelInput i ) : SV_Target0
	{
		Material m;
		m.Albedo = float3( 1, 1, 1 );
		m.Normal = TransformNormal( i, float3( 0, 0, 1 ) );
		m.Roughness = 1;
		m.Metalness = 0;
		m.AmbientOcclusion = 1;
		m.TintMask = 1;
		m.Opacity = 1;
		m.Emission = float3( 0, 0, 0 );
		m.Transmission = 0;
		
		float2 l_0 = i.vTextureCoords.xy * float2( 1, 1 );
		float l_1 = g_flTiling;
		float2 l_2 = l_0 * float2( l_1, l_1 );
		float4 l_3 = Tex2DS( g_tColour, g_sSampler0, l_2 );
		float4 l_4 = g_vTint_Colour;
		float4 l_5 = Tex2DS( g_tBlendMask, g_sSampler0, l_2 );
		float4 l_6 = saturate( lerp( l_4, SoftLight_blend( l_4, l_3 ), l_5.a ) );
		float3 l_7 = i.vPositionWithOffsetWs.xyz + g_vHighPrecisionLightingOffsetWs.xyz;
		float l_8 = l_7.y;
		float l_9 = g_flYPosition;
		float l_10 = l_8 + l_9;
		float l_11 = g_flYSmoothing;
		float l_12 = l_10 / l_11;
		float l_13 = saturate( l_12 );
		float4 l_14 = lerp( l_3, l_6, l_13 );
		float l_15 = l_7.z;
		float l_16 = g_flZPosition;
		float l_17 = l_15 + l_16;
		float l_18 = g_flZSmoothing;
		float l_19 = l_17 / l_18;
		float l_20 = saturate( l_19 );
		float l_21 = l_20 * l_4.a;
		float l_22 = 1 - l_21;
		float l_23 = g_bTintDirectionToggle ? l_21 : l_22;
		float4 l_24 = saturate( lerp( l_3, Overlay_blend( l_3, l_6 ), l_23 ) );
		float4 l_25 = l_14 * l_24;
		float4 l_26 = g_vScorchTint_Colour;
		float4 l_27 = Tex2DS( g_tScorchColour, g_sSampler0, l_2 );
		float4 l_28 = l_26 * l_27;
		float4 l_29 = g_vScorchLayer_Params;
		float4 l_30 = float4( l_29.r, l_29.g, 0, 0 );
		float3 l_31 = i.vPositionOs;
		float3 l_32 = float3( l_29.b, l_29.b, l_29.b ) * l_31;
		float4 l_33 = l_30 + float4( l_32, 0 );
		float4 l_34 = Tex2DS( g_tScorchLayer, g_sSampler1, l_33.xy );
		float l_35 = l_34.r - 0.5;
		float l_36 = l_35 * l_29.a;
		float l_37 = g_flScorchBlendDistance;
		float l_38 = l_29.a / l_37;
		float l_39 = l_36 * l_38;
		float l_40 = l_39 * -0.5;
		float4 l_41 = Tex2DS( g_tScorchBlendMask, g_sSampler0, l_2 );
		float l_42 = l_41.r - 0.5;
		float l_43 = l_42 * 32;
		float l_44 = l_40 - l_43;
		float l_45 = max( l_44, 0 );
		float l_46 = min( l_45, 1 );
		float4 l_47 = lerp( l_25, l_28, l_46 );
		float4 l_48 = Tex2DS( g_tNormal, g_sSampler0, l_2 );
		float4 l_49 = Tex2DS( g_tScorchNormal, g_sSampler0, l_2 );
		float4 l_50 = lerp( l_48, l_49, l_46 );
		float3 l_51 = TransformNormal( i, DecodeNormal( l_50.xyz ) );
		float4 l_52 = Tex2DS( g_tRough, g_sSampler0, l_2 );
		float4 l_53 = Tex2DS( g_tScorchRough, g_sSampler0, l_2 );
		float l_54 = lerp( l_52.a, l_53.r, l_46 );
		float4 l_55 = Tex2DS( g_tAO, g_sSampler0, l_2 );
		float4 l_56 = Tex2DS( g_tScorchAO, g_sSampler0, l_2 );
		float l_57 = lerp( l_55.r, l_56.r, l_46 );
		
		m.Albedo = l_47.xyz;
		m.Opacity = 1;
		m.Normal = l_51;
		m.Roughness = l_54;
		m.Metalness = 0;
		m.AmbientOcclusion = l_57;
		
		m.AmbientOcclusion = saturate( m.AmbientOcclusion );
		m.Roughness = saturate( m.Roughness );
		m.Metalness = saturate( m.Metalness );
		m.Opacity = saturate( m.Opacity );
		
		return ShadingModelStandard::Shade( i, m );
	}
}
