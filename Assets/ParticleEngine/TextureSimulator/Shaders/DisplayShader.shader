Shader "Custom/DisplayShader" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(-2,2)) = 0.5
		_Metallic ("Metallic", Range(-2,2)) = 0.0
    _Size     ("Size", Range(0, 0.02)) = 0.01
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Standard fullforwardshadows vertex:vert addshadow
		#pragma target 3.0

		sampler2D _MainTex;

		struct Input {
			float2 uv_MainTex;
      float4 color : COLOR;
      float3 viewDir;
		};

		half _Glossiness;
		half _Metallic;
    float4 _Colors[10];
    float _Size;

    float nrand(float2 n) {
      return frac(sin(dot(n.xy, float2(12.9898, 78.233)))* 43758.5453);
    }

    void vert(inout appdata_full v) {
      float4 particle = tex2Dlod(_MainTex, v.texcoord);
      v.vertex.xyz *= _Size;
      v.vertex.xyz += particle.xyz;
      v.color = _Colors[(int)particle.w];
    }

		void surf (Input IN, inout SurfaceOutputStandard o) {
      o.Albedo = IN.color.rgb;
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
      o.Normal = IN.viewDir;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
