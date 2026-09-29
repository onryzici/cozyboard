Shader "CozyBoard/PaintedCutout" {
 Properties { _BaseMap("Artwork",2D)="white"{} _Opacity("Opacity",Range(0,1))=1 _Silhouette("Shadow",Range(0,1))=0 }
 SubShader {
  Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+10" }
  Pass {
   Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial)
   float4 _BaseMap_ST; float _Opacity; float _Silhouette;
   CBUFFER_END
   struct A { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
   struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
   V vert(A i) { V o; o.positionCS=TransformObjectToHClip(i.positionOS.xyz); o.uv=i.uv; return o; }
   half4 frag(V i):SV_Target {
    half4 c=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv);
    if(_Silhouette>.5) {
     float a=c.a;
     a+=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv+float2(.008,0)).a;
     a+=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv-float2(.008,0)).a;
     a+=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv+float2(0,.008)).a;
     a+=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv-float2(0,.008)).a;
     c=half4(.12,.10,.13,a*.2);
    }
    c.a*=_Opacity; return c;
   }
   ENDHLSL
  }
 }
}
