Shader "Hidden/BloomEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" { }
    }
    
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always
        
        
        
        Pass //0
        {
            CGPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex: POSITION;
                float2 uv: TEXCOORD0;
            };
            
            struct v2f
            {
                float2 uv: TEXCOORD0;
                float4 vertex: SV_POSITION;
            };
            
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 _Filter;
            float _IterationsRatios;
            
            float3 Prefilter(float3 c)
            {
                half brightness = max(c.r, max(c.g, c.b));
                half soft = brightness - _Filter.y;
                soft = clamp(soft, 0, _Filter.z);
                soft = soft * soft * _Filter.w;
                half contribution = max(soft, brightness - _Filter.x);
                contribution /= max(brightness, 0.00001);
                return c * contribution;
            }
            
            float3 Sample(float2 uv)
            {
                return tex2D(_MainTex, uv).rgb;
            }
            
            float3 SampleBox(float2 uv, float delta)
            {
                float4 o = _MainTex_TexelSize.xyxy * float2(-delta * _IterationsRatios, delta * _IterationsRatios).xxyy;
                float3 s = Sample(uv + o.xy) +
                Sample(uv + o.zy) +
                Sample(uv + o.xw) +
                Sample(uv + o.zw);
                return s * 0.25;
            }
            
            float4 frag(v2f i): SV_Target
            {
                return float4(Prefilter(SampleBox(i.uv, 1)), 1);
            }
            ENDCG
            
        }
        
        Pass //1
        {
            CGPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex: POSITION;
                float2 uv: TEXCOORD0;
            };
            
            struct v2f
            {
                float2 uv: TEXCOORD0;
                float4 vertex: SV_POSITION;
            };
            
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _IterationsRatios;
            
            float3 Sample(float2 uv)
            {
                return tex2D(_MainTex, uv).rgb;
            }
            
            float3 SampleBox(float2 uv, float delta)
            {
                float4 o = _MainTex_TexelSize.xyxy * float2(-delta * _IterationsRatios, delta * _IterationsRatios).xxyy;
                float3 s = Sample(uv + o.xy) +
                Sample(uv + o.zy) +
                Sample(uv + o.xw) +
                Sample(uv + o.zw);
                return s * 0.25;
            }
            
            float4 frag(v2f i): SV_Target
            {
                return float4(SampleBox(i.uv, 0.5), 1);
            }
            ENDCG
            
        }
        
        Pass //2
        {
            Blend One One
            CGPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex: POSITION;
                float2 uv: TEXCOORD0;
            };
            
            struct v2f
            {
                float2 uv: TEXCOORD0;
                float4 vertex: SV_POSITION;
            };
            
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _IterationsRatios;
            
            float3 Sample(float2 uv)
            {
                return tex2D(_MainTex, uv).rgb;
            }
            
            float3 SampleBox(float2 uv, float delta)
            {
                float4 o = _MainTex_TexelSize.xyxy * float2(-delta * _IterationsRatios, delta * _IterationsRatios).xxyy;
                float3 s = Sample(uv + o.xy) +
                Sample(uv + o.zy) +
                Sample(uv + o.xw) +
                Sample(uv + o.zw);
                return s * 0.25;
            }
            
            float4 frag(v2f i): SV_Target
            {
                return float4(SampleBox(i.uv, 0.5), 1);
            }
            ENDCG
            
        }
        
        Pass //3
        {
            CGPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex: POSITION;
                float2 uv: TEXCOORD0;
            };
            
            struct v2f
            {
                float2 uv: TEXCOORD0;
                float4 vertex: SV_POSITION;
            };
            
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            sampler2D _MainTex, _SourceTex;
            float4 _MainTex_TexelSize;
            half _Intensity;
            float _IterationsRatios;
            
            float3 Sample(float2 uv)
            {
                return tex2D(_MainTex, uv).rgb;
            }
            
            float3 SampleBox(float2 uv, float delta)
            {
                float4 o = _MainTex_TexelSize.xyxy * float2(-delta * _IterationsRatios, delta * _IterationsRatios).xxyy;
                float3 s = Sample(uv + o.xy) +
                Sample(uv + o.zy) +
                Sample(uv + o.xw) +
                Sample(uv + o.zw);
                return s * 0.25;
            }
            
            float4 frag(v2f i): SV_Target
            {
                float4 c = tex2D(_SourceTex, i.uv);
                c.rgb += SampleBox(i.uv, 0.5) * _Intensity;
                return c;
            }
            ENDCG
            
        }
        
        Pass // 4
        {
            CGPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex: POSITION;
                float2 uv: TEXCOORD0;
            };
            
            struct v2f
            {
                float2 uv: TEXCOORD0;
                float4 vertex: SV_POSITION;
            };
            
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            half _Intensity;
            float _IterationsRatios;
            
            float3 Sample(float2 uv)
            {
                return tex2D(_MainTex, uv).rgb;
            }
            
            float3 SampleBox(float2 uv, float delta)
            {
                float4 o = _MainTex_TexelSize.xyxy * float2(-delta * _IterationsRatios, delta * _IterationsRatios).xxyy;
                float3 s = Sample(uv + o.xy) +
                Sample(uv + o.zy) +
                Sample(uv + o.xw) +
                Sample(uv + o.zw);
                return s * 0.25;
            }
            
            float4 frag(v2f i): SV_Target
            {
                return float4(_Intensity * SampleBox(i.uv, 0.5), 1);
            }
            ENDCG
            
        }
    }
}
