Shader "CozyBoard/FlatWorkspace" {
 Properties { _Surface("Surface",Color)=(.84,.67,.55,1) _Mat("Mat",Color)=(.65,.70,.73,1) }
 SubShader { Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
 Pass { Cull Off ZWrite On
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "WorkshopPaint.hlsl"
 CBUFFER_START(UnityPerMaterial)
 half4 _Surface; half4 _Mat;
 CBUFFER_END
 struct A {float4 positionOS:POSITION;float2 uv:TEXCOORD0;};
 struct V {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;float3 world:TEXCOORD1;};
 V vert(A i){V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.uv=i.uv;o.world=TransformObjectToWorld(i.positionOS.xyz);return o;}
 float rounded(float2 p,float2 b,float r){float2 q=abs(p)-b+r;return length(max(q,0))+min(max(q.x,q.y),0)-r;}
 half4 frag(V i):SV_Target {
  float2 p=i.world.xz;
  // Pale honey oak, with quiet lengthwise grain and broad natural variation.
  float warp=paintNoise(float2(p.x*.32,p.y*.48))*.7;
  float grain=paintNoise(float2(p.x*.24,(p.y+warp)*21))-.5;
  float broad=paintNoise(float2(p.x*.16,p.y*2.4))-.5;
  float fibers=sin((p.y+warp*.15)*145+paintNoise(p*.8)*4);
  half3 surface=half3(.57,.38,.22)+broad*.065+grain*.040+fibers*.004;
  float sunwash=1-smoothstep(0,12,length(p-float2(-5,4)));
  surface+=half3(.025,.020,.012)*sunwash;
  float2 center=float2(0,-.08),halfSize=float2(5.05,3.45);
  float wobble=(paintNoise(p*3)-.5)*.047+(paintNoise(p*17)-.5)*.009;
  float shadow=1-smoothstep(-.02,.12,rounded(p-center-float2(.08,-.10),halfSize,.20));
  surface=lerp(surface,surface*half3(.67,.66,.72),shadow*.42);
  float d=rounded(p-center,halfSize,.23)+wobble;
  float aa=max(fwidth(d),.002);
  float mask=1-smoothstep(-aa,aa,d);
  half3 matColor=_Mat.rgb+pigment(p)*.72;
  float inner=rounded(p-center,halfSize-.38,.015);
  float inside=1-smoothstep(-.005,.004,inner);
  float border=1-smoothstep(.009,.022,abs(inner));
  float2 g=p-center;
  float2 fine=abs(frac(g/.32+.5)-.5)*.32;
  float2 major=abs(frac(g/.64+.5)-.5)*.64;
  float aaGrid=max(fwidth(g.x),fwidth(g.y));
  float minorLine=1-smoothstep(.003,.003+aaGrid,min(fine.x,fine.y));
  float dash=step(.42,frac((g.x+g.y)*27));
  float majorLine=1-smoothstep(.007,.007+aaGrid,min(major.x,major.y));
  float axisLine=1-smoothstep(.022,.022+aaGrid,min(abs(g.x),abs(g.y)));
  float lines=max(border,max(axisLine*.90,max(majorLine*.53,minorLine*dash*.32))*inside);
  matColor=lerp(matColor,matColor*half3(.48,.51,.58),lines);
  // Thin darker lip and a broad irregular painted highlight, no glossy bevel.
  float worn=smoothstep(-.18,-.035,d)*step(.52,paintNoise(p*21));
  matColor+=worn*.025;
  matColor*=1-smoothstep(-.045,0,d)*.12;
  matColor+=(1-smoothstep(.02,.16,abs(d+.21)))*.008;
  half3 result=lerp(surface,matColor,mask);
  // Grid mat receives the same light as the work surface.
  return half4(windowColor(result,p),1);
 }
 ENDHLSL
 }}
}
