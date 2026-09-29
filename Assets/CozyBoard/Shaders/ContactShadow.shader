Shader "CozyBoard/ContactShadow" {
 Properties { _Footprint("Footprint",Vector)=(6.6,2.6,0,0) _PlaneSize("Plane size",Vector)=(7.4,3.4,0,0) _Softness("Softness",Float)=.16 _Opacity("Opacity",Range(0,1))=.32 }
 SubShader {
  Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" }
  Pass {
   Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   CBUFFER_START(UnityPerMaterial)
   float4 _Footprint; float4 _PlaneSize; float _Softness; float _Opacity;
   CBUFFER_END
   struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
   struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
   Varyings vert(Attributes i) { Varyings o; o.positionCS=TransformObjectToHClip(i.positionOS.xyz); o.uv=i.uv; return o; }
   half4 frag(Varyings i):SV_Target {
    float2 p=(i.uv-.5)*_PlaneSize.xy;
    float radius=min(.12,min(_Footprint.x,_Footprint.y)*.2);
    float2 q=abs(p)-(_Footprint.xy*.5-radius);
    float d=length(max(q,0))+min(max(q.x,q.y),0)-radius;
    return half4(.12,.085,.09,_Opacity*(1-smoothstep(-_Softness*.3,_Softness,d)));
   }
   ENDHLSL
  }
 }
}
