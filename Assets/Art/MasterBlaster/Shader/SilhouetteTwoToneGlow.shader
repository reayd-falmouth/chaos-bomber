Shader "Sprites/SilhouetteTwoToneGlow"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        [HDR] _InnerColor("Inner Glow Color", Color) = (1, 1, 0.5, 1) 
        [HDR] _OuterColor("Outer Glow Color", Color) = (0.71, 0.71, 0.42, 1) // Olive Gold
        _GlowWidth("Aura Width", Range(0, 30)) = 10
        _GlowSoftness("Aura Softness", Range(0.1, 10)) = 4
        _GlowIntensity("Aura Intensity", Range(0, 10)) = 2.5
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "PreviewType"="Plane" } [cite: 2]
        Cull Off ZWrite Off
        Blend One OneMinusSrcAlpha 

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex; [cite: 3]
            float4 _MainTex_TexelSize;
            float4 _InnerColor;
            float4 _OuterColor;
            float _GlowWidth;
            float _GlowSoftness;
            float _GlowIntensity; [cite: 4]

            struct v2f {
                float4 pos : SV_POSITION; [cite: 5]
                float2 uv  : TEXCOORD0;
            };

            v2f vert(appdata_base v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex); [cite: 6]
                o.uv = v.texcoord;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                float mainAlpha = tex2D(_MainTex, i.uv).a;
                float distField = 0; 
                const int RINGS = 4; [cite: 8]
                const int SAMPLES = 8; [cite: 9]
                
                for(int r = 1; r <= RINGS; r++) {
                    float ringStrength = 1.0 - ((float)r / (float)RINGS);
                    float currentWidth = _GlowWidth * ((float)r / (float)RINGS); [cite: 10]
                    
                    float ringAlpha = 0;
                    for(int s = 0; s < SAMPLES; s++) {
                        float angle = s * (6.28 / (float)SAMPLES); [cite: 11]
                        float2 off = float2(cos(angle), sin(angle)) * currentWidth * _MainTex_TexelSize.xy; 
                        ringAlpha = max(ringAlpha, tex2D(_MainTex, i.uv + off).a); 
                    }
                    distField = max(distField, ringAlpha * ringStrength); [cite: 14]
                }

                // Calculate the base mask
                float glowMask = saturate(distField - mainAlpha); [cite: 14]
                
                // Use a non-pow'd mask for the color lerp to keep the gradient smooth
                float3 lerpedColor = lerp(_OuterColor.rgb, _InnerColor.rgb, glowMask); 

                // Apply falloff curve to the intensity [cite: 15]
                float finalGlowMask = pow(glowMask, _GlowSoftness);

                float3 silhouette = float3(0,0,0) * mainAlpha; [cite: 16]
                float3 glow = lerpedColor * finalGlowMask * _GlowIntensity; [cite: 17]

                float finalA = saturate(mainAlpha + (finalGlowMask * _GlowIntensity)); [cite: 18]

                return fixed4(silhouette + glow, finalA); [cite: 19]
            }
            ENDCG
        }
    }
}