Shader "MieMieUIFrameWork/FloatingTextInstanced"
{
    Properties
    {
        _MainTex ("Atlas", 2D) = "white" {}
        _GlyphWidth ("Glyph Width", Float) = 0.35
        _GlyphHeight ("Glyph Height", Float) = 0.45
        _AtlasColumns ("Atlas Columns", Float) = 16
        _AtlasRows ("Atlas Rows", Float) = 3
        _FadePower ("Fade Power", Float) = 2
        _CritPopStart ("Crit Pop Start", Float) = 1.2
        _CritPopEnd ("Crit Pop End", Float) = 1
        _CritPopInvDuration ("Crit Pop Inv Duration", Float) = 4
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "DisableBatching" = "True"
        }

        Cull Off
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _GlyphWidth;
            float _GlyphHeight;
            float _AtlasColumns;
            float _AtlasRows;
            float _FadePower;
            float _CritPopStart;
            float _CritPopEnd;
            float _CritPopInvDuration;

            UNITY_INSTANCING_BUFFER_START(PerInstance)
                UNITY_DEFINE_INSTANCED_PROP(float4, _OriginTime)
                UNITY_DEFINE_INSTANCED_PROP(float4, _VelocityLife)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Parms)
                UNITY_DEFINE_INSTANCED_PROP(float4, _GlyphColor)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Indices0)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Indices1)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Indices2)
            UNITY_INSTANCING_BUFFER_END(PerInstance)

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 uv2 : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float FetchAtlasIndex(float slot, float4 i0, float4 i1, float4 i2)
            {
                int s = (int)slot;
                if (s == 0) return i0.x;
                if (s == 1) return i0.y;
                if (s == 2) return i0.z;
                if (s == 3) return i0.w;
                if (s == 4) return i1.x;
                if (s == 5) return i1.y;
                if (s == 6) return i1.z;
                if (s == 7) return i1.w;
                return i2.x;
            }

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                float4 originTime = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _OriginTime);
                float4 velocityLife = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _VelocityLife);
                float4 parms = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _Parms);
                float4 glyphColor = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _GlyphColor);
                float4 indices0 = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _Indices0);
                float4 indices1 = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _Indices1);
                float4 indices2 = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _Indices2);

                float glyphCount = parms.x;
                float glyphSlot = v.uv2.x;
                float2 corner = v.vertex.xy;

                if (glyphSlot >= glyphCount)
                {
                    o.pos = float4(0, 0, 0, 0);
                    o.uv = 0;
                    o.color = 0;
                    return o;
                }

                float age = _Time.y - originTime.w;
                float life = max(velocityLife.w, 0.0001);
                float t = saturate(age / life);

                float3 worldCenter = originTime.xyz + velocityLife.xyz * age;
                float scale = parms.y;
                if (parms.w > 0.5)
                {
                    float popT = saturate(t * _CritPopInvDuration);
                    scale *= lerp(_CritPopStart, _CritPopEnd, popT);
                }

                float step = _GlyphWidth * scale;
                float startX = -0.5 * (glyphCount - 1.0) * step;
                float localX = startX + glyphSlot * step;

                float3 camR = UNITY_MATRIX_V[0].xyz;
                float3 camU = UNITY_MATRIX_V[1].xyz;
                float3 worldPos = worldCenter
                    + camR * (localX + corner.x * _GlyphWidth * scale)
                    + camU * (corner.y * _GlyphHeight * scale);

                o.pos = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0));

                float idx = FetchAtlasIndex(glyphSlot, indices0, indices1, indices2);
                float cols = max(_AtlasColumns, 1.0);
                float rows = max(_AtlasRows, 1.0);
                float col = fmod(idx, cols);
                float row = floor(idx / cols);
                float uSize = 1.0 / cols;
                float vSize = 1.0 / rows;
                float padU = uSize * 0.02;
                float padV = vSize * 0.02;
                float u0 = col * uSize + padU;
                float u1 = (col + 1.0) * uSize - padU;
                float v0 = 1.0 - (row + 1.0) * vSize + padV;
                float v1 = 1.0 - row * vSize - padV;
                o.uv = float2(lerp(u0, u1, v.uv.x), lerp(v0, v1, v.uv.y));

                float fadePower = max(_FadePower, 0.01);
                float alpha = 1.0 - pow(t, fadePower);
                o.color = glyphColor;
                o.color.a *= alpha;
                if (t >= 1.0)
                {
                    o.color.a = 0.0;
                }

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                fixed4 tex = tex2D(_MainTex, i.uv);
                fixed4 col = tex * i.color;
                clip(col.a - 0.01);
                return col;
            }
            ENDCG
        }
    }
}
