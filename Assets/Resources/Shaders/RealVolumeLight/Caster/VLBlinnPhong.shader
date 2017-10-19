// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "VolumeLight/Caster/VLBlinnPhong"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_SpecColor ("SpecCol", color) = (1,1,1,1)
		_Specular ("Specular", float) = 0
		_Gloss ("Gloss", float) = 0
		_GI ("GI", cube) = "" {}
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
			#pragma multi_compile __ USE_COOKIE
			
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

			uniform float4 internalWorldLightPos;
			uniform float4 internalWorldLightColor;

			sampler2D internalShadowMap;
#ifdef USE_COOKIE
			sampler2D internalCookie;
#endif
			float4x4 internalWorldLightMV;
			float4x4 internalWorldLightVP;
			float4 internalProjectionParams;
			float internalBias;

			float3 GetLightDirection(float3 worldPos) {
				return internalWorldLightPos.xyz*(2 * internalWorldLightPos.w - 1)-worldPos*internalWorldLightPos.w;
			}

			float LinearLightEyeDepth(float z)
			{
				return 1.0 / (internalProjectionParams.z * z + internalProjectionParams.w);
			}
			
			v2f vert (appdata_base v)
			{
				v2f o;
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				o.vertex = mul(UNITY_MATRIX_VP, o.worldPos);
				o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.worldNormal = UnityObjectToWorldNormal(v.normal);
				UNITY_TRANSFER_FOG(o,o.vertex);
				float4 vpos = mul(internalWorldLightMV, mul(unity_ObjectToWorld, v.vertex));
				o.proj = mul(internalWorldLightVP, vpos);
#if UNITY_UV_STARTS_AT_TOP
				float scale = -1.0;
#else
				float scale = 1.0;
#endif
				float4 pj = o.proj * 0.5f;
				pj.xy = float2(pj.x, pj.y) + pj.w;
				pj.zw = o.proj.zw;
				o.proj = pj;
				o.depth = -vpos.z;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 shadow = tex2Dproj(internalShadowMap, i.proj);
#ifdef USE_COOKIE
				fixed4 cookie = tex2Dproj(internalCookie, i.proj);
				fixed3 cookiecol = cookie.rgb*cookie.a;
#else
				half2 toCent = i.proj.xy / i.proj.w - half2(0.5, 0.5);
				half l = 1 - saturate((length(toCent) - 0.3) / (0.5 - 0.3));
				fixed3 cookiecol = fixed3(l, l, l);
#endif
				float depth = LinearLightEyeDepth(DecodeFloatRGBA(shadow));
				float sc = step(i.depth - internalBias, depth)*(1 - saturate(i.depth*internalProjectionParams.w));

				float2 atten = saturate((0.5 - abs(i.proj.xy / i.proj.w - 0.5)) / (1 - 0.999));
				float3 litDir = normalize(GetLightDirection(i.worldPos.xyz));
				float3 viewDir = normalize(UnityWorldSpaceViewDir(i.worldPos.xyz));
				float3 h = normalize(viewDir + litDir);
				float ndl = max(0, dot(i.worldNormal, litDir));
				float spec = max(0, dot(i.worldNormal, h));
				float4 gi = texCUBE(_GI, i.worldNormal);

				fixed4 col = tex2D(_MainTex, i.uv);

				col.rgb *= UNITY_LIGHTMODEL_AMBIENT.rgb + cookiecol*(internalWorldLightColor.rgb* ndl*gi.rgb + _SpecColor.rgb * pow(spec, _Specular)*_Gloss*internalWorldLightColor.rgb)*atten.x*atten.y *sc;
				
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}
}
