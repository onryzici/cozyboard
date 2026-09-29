Shader "CozyBoard/RoundedKeyUI" {
    Properties {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Aspect ("Aspect", Float) = 1
        _Radius ("Corner radius", Range(.01,.3)) = .11
    }
    SubShader {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off ZWrite Off ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            sampler2D _MainTex; float4 _Color; float _Aspect,_Radius;
            v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color*_Color;return o;}
            fixed4 frag(v2f i):SV_Target {
                float2 size=float2(max(1,_Aspect),1);float2 p=(i.uv-.5)*size;
                float2 q=abs(p)-(size*.5-_Radius);float distance=length(max(q,0))+min(max(q.x,q.y),0)-_Radius;
                float edge=1-smoothstep(-.006,.006,distance);fixed4 color=tex2D(_MainTex,i.uv)*i.color;color.a*=edge;return color;
            }
            ENDCG
        }
    }
}
