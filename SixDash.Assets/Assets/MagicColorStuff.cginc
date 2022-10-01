#ifndef MAGIC_COLOR_STUFF
#define MAGIC_COLOR_STUFF

struct VertexInputColor {
    float4 vertex   : POSITION;
    half3 normal    : NORMAL;
    float2 uv0      : TEXCOORD0;
#ifdef UNITY_STANDARD_CORE_FORWARD_INCLUDED
    float2 uv1      : TEXCOORD1;
    #if defined(DYNAMICLIGHTMAP_ON) || defined(UNITY_PASS_META)
    float2 uv2      : TEXCOORD2;
    #endif
#endif
    #ifdef _TANGENT_TO_WORLD
    half4 tangent   : TANGENT;
    #endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
    float4 color    : COLOR;
    float2 uv3      : TEXCOORD3;
};

float _RenderMin;
float _RenderMax;

float ease(float x) {
    return clamp(1.0 - pow(2.0, -10.0 * x), 0.0, 1.0);
}

VertexInput modifyScale(VertexInputColor v) {
    // color xyz = object position, color w = path x position, uv3 x = out animation end, uv3 y = in animation end
    VertexInput o;
    const float start = v.color.w;
    const float outEnd = v.uv3.x;
    const float inEnd = v.uv3.y;
    const float outT = ease((_RenderMin - start) / (outEnd - start));
    const float inT = ease((_RenderMax - start) / (inEnd - start));
    v.vertex.xyz = lerp(v.color.xyz, v.vertex.xyz, (1.0 - outT) * inT);
    o.vertex = v.vertex;
    o.normal = v.normal;
    o.uv0 = v.uv0;
#ifdef UNITY_STANDARD_CORE_FORWARD_INCLUDED
    o.uv1 = v.uv1;
    #if defined(DYNAMICLIGHTMAP_ON) || defined(UNITY_PASS_META)
    o.uv2 = v.uv2;
    #endif
#endif
    #ifdef _TANGENT_TO_WORLD
    o.tangent = v.tangent;
    #endif
    return o;
}

#ifdef UNITY_STANDARD_CORE_FORWARD_INCLUDED
#if UNITY_STANDARD_SIMPLE
VertexOutputBaseSimple
#else
VertexOutputForwardBase
#endif
    vertBaseScaled(VertexInputColor v) {
    return vertBase(modifyScale(v));
}

#if UNITY_STANDARD_SIMPLE
VertexOutputAddSimple
#else
VertexOutputForwardAdd
#endif
    vertAddScaled(VertexInputColor v) {
    return vertAdd(modifyScale(v));
}
#endif
#ifdef UNITY_STANDARD_SHADOW_INCLUDED
void vertShadowCasterScaled(VertexInputColor v
    , out float4 opos : SV_POSITION
    #ifdef UNITY_STANDARD_USE_SHADOW_OUTPUT_STRUCT
    , out VertexOutputShadowCaster o
    #endif
    #ifdef UNITY_STANDARD_USE_STEREO_SHADOW_OUTPUT_STRUCT
    , out VertexOutputStereoShadowCaster os
    #endif
) {
    vertShadowCaster(modifyScale(v), opos
        #ifdef UNITY_STANDARD_USE_SHADOW_OUTPUT_STRUCT
        , o
        #endif
        #ifdef UNITY_STANDARD_USE_STEREO_SHADOW_OUTPUT_STRUCT
        , os
        #endif
        );
}
#endif

#endif
