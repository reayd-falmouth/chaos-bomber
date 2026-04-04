Shader "Sprites/SilhouetteGoldGlow"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        [HDR] _GlowColor("Glow Color", Color) = (0.7, 0.7, 0.4, 1)
        _GlowWidthPixels("Glow Width", Range(0, 20)) = 5
        _GlowSoftness("Glow Softness", Range(1, 10)) = 3
        _GlowIntensity("Glow Intensity", Range(0, 10)) = 2
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
            float _GlowWidthPixels;
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
                float combinedAlpha = 0;
                
                // Sampling circle for the aura
                for(float a = 0; a < 6.28; a += 0.785) {
                    float2 off = float2(cos(a), sin(a)) * _GlowWidthPixels * _MainTex_TexelSize.xy;
                    combinedAlpha += tex2D(_MainTex, i.uv + off).a;
                }
                combinedAlpha /= 8.0;

                // Mask out the inner silhouette so only the outer glow remains
                float glowMask = saturate(combinedAlpha - mainAlpha);
                glowMask = pow(glowMask, _GlowSoftness);

                // Silhouette = Black (0,0,0), Glow = Your Gold Color
                float3 finalRGB = _GlowColor.rgb * glowMask * _GlowIntensity;
                float finalA = saturate(mainAlpha + (glowMask * _GlowIntensity));

                return fixed4(finalRGB, finalA);
            }
            ENDCG
        }
    }
}