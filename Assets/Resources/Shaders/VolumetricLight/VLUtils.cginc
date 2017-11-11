#ifndef VL_UTILS
#define VL_UTILS 

sampler2D internalShadowMap;
#ifdef USE_COOKIE
sampler2D internalCookie;
#endif

uniform float4 internalWorldLightPos;
uniform float4 internalWorldLightColor;


float4x4 internalWorldLightMV;
float4x4 internalWorldLightVP;
float4 internalProjectionParams;
float internalBias;

#define VL_SHADOW_COORD(n,m) float vl_depth:TEXCOORD##n; \
							float4 vl_proj:TEXCOORD##m; 

//#if UNITY_UV_STARTS_AT_TOP
//#define VL_SCALE float vl_scale = -1.0; 
//#else
//#define VL_SCALE float vl_scale = 1.0; 
//#endif

#ifdef USE_COOKIE
#define COOKIE_COLOR(o) fixed4 vl_cookie = tex2Dproj(internalCookie, o.vl_proj); \
						fixed3 vl_cookiecol = vl_cookie.rgb*vl_cookie.a; 
#else
#define COOKIE_COLOR(o) half2 toCent = o.vl_proj.xy / o.vl_proj.w - half2(0.5, 0.5); \
					half l = 1 - saturate((length(toCent) - 0.3) / (0.5 - 0.3)); \
					fixed3 vl_cookiecol = fixed3(l, l, l); 
#endif

#define VL_TRANSFER_SHADOW(o) float4 vpos = mul(internalWorldLightMV, mul(unity_ObjectToWorld, v.vertex)); \
								o.vl_proj = mul(internalWorldLightVP, vpos); \
							float4 pj = o.vl_proj * 0.5f; \
							pj.xy = float2(pj.x, pj.y) + pj.w; \
							pj.zw = o.vl_proj.zw; \
							o.vl_proj = pj; \
							o.vl_depth = -vpos.z; 

#define VL_APPLY_SHADOW(i)  fixed4 shadow = tex2Dproj(internalShadowMap, i.vl_proj); \
							COOKIE_COLOR(i) \
							float depth = shadow.r / internalProjectionParams.w; \
							float vl_shadow = step(i.vl_depth - internalBias, depth) *(1 - saturate(i.vl_depth*internalProjectionParams.w)); \
							float2 vl_atten = saturate((0.5 - abs(i.vl_proj.xy / i.vl_proj.w - 0.5)) / (1 - 0.999)); 

#define VL_LIGHT vl_cookiecol * internalWorldLightColor.rgb \

#define VL_ATTEN vl_atten.x*vl_atten.y *vl_shadow\

inline float3 GetLightDirection(float3 worldPos) {
	return internalWorldLightPos.xyz*(2 * internalWorldLightPos.w - 1) - worldPos*internalWorldLightPos.w;
}


#endif