Shader "Custom/DisplayShader" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
    _Velocity ("Velocity", 2D) = "white" {}
    _ToonRamp ("Toon Ramp", 2D) = "white" {}

		_Glossiness ("Smoothness", Range(-2,2)) = 0.5
		_Metallic ("Metallic", Range(-2,2)) = 0.0
    _Size     ("Size", Range(0, 0.5)) = 0.01
    _TrailLength ("Trail Length", Range(0, 10000)) = 1000
    _Brightness ("Brightness", Float) = 1
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
    #pragma multi_compile COLOR_SPECIES COLOR_SPECIES_MAGNITUDE COLOR_VELOCITY
		#pragma surface surf CelShadingForward vertex:vert noforwardadd
		#pragma target 2.0

		sampler2D _MainTex;
    sampler2D _Velocity;
    sampler2D _ToonRamp;

		struct Input {
			float2 uv_MainTex;
      float4 color : COLOR;
      float3 viewDir;
		};

		half _Glossiness;
		half _Metallic;
    float4 _Colors[32];
    float _Size;
    float _TrailLength;
    float _Brightness;

    float nrand(float2 n) {
      return frac(sin(dot(n.xy, float2(12.9898, 78.233)))* 43758.5453);
    }

    half4 LightingCelShadingForward(SurfaceOutput  s, half3 lightDir, half atten) {
      half NdotL = dot(s.Normal, lightDir);

      NdotL = tex2D(_ToonRamp, float2(NdotL * 0.5 + 0.5, 0));

      half4 c;
      c.rgb = s.Albedo * _LightColor0.rgb * (NdotL * atten * 2);
      c.a = s.Alpha;
      return c;
    }

    void vert(inout appdata_full v) {
      float4 particle = tex2Dlod(_MainTex, v.texcoord);
      float4 velocity = tex2Dlod(_Velocity, v.texcoord);
      velocity.xyz *= velocity.w;

      float dir = saturate(-dot(normalize(velocity.xyz), normalize(v.vertex.xyz)) - 0.2);
      v.vertex.xyz -= velocity.xyz * dir * _TrailLength;

      v.vertex.xyz *= _Size;
      v.vertex.xyz += particle.xyz;

#ifdef COLOR_SPECIES
      v.color = _Colors[(int)particle.w];
#endif

#ifdef COLOR_VELOCITY
      v.color.rgb = abs(velocity.xyz) * _Brightness;
#endif

#ifdef COLOR_SPECIES_MAGNITUDE
      v.color = _Colors[(int)particle.w] * length(velocity.xyz) * _Brightness;
#endif
    }

		void surf (Input IN, inout SurfaceOutput  o) {
      o.Albedo = IN.color.rgb;
      //o.Normal = IN.viewDir;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
