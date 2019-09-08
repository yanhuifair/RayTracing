Shader "Hidden/FogEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" { }
    }
    
    CGINCLUDE
    #include "UnityCG.cginc"
    #include "LayerBlending.cginc"

    sampler2D _MainTex;
    half4 _MainTex_TexelSize;
    sampler2D_float _CameraDepthTexture;
    
    sampler2D _GradientTex;
    half4 _GradientTex_TexelSize;

    sampler2D _CurveTex;
    float _FogNear;
    float _FogFar;
    float _FarMul;
    
    float _FogLow;
    float _FogHeight;
    float _HeightMul;
    int _clipMode;
    int _useDisFog;
    int _useHeiFog;
    
    // for fast world space reconstruction
    uniform float4x4 _FrustumCornersWS;
    uniform float4 _CameraWS;
    
    struct appdata
    {
        float4 vertex: POSITION;
        half2 texcoord: TEXCOORD0;
    };
    
    struct v2f
    {
        float4 pos: SV_POSITION;
        float2 uv: TEXCOORD0;
        float2 uv_depth: TEXCOORD1;
        float4 interpolatedRay: TEXCOORD2;
        float3 worldPos: TEXCOORD3;
    };
    
    v2f vert(appdata v)
    {
        v2f o;
        v.vertex.z = 0.1;
        o.pos = UnityObjectToClipPos(v.vertex);
        o.uv = v.texcoord.xy;
        o.uv_depth = v.texcoord.xy;
        
        #if UNITY_UV_STARTS_AT_TOP
            if (_MainTex_TexelSize.y < 0)
                o.uv.y = 1 - o.uv.y;
        #endif
        
        int frustumIndex = v.texcoord.x + (2 * o.uv.y);
        o.interpolatedRay = _FrustumCornersWS[frustumIndex];
        o.interpolatedRay.w = frustumIndex;
        
        o.worldPos.xyz = mul(unity_ObjectToWorld, v.vertex);
        return o;
    }
    
    
    
    float ComputeDistance(float depth, float4 wsDir)
    {
        float dis = 0;
        if(_clipMode == 1)
        {
            dis = length(wsDir.xyz);
        }
        
        if(_clipMode == 2)
        {
            dis = depth * _ProjectionParams.z;
            dis -= _ProjectionParams.y;
        }
        
        float g = 0;
        float d = dis;
        if(_FogNear != _FogFar)
            g += ((d - _FogNear) / (_FogFar - _FogNear)) ;
        
        
        return saturate(g * _FarMul);
    }
    
    float ComputeHeight(float4 wsDir)
    {
        float4 wsPos = _CameraWS + wsDir;
        float g = 0;
        float h = wsPos.y;
        if(_FogHeight != _FogLow)
            g += ((h - _FogLow) / (_FogHeight - _FogLow));
        
        return saturate(g) * _HeightMul;
    }
    
    float ComputeFogFactor(float fac)
    {
        return tex2D(_CurveTex, float2(fac, 0)).a;
    }

    float _time;
    float _dpi;
    float _denoise;
    float Noise(v2f i)
    {
        float niose = frac(sin(dot(i.uv, float2(12.9898, 78.233))) * 178.54534 * _dpi + (_Time.y * _time / 1000) % 10);
        return niose;
    }
    
    half4 ComputeFog(v2f i): SV_Target
    {
        
        float rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, UnityStereoTransformScreenSpaceTex(i.uv_depth));
        float dpth01 = Linear01Depth(rawDepth);
        float4 wsDir = dpth01 * i.interpolatedRay;
        
        float g = 0;
        g += ComputeDistance(dpth01, wsDir) * _useDisFog ;
        g += ComputeHeight(wsDir) * _useHeiFog ;

        float fogFactor = ComputeFogFactor(g);
        //add noise
        fogFactor += ((Noise(i) * 2) - 0.5) * _GradientTex_TexelSize.x * _denoise;
        fogFactor = saturate(fogFactor);

        //lerp
        float4 fogColor = tex2D(_GradientTex, float2(fogFactor, 0));
        float4 sceneColor = tex2D(_MainTex, UnityStereoTransformScreenSpaceTex(i.uv));
        float4 final = lerp(sceneColor + fogColor * fogColor.a, sceneColor * (1 - fogColor.a) + fogColor * fogColor.a, fogFactor);
        return final;
    }
    ENDCG
    
    SubShader
    {
        // No culling or depth
        Cull Off
        ZWrite On
        ZTest Always
        Fog
        {
            Mode Off
        }
        
        Pass
        {
            CGPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag
            fixed4 frag(v2f i): SV_Target
            {
                return ComputeFog(i);
            }
            ENDCG
            
        }
    }
}
