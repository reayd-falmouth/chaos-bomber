Shader "Sprites/SilhouetteSuperSmoothGlow"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        [HDR] _InnerColor("Inner Color", Color) = (1,1,1,1)
        [HDR] _OuterColor("Outer Color", Color) = (0.71, 0.71, 0.42, 1)
        _GlowWidth("Glow Width", Range(0, 50)) = 15
        _GlowSoftness("Glow Softness", Range(0.1, 10)) = 2
        _GlowIntensity("Glow Intensity", Range(0, 10)) = 2.5
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off ZWrite Off
        // UI shaders must respect source alpha so CanvasGroup alpha can hide/show reliably.
        Blend SrcAlpha OneMinusSrcAlpha 

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 _InnerColor;
            float4 _OuterColor;
            float _GlowWidth;
            float _GlowSoftness;
            float _GlowIntensity;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
                fixed4 color : COLOR;
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
                
                // Continuous Distance Sampling
                float maxAlpha = 0;
                // Increase samples to 16 for a smoother blur at high widths
                const int SAMPLES = 16; 
                
                for(int s = 0; s < SAMPLES; s++) {
                    float angle = s * (6.2831 / (float)SAMPLES);
                    float2 dir = float2(cos(angle), sin(angle));
                    
                    // Sample at multiple distances to create a smooth field
                    for(float d = 0.1; d <= 1.0; d += 0.2) {
                        float2 off = dir * d * _GlowWidth * _MainTex_TexelSize.xy;
                        float sampleA = tex2D(_MainTex, i.uv + off).a;
                        // Use a falloff based on the sampling distance 'd'
                        maxAlpha = max(maxAlpha, sampleA * (1.0 - d));
                    }
                }

                // Isolate the glow area
                float glowMask = saturate(maxAlpha - mainAlpha);
                
                // Color Gradient: Lerp based on the raw distance
                // This ensures OuterColor is visible at the fringe
                float3 finalGlowCol = lerp(_OuterColor.rgb, _InnerColor.rgb, pow(glowMask, 0.5));
                
                // Alpha Falloff: This controls the "Blurry" edge
                float falloff = pow(glowMask, _GlowSoftness);

                float3 silhouette = float3(0,0,0) * mainAlpha; 
                float3 glow = finalGlowCol * falloff * _GlowIntensity;
                float finalA = saturate(mainAlpha + (falloff * _GlowIntensity));
                finalA *= i.color.a; // CanvasGroup alpha comes through as vertex color alpha for UI.

                return fixed4(silhouette + glow, finalA);
            }
            ENDCG
        }
    }
}