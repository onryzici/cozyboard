Shader "CozyBoard/WorkMat" {
 Properties { _BaseColor("Mat colour",Color)=(.37,.51,.48,1) _Size("Surface size",Vector)=(3.5,2.7,0,0) _Grid("Grid spacing",Float)=.24 _Border("Printed border",Float)=1 _Shadow("Soft key shadow",Float)=0 }
 SubShader {Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"} Pass {Cull Off
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "WorkshopPaint.hlsl"
 CBUFFER_START(UnityPerMaterial)
 float4 _BaseColor,_Size;float _Grid,_Border,_Shadow;
 float4 _ShadowPoints[16];int _ShadowCount;
 CBUFFER_END
 struct A{float4 p:POSITION;float2 uv:TEXCOORD0;};struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;};
 V vert(A a){V o;o.p=TransformObjectToHClip(a.p.xyz);o.uv=a.uv;return o;}
 float grid(float2 p,float spacing){float2 q=abs(frac(p/spacing-.5)-.5)*spacing;float2 aa=max(fwidth(p),.0001);return 1-min(smoothstep(aa.x*.4,aa.x*1.4,q.x),smoothstep(aa.y*.4,aa.y*1.4,q.y));}
 half4 frag(V i):SV_Target{
 float2 p=(i.uv-.5)*_Size.xy;
 float fine=paintNoise(p*190)-.5;
 float3 color=_BaseColor.rgb+fine*.004;
 color*=1-grid(p,_Grid)*.10-grid(p,_Grid*5)*.07;
 float edge=min((.5-abs(i.uv.x))*_Size.x,(.5-abs(i.uv.y))*_Size.y);
 float aa=max(fwidth(edge),.0005);float border=(1-smoothstep(aa,aa*2,abs(edge-.12)))+(1-smoothstep(aa,aa*2,abs(edge-.18)))*.45;
 color*=1-border*.24*_Border;
 float distanceToEdge=1000;
 [loop] for(int j=0;j<_ShadowCount;j++){float2 a=_ShadowPoints[j].xy,b=_ShadowPoints[(j+1)%_ShadowCount].xy;float2 edge=b-a;distanceToEdge=min(distanceToEdge,(edge.x*(p.y-a.y)-edge.y*(p.x-a.x))/max(length(edge),.0001));}
 float shadow=_ShadowCount>2?smoothstep(-.055,.025,distanceToEdge)*_Shadow:0;color*=1-shadow*.28;
 return half4(color,1);
 }
 ENDHLSL
 }}
}
