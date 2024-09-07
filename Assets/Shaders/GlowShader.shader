Shader "Unlit/GlowShader" {
    Properties {
        _Color ("Main Color", Color) = (1,1,1,1)
        _EmissionColor ("Emission Color", Color) = (1,1,1,1)
    }
    SubShader {
        Tags { "Queue" = "Transparent" }
        LOD 200
        Blend SrcAlpha One
        ZWrite Off
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata_t {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };
            struct v2f {
                float4 pos : SV_POSITION;
                float3 normal : TEXCOORD0;
            };
            fixed4 _EmissionColor;
            v2f vert (appdata_t v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.normal = v.normal;
                return o;
            }
            fixed4 frag (v2f i) : SV_Target {
                return _EmissionColor;
            }
            ENDCG
        }
    }
}
