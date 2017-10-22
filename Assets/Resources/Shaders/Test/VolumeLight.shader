Shader "Hidden/VolumeLight"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_DepthTex("DepthTex", 2D) = "white" {}
		_Cookie("Cookie", 2D) = "white"{}
		_Color("Color", color) = (1,1,1,1)
		_LightParams("Params", vector) = (0,0,0,0)
	}
	SubShader
	{
		Tags{ "RenderType" = "Transparent" "Queue" = "Transparent" }
		LOD 100

		Pass
		{
			zwrite off
			blend srcalpha one
			colormask rgb
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
			sampler2D _Cookie;

			float4x4 internalProjection;
			float4x4 internalProjectionInv;

			half4 _Color;
			half4 _LightParams;

			half3 camPos;
			
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
				float4 col = 0;

				float4 beginPjPos = mul(internalProjection, float4(i.viewPos, 1));
				beginPjPos /= beginPjPos.w;
				float4 pjCamPos = mul(internalProjection, float4(i.viewCamPos, 1));
				pjCamPos /= pjCamPos.w;

				float3 pjViewDir = normalize(beginPjPos.xyz - pjCamPos.xyz);
				if (i.viewCamPos.z > 0)
					pjViewDir = -pjViewDir;

				for (float k = 0; k< RAY_STEP; k++) {
					float4 curpos = beginPjPos;
					float3 vdir = pjViewDir.xyz*k*delta;
					curpos.xyz += vdir;
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
					/*float boardFac = 0;
					if (pjuv.x>=0 && pjuv.x<=1 && pjuv.y>=0 && pjuv.y<=1)
						boardFac = 1;*/

					half dep = DecodeFloatRGBA(tex2D(_DepthTex, pjuv));
					half4 cookie = tex2D(_Cookie, pjuv);
					float2 atten = saturate((0.5 - abs(pjuv - 0.5)) / (1 - _Color.a));
					
					half cdep = -curvpos.z / _LightParams.x;
					float range = 1 - saturate((cdep - _LightParams.y) / (1 - _LightParams.y));

					if (cdep < dep) {
						col.rgb += cookie.rgb*cookie.a*_Color.rgb*delta / 2 * boardFac*atten.x*atten.y*range;
					}
				}

				col.a = 1;
				
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}
}
