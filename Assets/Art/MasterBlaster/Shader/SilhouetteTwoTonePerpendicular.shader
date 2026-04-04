Shader "Sprites/SilhouetteTwoTonePerpendicular"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        [HDR] _InnerColor("Inner Color", Color) = (1,1,1,1)
        [HDR] _OuterColor("Outer Color", Color) = (0.71, 0.71, 0.42, 1) // Olive Gold
        _GlowWidth("Glow Width", Range(0, 30)) = 10
        _GlowSoftness("Glow Softness", Range(0.1, 10)) = 4
        _GlowIntensity("Glow Intensity", Range(0, 10)) = 2.5
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
            float4 _InnerColor;
            float4 _OuterColor;
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
                float distField = 0;
                const int RINGS = 4; // [cite: 8]
                const int SAMPLES = 8; // [cite: 8]
                
                for(int r = 1; r <= RINGS; r++) {
                    float ringStrength = 1.0 - ((float)r / (float)RINGS); // [cite: 9]
                    float currentWidth = _GlowWidth * ((float)r / (float)RINGS); // [cite: 10]
                    
                    float ringMaxAlpha = 0;
                    for(int s = 0; s < SAMPLES; s++) {
                        float angle = s * (6.28 / (float)SAMPLES); // [cite: 11]
                        float2 off = float2(cos(angle), sin(angle)) * currentWidth * _MainTex_TexelSize.xy; // 
                        ringMaxAlpha = max(ringMaxAlpha, tex2D(_MainTex, i.uv + off).a); // 
                    }
                    distField = max(distField, ringMaxAlpha * ringStrength); // [cite: 13]
                }

                float glowMask = saturate(distField - mainAlpha); // [cite: 14]
                
                // Two-color interpolation based on the distance field
                float3 finalGlowCol = lerp(_OuterColor.rgb, _InnerColor.rgb, glowMask);
                
                float falloff = pow(glowMask, _GlowSoftness); // [cite: 15]

                float3 silhouette = float3(0,0,0) * mainAlpha; // [cite: 16]
                float3 glow = finalGlowCol * falloff * _GlowIntensity; // [cite: 17]
                float finalA = saturate(mainAlpha + (falloff * _GlowIntensity)); // [cite: 18]

                return fixed4(silhouette + glow, finalA); // [cite: 19]
            }
            ENDCG
        }
    }
}