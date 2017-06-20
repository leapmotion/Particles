Shader "Custom/DisplayShader" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
    _Velocity ("Velocity", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(-2,2)) = 0.5
		_Metallic ("Metallic", Range(-2,2)) = 0.0
    _Size     ("Size", Range(0, 0.5)) = 0.01
    _TrailLength ("Trail Length", Range(0, 10000)) = 1000
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Standard vertex:vert noforwardadd
		#pragma target 2.0

		sampler2D _MainTex;
    sampler2D _Velocity;

		struct Input {
			float2 uv_MainTex;
      float4 color : COLOR;
      float3 viewDir;
		};

		half _Glossiness;
		half _Metallic;
    float4 _Colors[10];
    float _Size;
    float _TrailLength;

    float nrand(float2 n) {
      return frac(sin(dot(n.xy, float2(12.9898, 78.233)))* 43758.5453);
    }

    void vert(inout appdata_full v) {
      float4 particle = tex2Dlod(_MainTex, v.texcoord);
      float4 velocity = tex2Dlod(_Velocity, v.texcoord);
      velocity.xyz *= velocity.w;

      float dir = saturate(-dot(normalize(velocity.xyz), normalize(v.vertex.xyz)) - 0.2);
      v.vertex.xyz -= velocity.xyz * dir * _TrailLength;

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
