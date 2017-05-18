Shader "Custom/ParticleDisplay" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Standard fullforwardshadows vertex:vert
		#pragma target 5.0
    #include "Assets/Shaders/ParticleData.cginc"

#ifdef SHADER_API_D3D11
    StructuredBuffer<Particle> _Particles;
#endif

    struct appdata {
      uint inst : SV_InstanceID;

      float4 vertex : POSITION;
      float3 normal : NORMAL;
      float4 texcoord : TEXCOORD0;
      float4 texcoord1 : TEXCOORD1;
      float4 texcoord2 : TEXCOORD2;
    };

		struct Input {
			float2 uv_MainTex;
		};

    void vert(inout appdata v, out Input o) {
      UNITY_INITIALIZE_OUTPUT(Input, o);

      v.vertex.xyz += _Particles[v.inst].position;
    }

		void surf (Input IN, inout SurfaceOutputStandard o) {
			o.Albedo = float3(1,1,1);
			o.Metallic = 0;
			o.Smoothness = 0;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
