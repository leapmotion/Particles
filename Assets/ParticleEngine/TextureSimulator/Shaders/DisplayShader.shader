Shader "Custom/DisplayShader" {
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
		#pragma target 3.0

		sampler2D _MainTex;

		struct Input {
			float2 uv_MainTex;
		};

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;

    float nrand(float2 n) {
      return frac(sin(dot(n.xy, float2(12.9898, 78.233)))* 43758.5453);
    }

    void vert(inout appdata_full v) {
      v.vertex.xyz += tex2Dlod(_MainTex, v.texcoord);
    }

		void surf (Input IN, inout SurfaceOutputStandard o) {
      //float r = nrand(IN.uv_MainTex);
      //float g = nrand(IN.uv_MainTex * 2);
      //float b = nrand(IN.uv_MainTex * 3);

      float r = frac(5.1237234872 * IN.uv_MainTex.x);
      float g = IN.uv_MainTex.y;
      float b = 0;


      o.Albedo = float3(r, g, b);
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
