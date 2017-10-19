Shader "Unlit/VolumeLight_test3"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_DepthTex("DepthTex", 2D) = "white" {}
	}
	SubShader
	{
		Tags{ "RenderType" = "Transparent" "Queue" = "Transparent" }
		LOD 100

		Pass
		{
			zwrite off
			blend srcalpha one
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			#define RAY_STEP 64

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				UNITY_FOG_COORDS(0)
				float4 vertex : SV_POSITION;
				float3 viewPos : TEXCOORD1;
				float3 viewCamPos : TEXCOORD2;
			};

			sampler2D _DepthTex;

			float4x4 internalProjection;
			float4x4 internalProjectionInv;
			half4 lightZParams;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				UNITY_TRANSFER_FOG(o,o.vertex);

				o.viewPos = v.vertex.xyz;
				o.viewPos.z *= -1;

				o.viewCamPos = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos.xyz, 1)).xyz;
				o.viewCamPos.z *= -1;

				return o;
			}
			
			float4 frag (v2f i) : SV_Target
			{

				float delta = 2.0 / RAY_STEP;
				float4 col = float4(1, 1, 1, 0);

				float4 beginPjPos = mul(internalProjection, float4(i.viewPos, 1));
				beginPjPos /= beginPjPos.w;
				float4 pjCamPos = mul(internalProjection, float4(i.viewCamPos, 1));
				pjCamPos /= pjCamPos.w;

				float3 pjViewDir = normalize(beginPjPos.xyz - pjCamPos.xyz);

				for (float k = 0; k< RAY_STEP; k++) {
					float4 curpos = beginPjPos;
					curpos.xyz += pjViewDir.xyz*k*delta;
					float4 curvpos = mul(internalProjectionInv, curpos);
					curvpos /= curvpos.w;
					float boardFac = 1;
					if (curpos.x >= -1 && curpos.x <= 1 && curpos.y >= -1 && curpos.y <= 1 && curpos.z >= -1 && curpos.z <= 1)
						boardFac = 1;
					curpos = ComputeScreenPos(curpos);
					half2 pjuv = curpos.xy / curpos.w;
#if UNITY_UV_STARTS_AT_TOP
					pjuv.y = 1 - pjuv.y;
#endif

					half dep = DecodeFloatRGBA(tex2D(_DepthTex, pjuv));
					half cdep = -curvpos.z / lightZParams.x;

					if (cdep < dep)
						col.a += 0.01 *boardFac;
				}
				
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}
}
