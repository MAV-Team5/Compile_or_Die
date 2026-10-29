// 증강 이펙트를 아이콘 고유색으로 다시 칠한다.
// 원래 그림의 밝기(가장 밝은 채널)를 색의 세기로, 흰기(가장 어두운 채널)를 흰 속심으로 쓴다 —
// 파란 칼날이든 초록 파티클이든 모양·번짐은 그대로 두고 색만 _NeonColor 로 바뀐다.
Shader "CoD/NeonRecolor"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _NeonColor ("Neon Color", Color) = (1,1,1,1)
        _Boost ("Boost", Float) = 1.6
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _NeonColor;
                float _Boost;
            CBUFFER_END

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = TransformObjectToHClip(v.vertex.xyz);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                half4 t = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                // 밝기는 가장 밝은 채널, 흰기는 가장 어두운 채널
                half v = max(t.r, max(t.g, t.b));
                half w = min(t.r, min(t.g, t.b));
                // 어두운 부분까지 끌어올려 색을 꽉 채운다 — 흐릿하지 않게
                half lit = saturate(pow(v, 0.55) * _Boost);
                half3 neon = _NeonColor.rgb * lit;
                // 정말 하얀 심지만 흰색으로 — 나머지는 전부 형광색
                half core = smoothstep(0.82, 1.0, w);
                half3 rgb = lerp(neon, half3(1,1,1), core * 0.7);
                // 원래 반투명하게 깔린 이펙트(알파 0.4 등)도 또렷하게 — 페이드 곡선은 유지
                half a = saturate(t.a * 1.35 * sqrt(saturate(i.color.a)) * 1.25);
                return half4(rgb, a);
            }
            ENDHLSL
        }
    }
}
