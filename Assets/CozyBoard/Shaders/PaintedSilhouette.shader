Shader "CozyBoard/PaintedSilhouette" {
 Properties { _GroundY("Receiving surface",Float)=.018 _Opacity("Opacity",Range(0,1))=.30 }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent-10"}
 Pass {Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Back
 Stencil {Ref 16 Comp NotEqual Pass Replace ReadMask 16 WriteMask 16}
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 CBUFFER_START(UnityPerMaterial)
 float _GroundY;float _Opacity;
 CBUFFER_END
 struct A{float4 p:POSITION;};struct V{float4 p:SV_POSITION;float2 world:TEXCOORD0;};
 V vert(A i){V o;float3 w=TransformObjectToWorld(i.p.xyz);float lift=max(0,w.y-_GroundY);w.x+=lift*.52+.055;w.z-=lift*.66+.07;w.y=_GroundY;o.world=w.xz;o.p=TransformWorldToHClip(w);return o;}
 half4 frag(V i):SV_Target{return half4(.12,.17,.19,_Opacity);}
 ENDHLSL
 }}
}
