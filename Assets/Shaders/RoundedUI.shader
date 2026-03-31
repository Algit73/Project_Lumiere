// Solid-colour URP UI shader with SDF-based rounded corners.
// Assign to Image components. Add RoundedCorners.cs to auto-update _RectSize.
//
// _CornerRadius : radius in pixels  (0 = square, large = pill)
// _RectSize     : managed by RoundedCorners.cs — do not set manually

Shader "Custom/RoundedUI"
{
    Properties
    {
        [HideInInspector] _MainTex ("Texture", 2D) = "white" {}
        _Color        ("Tint",              Color)        = (1,1,1,1)
        _CornerRadius ("Corner Radius (px)", Range(0, 300)) = 10
        // Set automatically by RoundedCorners.cs
        _RectSize     ("Rect Size",          Vector)       = (200, 60, 0, 0)

        // Unity UI Mask (Mask component) — managed by Unity, do not touch
        [HideInInspector] _StencilComp      ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil          ("Stencil ID",         Float) = 0
        [HideInInspector] _StencilOp        ("Stencil Operation",  Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask  ("Stencil Read Mask",  Float) = 255
        [HideInInspector] _ColorMask        ("Color Mask",         Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Transparent"
            "RenderType"      = "Transparent"
            "RenderPipeline"  = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Stencil
        {
            Ref       [_Stencil]
            Comp      [_StencilComp]
            Pass      [_StencilOp]
            ReadMask  [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        ColorMask [_ColorMask]
        ZTest     [unity_GUIZTestMode]
        Blend     SrcAlpha OneMinusSrcAlpha
        ZWrite    Off
        Cull      Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float  _CornerRadius;
                float4 _RectSize;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;      // Image.color vertex tint
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color       = IN.color * _Color;
                return OUT;
            }

            // Signed distance from the surface of a rounded rectangle.
            // Returns < 0 inside, > 0 outside.
            float RoundedBoxSDF(float2 pos, float2 halfSize, float radius)
            {
                float2 q = abs(pos) - halfSize + radius;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - radius;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Pixel position centred on the rect origin
                float2 rectSize = _RectSize.xy;
                float2 pos      = (IN.uv - 0.5) * rectSize;
                float2 halfSize = rectSize * 0.5;
                float  radius   = clamp(_CornerRadius, 0.0, min(halfSize.x, halfSize.y));

                // 1-pixel anti-aliased edge
                float dist  = RoundedBoxSDF(pos, halfSize, radius);
                float alpha = 1.0 - smoothstep(-1.0, 1.0, dist);

                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * IN.color;
                col.a    *= alpha;
                return col;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
