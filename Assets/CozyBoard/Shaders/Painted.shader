Shader "CozyBoard/Painted" {
 Properties { _BaseMap("Pigment",2D)="white"{} _BaseColor("Color",Color)=(1,1,1,1) _Shading("Painted normal shading",Range(0,1))=1 }
 SubShader {
  Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
  Pass {
   Tags { "LightMode"="UniversalForward" }
   Cull Back ZWrite On
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "WorkshopPaint.hlsl"
   TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial)
   float4 _BaseMap_ST; half4 _BaseColor; float _Shading;
   CBUFFER_END
   struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; };
   struct Varyings { float4 positionCS:SV_POSITION; float3 normalWS:TEXCOORD0; float2 uv:TEXCOORD1; float3 world:TEXCOORD2; };
   Varyings vert(Attributes i) { Varyings o; o.positionCS=TransformObjectToHClip(i.positionOS.xyz); o.world=TransformObjectToWorld(i.positionOS.xyz); o.normalWS=TransformObjectToWorldNormal(i.normalOS); o.uv=TRANSFORM_TEX(i.uv,_BaseMap); return o; }
   half4 frag(Varyings i):SV_Target {
    half3 paint=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb*_BaseColor.rgb;
    float3 n=normalize(i.normalWS);
    float light=dot(n,normalize(float3(-.65,.9,.65)));
    float band=smoothstep(-.30,.82,light);
    half3 tone=lerp(half3(.45,.48,.56),half3(1,.985,.955),band);
    paint*=lerp(half3(1,1,1),tone,_Shading);
    paint+=pigment(i.world.xz*2.5)*.12;
    return half4(windowColor(paint,i.world.xz),1);
   }
   ENDHLSL
  }
 }
}
