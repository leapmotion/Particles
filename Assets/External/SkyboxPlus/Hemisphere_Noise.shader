Shader "SkyboxPlus/Hemisphere Noise"
{
    Properties
    {
        _NoiseArray("Noise", 2DArray) = "" {}
        _NoiseAmount("Noise Amount", Range(0, 1)) = 0.01
        [HDR] _TopColor("North Pole", Color) = (0.35, 0.37, 0.42)
        [HDR] _MiddleColor("Equator", Color) = (0.15, 0.15, 0.15)
        [HDR] _BottomColor("South Pole", Color) = (0.12, 0.13, 0.15)
        [Gamma] _Exposure("Exposure", Range(0, 8)) = 1
    }
    CGINCLUDE

    #include "UnityCG.cginc"

    half3 _TopColor;
    half3 _MiddleColor;
    half3 _BottomColor;
    half _Exposure;

    struct appdata_t {
        float4 vertex : POSITION;
    };

    struct v2f {
        float4 vertex : SV_POSITION;
        float3 texcoord : TEXCOORD0;
        float4 screenPos : TEXCOORD1;
    };

    v2f vert(appdata_t v)
    {
        v2f o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        o.texcoord = v.vertex.xyz;
        o.screenPos = ComputeScreenPos(o.vertex);
        return o;
    }

    sampler2D _Noise;
    float _NoiseAmount;
    UNITY_DECLARE_TEX2DARRAY(_NoiseArray);

    half4 frag(v2f i) : SV_Target
    {
        half t1 = max(+i.texcoord.y, 0);
        half t2 = max(-i.texcoord.y, 0);
        half3 c = lerp(lerp(_MiddleColor, _TopColor, t1), _BottomColor, t2);
        c *= _Exposure;

        float3 noiseUv;
        noiseUv.x = frac(i.screenPos.x / i.screenPos.w * _ScreenParams.x / 64);
        noiseUv.y = frac(i.screenPos.y / i.screenPos.w * _ScreenParams.y / 64);
        noiseUv.z = frac(_Time.y * 23.023) * 64;

        c.xyz += UNITY_SAMPLE_TEX2DARRAY(_NoiseArray, noiseUv).aaa * _NoiseAmount;

        return half4(c * _Exposure, 1);
    }

    ENDCG
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDCG
        }
    }
    Fallback Off
}
