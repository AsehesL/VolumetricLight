// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "VolumetricLight/Caster/VLBlinnPhong"
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
			#pragma target 3.0
			#pragma multi_compile_fog
			#pragma multi_compile __ USE_COOKIE
			
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "../VLUtils.cginc"

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float3 worldNormal : TEXCOORD1;
				float4 worldPos : TEXCOORD2;
				UNITY_FOG_COORDS(3)
				float4 vertex : SV_POSITION;
				VL_SHADOW_COORD(4,5)
			};

			sampler2D _MainTex;
			samplerCUBE _GI;
			float4 _MainTex_ST;

			float _Specular;
			half _Gloss;
			
			v2f vert (appdata_base v)
			{
				v2f o;
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				o.vertex = mul(UNITY_MATRIX_VP, o.worldPos);
				o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.worldNormal = UnityObjectToWorldNormal(v.normal);
				UNITY_TRANSFER_FOG(o,o.vertex);
				VL_TRANSFER_SHADOW(o)
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				VL_APPLY_SHADOW(i) 
				float3 litDir = normalize(GetLightDirection(i.worldPos.xyz));
				float3 viewDir = normalize(UnityWorldSpaceViewDir(i.worldPos.xyz));
				float3 h = normalize(viewDir + litDir);
				float ndl = max(0, dot(i.worldNormal, litDir));
				float spec = max(0, dot(i.worldNormal, h));
				float4 gi = texCUBE(_GI, i.worldNormal);

				fixed4 col = tex2D(_MainTex, i.uv); 
				col.rgb *= UNITY_LIGHTMODEL_AMBIENT.rgb + VL_LIGHT*(ndl*gi.rgb + _SpecColor.rgb * pow(spec, _Specular)*_Gloss) *VL_ATTEN;
				
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}
}
