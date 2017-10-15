Shader "Unlit/VolumeLight_test"
{
	Properties
	{
		_DepthTex ("DepthTexture", 2D) = "black" {}
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
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				UNITY_FOG_COORDS(0)
				float4 worldPos : TEXCOORD1;
				float3 viewDir : TEXCOORD2;
				float4 vertex : SV_POSITION;
			};

			sampler2D _DepthTex;

			float4x4 internalWorldToCamera;
			float4x4 internalProjection;
			half4 lightZParams;

			float LinearVolumeLightEyeDepth(float z)
			{
				return 1.0 / (lightZParams.z * z + lightZParams.w);
			}

			
			v2f vert (appdata v)
			{
				v2f o;
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				o.viewDir = UnityWorldSpaceViewDir(o.worldPos.xyz);
				o.vertex = UnityObjectToClipPos(v.vertex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			float4 frag (v2f i) : SV_Target
			{
				float step = 64;
				//float delta = step/volumeParams.x;

				//float4 col = float4(1,1,1,0);

				//for(int i=0;i<step;i++){
				//	float3 wp = i.worldPos.xyz+i.viewDir*i*delta;
				//	float4 pj = mul(projMatrix, float4(wp,1)).xyz
				//	pj = ComputeGrabScreenPos(pj);

				//	float2 pjuv = pj.xy/pj.w;
				//	if(pjuv.x<0||pjuv.x>1||pjuv.y<0||pjuv.y>1){
				//		break;
				//	}

				//}
				i.worldPos = mul(internalWorldToCamera, i.worldPos);
				float4 pjPos = mul(internalProjection, i.worldPos);
				pjPos = ComputeScreenPos(pjPos);
				float2 pjUv = pjPos.xy/pjPos.w;
				#if UNITY_UV_STARTS_AT_TOP
				pjUv.y = 1 - pjUv.y;
				#endif
				float depth = LinearVolumeLightEyeDepth(tex2D(_DepthTex, pjUv).r);
				float cDep = -i.worldPos.z/i.worldPos.w*lightZParams.w;

				//float4 col = float4(cDep,cDep,cDep,1);
				float4 col = float4(1,1,1,1);
				if(cDep < depth)
					col.rgb = 0;

				//fixed4 col = tex2D(_MainTex, i.uv);
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}
}
