Shader "UI/SilhouetteOuterGlow_UI"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        [HDR] _GlowColor("Glow Color", Color) = (0,1,1,1)
        _GlowWidthPixels("Glow Width", Range(0, 50)) = 10
        _GlowSoftness("Glow Softness", Range(0.1, 10)) = 2
        _GlowIntensity("Glow Intensity", Range(0, 10)) = 2
        
        // Required for UI.Image compatibility
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
        
        // Stencil block required for UI masking support
        Stencil {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha // Standard UI blending

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            // Removed _TexelSize dependency for UI stability
            float4 _GlowColor;
            float _GlowWidthPixels;
            float _GlowSoftness;
            float _GlowIntensity;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                fixed4 color  : COLOR;
            };

            struct v2f {
                float4 pos    : SV_POSITION;
                float2 uv     : TEXCOORD0;
                fixed4 color  : COLOR;
            };

            v2f vert(appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                float mainAlpha = tex2D(_MainTex, i.uv).a;
                
                // For UI, we use a constant small step if TexelSize is failing
                float2 step = float2(0.001, 0.001) * _GlowWidthPixels;
                
                float combinedAlpha = 0;
                // 8-directional sampling
                combinedAlpha += tex2D(_MainTex, i.uv + float2(step.x, 0)).a;
                combinedAlpha += tex2D(_MainTex, i.uv - float2(step.x, 0)).a;
                combinedAlpha += tex2D(_MainTex, i.uv + float2(0, step.y)).a;
                combinedAlpha += tex2D(_MainTex, i.uv - float2(0, step.y)).a;
                combinedAlpha += tex2D(_MainTex, i.uv + float2(step.x, step.y) * 0.7).a;
                combinedAlpha += tex2D(_MainTex, i.uv + float2(-step.x, step.y) * 0.7).a;
                combinedAlpha += tex2D(_MainTex, i.uv + float2(step.x, -step.y) * 0.7).a;
                combinedAlpha += tex2D(_MainTex, i.uv - float2(step.x, step.y) * 0.7).a;
                combinedAlpha /= 8.0;

                // Expand and smooth the glow
                float glowMask = saturate(combinedAlpha - mainAlpha);
                glowMask = pow(glowMask, 1.0 / _GlowSoftness); // Inverted softness for easier UI control

                // Silhouette = Black
                float4 silhouette = float4(0, 0, 0, mainAlpha);
                
                // Glow = HDR Color
                float4 glow = _GlowColor * glowMask * _GlowIntensity;

                // Output: Glow + Silhouette
                float4 final = silhouette + glow;
                final.a = saturate(mainAlpha + glowMask * _GlowIntensity);
                
                return final;
            }
            ENDCG
        }
    }
}