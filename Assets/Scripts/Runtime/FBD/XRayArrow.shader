Shader "Custom/XRayArrow"
{
    Properties
    {
        _Color ("Main Color", Color) = (1,1,1,1)
        _XRayAlpha ("X-Ray Transparency", Range(0,1)) = 0.2
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+1" }

        // ==========================================
        // PASS 1: NORMAL ARROW (Outside the object)
        // ==========================================
        Pass
        {
            Name "Normal"
            // This tag forces URP to actually render this pass!
            Tags { "LightMode" = "UniversalForward" }

            ZWrite Off
            ZTest LEqual // Only draw when NOT blocked
            Cull Off     // Don't hide the back of the 2D arrow
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; };

            float4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return _Color; // Full solid color
            }
            ENDCG
        }

        // ==========================================
        // PASS 2: THE X-RAY (Shadow inside the object)
        // ==========================================
        Pass
        {
            Name "XRay"
            // This tag tricks URP into rendering the second pass!
            Tags { "LightMode" = "SRPDefaultUnlit" }

            ZWrite Off
            ZTest Greater // Only draw when blocked
            Cull Off      
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; };

            float4 _Color;
            float _XRayAlpha;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return float4(_Color.rgb, _XRayAlpha); // Transparent shadow
            }
            ENDCG
        }
    }
}