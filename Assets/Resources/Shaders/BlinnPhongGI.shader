// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Lighting/Forward/BlinnPhongGI"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_SpecColor ("SpecCol", color) = (1,1,1,1)
		_Specular ("Specular", float) = 0
		_Gloss ("Gloss", float) = 0
		_GI ("GI", cube) = "" {}
		_GIColor ("GIColor", color) = (0,0,0,1)
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
				float3 worldNormal : TEXCOORD1;
				float4 worldPos : TEXCOORD2;
				UNITY_FOG_COORDS(3)
				float4 vertex : SV_POSITION;
				float depth : TEXCOORD4;
				float4 proj:TEXCOORD5;
			};

			sampler2D _MainTex;
			samplerCUBE _GI;
			float4 _MainTex_ST;

			float _Specular;
			half _Gloss;

			half4 _GIColor;

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
			
			v2f vert (appdata_base v)
			{
				v2f o;
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				o.vertex = mul(UNITY_MATRIX_VP, o.worldPos);
				o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.worldNormal = UnityObjectToWorldNormal(v.normal);
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
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 shadow = tex2Dproj(internalShadowMap, i.proj);
				fixed4 cookie = tex2Dproj(internalCookie, i.proj);
				float depth = DecodeFloatRGBA(shadow);
				float sc = step(i.depth - internalBias, depth)*0.7 + 0.3;
				sc *= 1 - saturate((i.depth - internalProjectionParams.x) / (1 - internalProjectionParams.x));

				float2 atten = saturate((0.5 - abs(i.proj.xy / i.proj.w - 0.5)) / (1 - internalWorldLightColor.a));
				float3 litDir = normalize(GetLightDirection(i.worldPos.xyz));
				float3 viewDir = normalize(UnityWorldSpaceViewDir(i.worldPos.xyz));
				float3 h = normalize(viewDir + litDir);
				float ndl = max(0, dot(i.worldNormal, litDir));
				float spec = max(0, dot(i.worldNormal, h));
				float4 gi = texCUBE(_GI, i.worldNormal)+ _GIColor;

				fixed4 col = tex2D(_MainTex, i.uv);

				col.rgb *= UNITY_LIGHTMODEL_AMBIENT.rgb + cookie.rgb*cookie.a*(internalWorldLightColor.rgb* ndl*gi.rgb + _SpecColor.rgb * pow(spec, _Specular)*_Gloss*internalWorldLightColor.rgb) *atten.x*atten.y*sc;
				
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
				//return fixed4(depth, depth, depth, 1);
			}
			ENDCG
		}
	}
}
