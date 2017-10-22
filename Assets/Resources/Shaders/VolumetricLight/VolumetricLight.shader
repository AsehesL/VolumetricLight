Shader "Hidden/VolumetricLight"
{
	Properties
	{
	}
	SubShader
	{
		Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "IgnoreProjector"="true" }
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
			#pragma multi_compile __ USE_COOKIE
			#pragma multi_compile VOLUMETRIC_LIGHT_QUALITY_LOW VOLUMETRIC_LIGHT_QUALITY_MIDDLE VOLUMETRIC_LIGHT_QUALITY_HIGH
			
			#include "UnityCG.cginc"

			#if  VOLUMETRIC_LIGHT_QUALITY_LOW
				#define RAY_STEP 16
			#elif VOLUMETRIC_LIGHT_QUALITY_MIDDLE
				#define RAY_STEP 32
			#elif VOLUMETRIC_LIGHT_QUALITY_HIGH
				#define RAY_STEP 64
			#endif

			struct appdata
			{
				float4 vertex : POSITION;
				float3 color : COLOR;
			};

			struct v2f
			{
				UNITY_FOG_COORDS(0)
				float4 vertex : SV_POSITION;
				float3 viewPos : TEXCOORD1;
				float3 viewCamPos : TEXCOORD2;
				float3 vcol : COLOR;
			};

			uniform float4 internalWorldLightColor;
			uniform float4 internalWorldLightPos;

			sampler2D internalShadowMap;
#ifdef USE_COOKIE
			sampler2D internalCookie;
#endif
			float4x4 internalWorldLightVP;
			float4 internalProjectionParams;

			float LinearLightEyeDepth(float z)
			{
				float oz = (-z*(1 / internalProjectionParams.w - 0.01) + 1 / internalProjectionParams.w + 0.01) / 2;
				float pz = 1.0 / (internalProjectionParams.y * z + internalProjectionParams.z);
				return lerp(oz, pz, internalWorldLightPos.w);
			}
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				UNITY_TRANSFER_FOG(o,o.vertex);

				o.viewPos = v.vertex.xyz;
				o.viewPos.z *= -1;

				o.viewCamPos = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos.xyz, 1)).xyz;
				o.viewCamPos.z *= -1;
				o.vcol = v.color;

				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float delta = 2.0 / RAY_STEP;
				float4 col = 0;

				float4 beginPjPos = mul(internalWorldLightVP, float4(i.viewPos, 1));
				beginPjPos /= beginPjPos.w;
				float4 pjCamPos = mul(internalWorldLightVP, float4(i.viewCamPos, 1));
				pjCamPos /= pjCamPos.w;

				float3 pjViewDir = normalize(beginPjPos.xyz - pjCamPos.xyz);
				pjViewDir -= 2 * pjViewDir*step(0, i.viewCamPos.z)*internalWorldLightPos.w;

				for (float k = 0; k< RAY_STEP; k++) {
					float4 curpos = beginPjPos;
					float3 vdir = pjViewDir.xyz*k*delta;
					curpos.xyz += vdir;

					half cdep = LinearLightEyeDepth(-curpos.z);
					float boardFac = step(-1, curpos.x)*step(-1, curpos.y)*step(-1, curpos.z)*step(curpos.x, 1)*step(curpos.y, 1)*step(curpos.z, 1);
					curpos = ComputeScreenPos(curpos);
					half2 pjuv = curpos.xy / curpos.w;
#if UNITY_UV_STARTS_AT_TOP
					pjuv.y = 1 - pjuv.y;
#endif

#ifdef USE_COOKIE
					fixed4 cookie = tex2D(internalCookie, pjuv);
					fixed3 cookiecol = cookie.rgb*cookie.a;
#else
					half2 toCent = pjuv - half2(0.5, 0.5);
					half l = 1 - saturate((length(toCent) - 0.3) / (0.5 - 0.3));
					fixed3 cookiecol = fixed3(l, l, l);
#endif

					half dep = DecodeFloatRGBA(tex2D(internalShadowMap, pjuv)) / internalProjectionParams.w;
					float shadow = step(cdep, dep) *(1 - saturate(cdep*internalProjectionParams.w));

					col.rgb += cookiecol*i.vcol.rgb*delta / 2 * boardFac*shadow;
				}

				col.a = 1;

				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}
}
