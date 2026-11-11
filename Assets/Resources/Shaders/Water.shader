// Pixel-art lake: samples a live render of everything above the waterline, mirrored, with
// row-by-row ripples (like the jam's 2DWater reflection), tinted deeper blue with depth.
Shader "SW/Water"
{
    Properties
    {
        _ReflTex ("Reflection", 2D) = "black" {}
        _Shallow ("Shallow", Color) = (0.26, 0.55, 0.72, 1)
        _Deep ("Deep", Color) = (0.12, 0.30, 0.52, 1)
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        ZWrite Off
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _ReflTex;
            float4 _ReflTex_TexelSize;
            float4 _Shallow, _Deep;
            float _WaterY, _ReflH, _CamLeft, _CamWidth, _T, _Calm, _Dread;

            struct v2f { float4 pos : SV_POSITION; float2 world : TEXCOORD0; };

            v2f vert(appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.world = mul(unity_ObjectToWorld, v.vertex).xy;
                return o;
            }

            float hash(float n) { return frac(sin(n * 12.9898) * 43758.5453); }

            fixed4 frag(v2f i) : SV_Target
            {
                // work in art pixels (16 per unit) so ripples stay on the pixel grid
                float row = floor((_WaterY - i.world.y) * 16.0);
                float d = saturate(row / (_ReflH * 16.0));
                float wave = sin(row * 0.85 + _T * 1.9) * 0.6 + sin(row * 0.31 - _T * 1.1) * 0.4;
                float drift = hash(row + floor(_T * 0.7 + row * 0.05)) - 0.5;
                float amp = (0.35 + d * 2.2) * (1.0 - _Calm * 0.85);
                float ox = (wave + drift * 0.8) * amp / 16.0;
                float u = (i.world.x + ox - _CamLeft) / _CamWidth;
                u = (floor(u * _ReflTex_TexelSize.z) + 0.5) * _ReflTex_TexelSize.x;
                float v = (row + 0.5) / (_ReflH * 16.0);
                fixed4 r = tex2D(_ReflTex, float2(u, saturate(v)));

                float3 water = lerp(_Shallow.rgb, _Deep.rgb, d);
                float3 col = lerp(r.rgb * 0.92, water, 0.30 + 0.32 * d);

                // glints: short bright dashes that drift along the rows
                float seg = floor((i.world.x * 16.0 + _T * (6.0 + hash(row) * 6.0)) / 9.0);
                float glint = step(0.975 - d * 0.02, hash(seg * 1.31 + row * 7.7)) * (1.0 - _Calm);
                col += glint * 0.10 * (1.0 - d * 0.5);

                col = lerp(col, col * float3(0.75, 0.55, 0.55), _Dread);
                float a = row < 1.0 ? 0.55 : lerp(0.8, 1.0, saturate(d * 5.0));
                return fixed4(col, a);
            }
            ENDCG
        }
    }
}
