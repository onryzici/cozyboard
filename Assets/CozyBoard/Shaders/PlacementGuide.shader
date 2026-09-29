Shader "CozyBoard/PlacementGuide" {
 SubShader {Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+20" "RenderType"="Transparent"}
 Pass {Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct A{float4 p:POSITION;float2 uv:TEXCOORD0;};struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;};
 V vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.uv=i.uv;return o;}
 half4 frag(V i):SV_Target {
 float2 q=abs(i.uv-.5)-.43;float d=max(q.x,q.y);
 float edge=1-smoothstep(.003,.003+fwidth(d)*1.1,abs(d));
 float coord=q.x>q.y?i.uv.y:i.uv.x;
 float dash=1-smoothstep(.58,.63,frac(coord*18));
 return half4(.18,.24,.23,edge*dash*.62);
 }
 ENDHLSL
 }}
}
