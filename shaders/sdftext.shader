HEADER
{
	Description = "SDF Text";
}

MODES
{
	Default();
    VrForward();
}

FEATURES
{
	#include "ui/features.hlsl"
}

COMMON
{
	#include "ui/common.hlsl"
}
  

VS
{
	#include "ui/vertex.hlsl"  
}

PS
{
    #include "ui/pixel.hlsl"

    RenderState( SrgbWriteEnable0, true );
	RenderState( ColorWriteEnable0, RGBA );
	RenderState( FillMode, SOLID );
	RenderState( CullMode, NONE );

	// No depth
	RenderState( DepthEnable, false );
	RenderState( DepthWriteEnable, false );

    CreateTexture2D( g_tFont ) < Attribute( "Font" ); Filter( MIN_MAG_LINEAR_MIP_POINT ); OutputFormat( BC7 ); SrgbRead( false ); AddressU( CLAMP ); AddressV( CLAMP ); >;
    float4 color < Attribute( "Color" ); Default4( 1, 1, 1, 1 ); >;

	PS_OUTPUT MainPs( PS_INPUT i )
	{
        const float threshold = 0.45f;
        const float smoothing = 0.1f;

        PS_OUTPUT o;
        UI_CommonProcessing_Pre( i );

        float distance = Tex2D( g_tFont, i.vTexCoord.xy ).r;

		float4 vImage = Tex2D( g_tFont, i.vTexCoord.xy );
		o.vColor.rgb = color.rgb;
        o.vColor.a = smoothstep( threshold - smoothing, threshold + smoothing, distance ) * color.a;

		return UI_CommonProcessing_Post( i, o );
	}
}