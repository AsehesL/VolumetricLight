Shader "Hidden/ShadowMapRenderer"
{
	Properties
	{
	}
	SubShader
	{
		Tags{ "RenderType" = "Opaque" }
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float depth : TEXCOORD0;
			};
				
			v2f vert(appdata_base v)
			{
				v2f o;
				UNITY_INITIALIZE_OUTPUT(v2f, o);
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.depth = COMPUTE_DEPTH_01;
				return o;
			}
				
			fixed4 frag(v2f i) : SV_Target
			{
				return EncodeFloatRGBA(i.depth);
			}
			ENDCG
		}
	}
}
