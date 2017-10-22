// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "VolumetricLight/Caster/VLBlinnPhongBump"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		[Normal][NoScaleOffset]_BumpTex("BumpTex", 2D) = "bump" {}
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
			#pragma target 3.0
			#pragma multi_compile_fog
			#pragma multi_compile __ USE_COOKIE
			
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "../VLUtils.cginc"

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
				VL_SHADOW_COORD(2, 3)
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
			
			v2f vert (appdata_full v)
			{
				v2f o;
				
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				VL_TRANSFER_SHADOW(o)

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
				VL_APPLY_SHADOW(i)
				float3 worldPos = float3(i.RT0.w,i.RT1.w,i.RT2.w);
				float3 rnormal = UnpackNormal(tex2D(_BumpTex, i.uv));
				float3 worldNormal = float3(dot(i.RT0.xyz, rnormal), dot(i.RT1.xyz, rnormal), dot(i.RT2.xyz, rnormal));
				
				float3 litDir = normalize(GetLightDirection(worldPos.xyz));
				float3 viewDir = normalize(UnityWorldSpaceViewDir(worldPos.xyz));
				float3 h = normalize(viewDir + litDir);
				float ndl = max(0, dot(worldNormal, litDir));
				float spec = max(0, dot(worldNormal, h));
				float4 gi = texCUBE(_GI, worldNormal);

				fixed4 col = tex2D(_MainTex, i.uv);

				col.rgb *= UNITY_LIGHTMODEL_AMBIENT.rgb + VL_LIGHT*(ndl*gi.rgb + _SpecColor.rgb * pow(spec, _Specular)*_Gloss) *VL_ATTEN;
				
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}
}
