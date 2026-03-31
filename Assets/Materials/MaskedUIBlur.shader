// URP-compatible replacement for the legacy GrabPass blur.
// GrabPass is not supported in URP — this version uses _CameraOpaqueTexture
// (requires "Opaque Texture" enabled in the URP Renderer Asset) and performs
// a single-pass separable Gaussian blur in the fragment shader.
//
// URP Renderer setup required:
//   Edit > Project Settings > Graphics > URP Asset > Renderer > Opaque Texture = ON
//   (or toggle in the active URP Asset Inspector)

Shader "Custom/MaskedUIBlur"
{
    Properties
    {
        _Size          ("Blur Radius",        Range(0, 30))  = 1
        [HideInInspector] _MainTex ("Masking Texture", 2D)  = "white" {}
        _AdditiveColor ("Additive Tint",      Color)        = (0, 0, 0, 0)
        _MultiplyColor ("Multiply Tint",      Color)        = (1, 1, 1, 1)

        // Rounded corners — auto-updated by RoundedCorners.cs
        _CornerRadius  ("Corner Radius (px)",  Range(0, 300)) = 0
        _RectSize      ("Rect Size (auto)",    Vector)        = (560, 500, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "Transparent"
            "Queue"           = "Transparent"
            "RenderPipeline"  = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "UIBlur"

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // _CameraOpaqueTexture is the URP equivalent of GrabPass.
            // Enable "Opaque Texture" in your URP Renderer Asset to use it.
            TEXTURE2D(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);
            float4 _CameraOpaqueTexture_TexelSize;

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float  _Size;
                float4 _AdditiveColor;
                float4 _MultiplyColor;
                float  _CornerRadius;
                float4 _RectSize;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uvScreen    : TEXCOORD0;   // screen-space UV for camera texture
                float2 uvMask      : TEXCOORD1;   // object UV for masking texture
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);

                // Convert clip position to [0,1] screen UV
                float2 screenPos = OUT.positionHCS.xy / OUT.positionHCS.w;
                OUT.uvScreen     = screenPos * 0.5 + 0.5;
                #if UNITY_UV_STARTS_AT_TOP
                OUT.uvScreen.y   = 1.0 - OUT.uvScreen.y;
                #endif

                OUT.uvMask = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            // 9-tap Gaussian weights
            static const float weights[9] = { 0.05, 0.09, 0.12, 0.15, 0.18, 0.15, 0.12, 0.09, 0.05 };
            static const float offsets[9] = { -4,   -3,   -2,   -1,    0,    1,    2,    3,    4   };

            // SDF for a rounded rectangle (same as RoundedUI.shader)
            float RoundedBoxSDF(float2 pos, float2 halfSize, float radius)
            {
                float2 q = abs(pos) - halfSize + radius;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - radius;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 ts = _CameraOpaqueTexture_TexelSize.xy * _Size;

                // Horizontal pass
                half4 hSum = (half4)0;
                for (int i = 0; i < 9; i++)
                    hSum += SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture,
                                IN.uvScreen + float2(ts.x * offsets[i], 0)) * weights[i];

                // Vertical pass (on the already-blurred horizontal result approximation)
                half4 sum = (half4)0;
                for (int j = 0; j < 9; j++)
                    sum += SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture,
                               IN.uvScreen + float2(0, ts.y * offsets[j])) * weights[j];

                half4 blurred = (hSum + sum) * 0.5;

                half4 result;
                result.rgb = blurred.rgb * _MultiplyColor.rgb + _AdditiveColor.rgb;
                result.a   = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uvMask).a;

                // Rounded corner clipping via SDF (_CornerRadius = 0 skips this)
                if (_CornerRadius > 0.0)
                {
                    float2 pos      = (IN.uvMask - 0.5) * _RectSize.xy;
                    float2 halfSize = _RectSize.xy * 0.5;
                    float  radius   = min(_CornerRadius, min(halfSize.x, halfSize.y));
                    result.a       *= 1.0 - smoothstep(-1.0, 1.0, RoundedBoxSDF(pos, halfSize, radius));
                }

                return result;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}