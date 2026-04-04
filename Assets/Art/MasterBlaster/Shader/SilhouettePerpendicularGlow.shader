Shader "Sprites/SilhouettePerpendicularGlow"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        [HDR] _GlowColor("Glow Color", Color) = (0.71, 0.71, 0.42, 1) // Olive Gold
        _GlowWidth("Aura Width", Range(0, 30)) = 10
        _GlowSoftness("Aura Softness", Range(0.1, 10)) = 4
        _GlowIntensity("Aura Intensity", Range(0, 10)) = 2.5
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "PreviewType"="Plane" }
        Cull Off ZWrite Off
        Blend One OneMinusSrcAlpha 

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 _GlowColor;
            float _GlowWidth;
            float _GlowSoftness;
            float _GlowIntensity;

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert(appdata_base v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                float mainAlpha = tex2D(_MainTex, i.uv).a;
                
                // We use multiple "rings" to calculate distance from the edge
                float distField = 0;
                const int RINGS = 4; // How many layers of glow
                const int SAMPLES = 8; // Samples per layer
                
                for(int r = 1; r <= RINGS; r++) {
                    float ringStrength = 1.0 - ((float)r / (float)RINGS);
                    float currentWidth = _GlowWidth * ((float)r / (float)RINGS);
                    
                    float ringAlpha = 0;
                    for(int s = 0; s < SAMPLES; s++) {
                        float angle = s * (6.28 / (float)SAMPLES);
                        float2 off = float2(cos(angle), sin(angle)) * currentWidth * _MainTex_TexelSize.xy;
                        // Using max() prevents the "triangular beam" in gaps
                        ringAlpha = max(ringAlpha, tex2D(_MainTex, i.uv + off).a);
                    }
                    // Accumulate a smooth distance-based mask
                    distField = max(distField, ringAlpha * ringStrength);
                }

                // Mask out the silhouette so the glow is strictly outward
                float glowMask = saturate(distField - mainAlpha);
                
                // Apply falloff curve
                glowMask = pow(glowMask, _GlowSoftness);

                // Silhouette = Solid Black
                float3 silhouette = float3(0,0,0) * mainAlpha; 
                
                // Glow = The Olive Gold Aura
                float3 glow = _GlowColor.rgb * glowMask * _GlowIntensity;

                // Combine alpha: ensures the glow is visible even on transparent backgrounds
                float finalA = saturate(mainAlpha + (glowMask * _GlowIntensity));

                return fixed4(silhouette + glow, finalA);
            }
            ENDCG
        }
    }
}