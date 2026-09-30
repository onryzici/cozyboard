Shader "CozyBoard/KeycapStudio" {
 Properties { _BaseMap("Paint",2D)="white"{} _BaseColor("Color",Color)=(1,1,1,1) _Smoothness("Finish",Range(0,1))=.32 }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"} Pass {
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
 CBUFFER_START(UnityPerMaterial)
 float4 _BaseColor; float _Smoothness;
 float4 _WetPoints[12]; float _WetTimes[12];
 CBUFFER_END
 struct A {float4 p:POSITION; float3 n:NORMAL; float2 uv:TEXCOORD0;};
 struct V {float4 p:SV_POSITION; float3 n:TEXCOORD0; float3 w:TEXCOORD1; float2 uv:TEXCOORD2;float3 local:TEXCOORD3;};
 V vert(A a){V o;o.w=TransformObjectToWorld(a.p.xyz);o.p=TransformWorldToHClip(o.w);o.n=TransformObjectToWorldNormal(a.n);o.uv=a.uv;o.local=a.p.xyz;return o;}
 half4 frag(V i):SV_Target{
 float3 n=normalize(i.n),l=normalize(float3(-.6,1,-.7)),v=normalize(_WorldSpaceCameraPos-i.w);
 float key=saturate(dot(n,l)),fill=saturate(dot(n,normalize(float3(.8,.2,.4))));
 float3 color=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb*_BaseColor.rgb;
 float wet=0;
 [unroll] for(int k=0;k<12;k++){float age=_Time.y-_WetTimes[k];float r=_WetPoints[k].w;if(r>.00001&&age<2.4){float dist=length(i.local-_WetPoints[k].xyz)/(r*(1+min(age,.2)*.22));wet=max(wet,(1-smoothstep(.68,1,dist))*saturate(1-age/2.4));}}
 float grain=sin(i.local.x*1100+i.local.z*520)*sin(i.local.z*980+i.local.y*450);
 color*=1+grain*.009-wet*.045;
 float wetShine=pow(saturate(dot(n,normalize(l+v))),52)*wet*.36;
 float spec=pow(saturate(dot(n,normalize(l+v))),lerp(18,110,_Smoothness))*.23;
 return half4(color*(.46+.48*key+.13*fill)+spec+wetShine,1);
 }
 ENDHLSL
 }}
}
