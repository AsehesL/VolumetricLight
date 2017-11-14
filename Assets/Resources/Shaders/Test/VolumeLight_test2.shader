Shader "Unlit/VolumeLight_test2"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_DepthTex("DepthTex", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType"="Transparent" "Queue"="Transparent" }
		LOD 100

		Pass
		{
			zwrite off
			blend srcalpha one
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				UNITY_FOG_COORDS(0)
				float3 camPos : TEXCOORD1;
				float3 camViewDir : TEXCOORD2;
				float4 vertex : SV_POSITION;
			};

			sampler2D_float _DepthTex;

			float4x4 internalProjection;
			half4 lightZParams;

			
			v2f vert (appdata v)
			{
				v2f o;
				//float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
				o.vertex = UnityObjectToClipPos(v.vertex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				o.camPos = v.vertex.xyz;
				o.camPos.z *= -1;

				float3 mainCamPos = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos.xyz, 1)).xyz;
				mainCamPos.z *= -1;

				o.camViewDir = normalize(o.camPos.xyz - mainCamPos); 

				//o.depth = -worldPos.z / lightZParams.x;
				//worldPos = mul(internalProjection, worldPos);
				//o.proj = ComputeScreenPos(worldPos);
				return o;
			}
			
			float4 frag (v2f i) : SV_Target
			{
				float step = 64;
				float delta = lightZParams.y / step*2;

				float4 col = float4(1,1,1,0);

				for(int k=0;k<step;k++){
					float3 cpos = i.camPos.xyz + i.camViewDir*k*delta;
					float4 pjpos = mul(internalProjection, float4(cpos, 1));
					pjpos = ComputeScreenPos(pjpos);
					half2 pjuv = pjpos.xy/pjpos.w;  
					#if UNITY_UV_STARTS_AT_TOP
					pjuv.y = 1 - pjuv.y;
					#endif
					float boardFac = 0;
					if(pjuv.x>0&&pjuv.x<1&&pjuv.y>0&&pjuv.y<1)
						boardFac = 1;
					
					half dep = DecodeFloatRGBA(tex2D(_DepthTex, pjuv));
					half cdep = -cpos.z / lightZParams.x;

					if(cdep < dep)
						col.a += 0.01*boardFac;
						//col.a = 1;
				}

				//half2 pjuv = i.proj.xy/i.proj.w;  
				//#if UNITY_UV_STARTS_AT_TOP
				//pjuv.y = 1 - pjuv.y;
				//#endif
				//half dep = tex2D(_DepthTex, pjuv).r; 

				//float4 col = float4(1,1,1,1);
				//if (i.depth > dep) 
				//	col.a = 0;
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}
}
