Shader "DungeonGuide/Map"
{
    Properties
    {
        [PerRendererData] _MainTex ("Terrain", 2D) = "white" {}
        _FogMask ("Fog and visits", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _VisitedColor ("Visited room", Vector) = (0.486,0.694,0.459,1)
        _ScannedColor ("Unvisited geometry", Vector) = (0.357,0.427,0.475,1)
        _RevealAll ("Reveal all", Float) = 0
        _LinearColorSpace ("Linear color space", Float) = 0
        _StencilComp ("Stencil comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil operation", Float) = 0
        _StencilWriteMask ("Stencil write mask", Float) = 255
        _StencilReadMask ("Stencil read mask", Float) = 255
        _ColorMask ("Color mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Alpha clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 vertex : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; float4 local : TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
            sampler2D _MainTex;
            sampler2D _FogMask;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            fixed4 _VisitedColor;
            fixed4 _ScannedColor;
            float4 _ClipRect;
            float _RevealAll;
            float _LinearColorSpace;
            v2f vert(appdata input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.local = input.vertex;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                output.color = input.color * _Color;
                return output;
            }
            float noise(int2 p) { return ((p.x * 73856093 ^ p.y * 19349663) & 255) / 255.0; }
            fixed3 cloud(float2 uv)
            {
                float2 pixel = floor(uv * _MainTex_TexelSize.zw);
                int2 grid = int2(floor(pixel / 64));
                float2 t = frac(pixel / 64);
                t = t * t * (3 - 2 * t);
                float n = lerp(lerp(noise(grid), noise(grid + int2(1,0)), t.x),
                    lerp(noise(grid + int2(0,1)), noise(grid + int2(1,1)), t.x), t.y);
                return floor(float3(72,78,82) + n * float3(30,33,35)) / 255;
            }
            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 terrain = tex2D(_MainTex, input.uv);
                fixed3 mask = tex2D(_FogMask, input.uv).rgb;
                terrain.rgb = lerp(terrain.rgb, LinearToGammaSpace(terrain.rgb), _LinearColorSpace);
                terrain.rgb = lerp(terrain.rgb, lerp(_ScannedColor.rgb, _VisitedColor.rgb, mask.g),
                    mask.b * max(_RevealAll, mask.g));
                terrain.rgb = lerp(terrain.rgb, cloud(input.uv), mask.r * (1 - _RevealAll));
                terrain.rgb = lerp(terrain.rgb, GammaToLinearSpace(terrain.rgb), _LinearColorSpace);
                terrain *= input.color;
                #ifdef UNITY_UI_CLIP_RECT
                terrain.a *= UnityGet2DClipping(input.local.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(terrain.a - 0.001);
                #endif
                return terrain;
            }
            ENDCG
        }
    }
}
