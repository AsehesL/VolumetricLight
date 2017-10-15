// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Lighting/Forward/BlinnPhongRim"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		[Normal][NoScaleOffset]_BumpTex("BumpTex", 2D) = "bump" {}
		_SpecColor ("SpecCol", color) = (1,1,1,1)
		_Specular ("Specular", float) = 0
		_Gloss ("Gloss", float) = 0
		_GI ("GI", cube) = "" {}
		_GIColor ("GIColor", color) = (0,0,0,1)
		_RimColor ("RimColor", color) = (0,0,0,1)
		_RimPower ("RimPower", float) = 0
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"
			#include "Lighting.cginc"

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
				float depth : TEXCOORD2;
				float4 proj:TEXCOORD3;
				float4 RT0 : TEXCOORD4;
				float4 RT1 : TEXCOORD5;
				float4 RT2 : TEXCOORD6;
			};

			sampler2D _BumpTex;
			sampler2D _MainTex;
			samplerCUBE _GI;
			float4 _MainTex_ST;

			float _Specular;
			half _Gloss;

			half4 _GIColor;

			half4 _RimColor;
			float _RimPower;

			uniform float4 internalWorldLightPos;
			uniform float4 internalWorldLightColor;

			sampler2D internalShadowMap;
			sampler2D internalCookie;
			float4x4 internalWorldLightMV;
			float4x4 internalWorldLightVP;
			float4 internalProjectionParams;
			float internalBias;

			float3 GetLightDirection(float3 worldPos) {
				if (internalWorldLightPos.w == 0)
					return -internalWorldLightPos.xyz;
				else
					return internalWorldLightPos.xyz - worldPos;
			}
			
			v2f vert (appdata_full v)
			{
				v2f o;
				
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				float4 cpos = mul(internalWorldLightMV, mul(unity_ObjectToWorld, v.vertex));
				o.proj = mul(internalWorldLightVP, cpos);
#if UNITY_UV_STARTS_AT_TOP
				float scale = -1.0;
#else
				float scale = 1.0;
#endif
				float4 pj = o.proj * 0.5f;
				pj.xy = float2(pj.x, pj.y) + pj.w;
				pj.zw = o.proj.zw;
				o.proj = pj;
				o.depth = -cpos.z*internalProjectionParams.w;

				float3 worldPos = mul(unity_ObjectToWorld, v.vertex);
				float3 worldNormal = UnityObjectToWorldNormal(v.normal);
				float3 worldTan = UnityObjectToWorldDir(v.tangent.xyz);
				float tanSign = v.tangent.w * unity_WorldTransformParams.w;
				float3 worldBinormal = cross(worldNormal, worldTan)*tanSign;
				o.RT0 = float4(worldTan.x, worldBinormal.x, worldNormal.x, worldPos.x);
				o.RT1 = float4(worldTan.y, worldBinormal.y, worldNormal.y, worldPos.y);
				o.RT2 = float4(worldTan.z, worldBinormal.z, worldNormal.z, worldPos.z);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float3 worldPos = float3(i.RT0.w,i.RT1.w,i.RT2.w);
				float3 rnormal = UnpackNormal(tex2D(_BumpTex, i.uv));
				float3 worldNormal = float3(dot(i.RT0.xyz, rnormal), dot(i.RT1.xyz, rnormal), dot(i.RT2.xyz, rnormal));

				fixed4 shadow = tex2Dproj(internalShadowMap, i.proj);
				fixed4 cookie = tex2Dproj(internalCookie, i.proj);
				float depth = DecodeFloatRGBA(shadow);
				float sc = step(i.depth - internalBias, depth)*0.7 + 0.3;
				sc *= 1 - saturate((i.depth - internalProjectionParams.x) / (1 - internalProjectionParams.x));

				float2 atten = saturate((0.5 - abs(i.proj.xy / i.proj.w - 0.5)) / (1 - internalWorldLightColor.a));
				float3 litDir = normalize(GetLightDirection(worldPos.xyz));
				float3 viewDir = normalize(UnityWorldSpaceViewDir(worldPos.xyz));
				float3 h = normalize(viewDir + litDir);
				float ndl = max(0, dot(worldNormal, litDir));
				float ndv = 1 - max(0, dot(worldNormal, viewDir));
				float spec = max(0, dot(worldNormal, h));
				float4 gi = texCUBE(_GI, worldNormal)+ _GIColor;

				fixed4 col = tex2D(_MainTex, i.uv);

				fixed3 rim = _RimColor.rgb*cookie.rgb*cookie.a*pow(ndv, _RimPower)*sc;

				col.rgb *= UNITY_LIGHTMODEL_AMBIENT.rgb + cookie.rgb*cookie.a*(internalWorldLightColor.rgb* ndl*gi.rgb + _SpecColor.rgb * pow(spec, _Specular)*_Gloss*internalWorldLightColor.rgb) *atten.x*atten.y*sc;
				
				col.rgb += rim;
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
				//return fixed4(depth, depth, depth, 1);
			}
			ENDCG
		}
	}
}
