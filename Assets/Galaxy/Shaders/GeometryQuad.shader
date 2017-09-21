Shader "Unlit/QuadDisplayShader"{
	Properties { 
    _MainTex ("Positions", 2D) = "" {}
    _Particle("Particle", 2D) = "white" {}
    _Size    ("Size", Range(0, 0.1)) = 0.05
    _Bright  ("Brightness", Float) = 0.1
  }


	SubShader {
		Tags { "Queue"="Transparent" "RenderType"="Transparent" }
		LOD 100

    Cull Off
    Blend SrcAlpha OneMinusSrcAlpha
    ZWrite Off

		Pass {
			CGPROGRAM
      #pragma target 5.0
			#pragma vertex vert
      #pragma geometry geom
			#pragma fragment frag

			#include "UnityCG.cginc"

      struct appdata {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
      };

      struct v2g {
        float4 pos : POSITION;
        float3 toCam : TEXCOORD1;
      };

			struct g2f {
        float4 pos : SV_POSITION;
        float4 uv : TEXCOORD0;
			};

      sampler2D _MainTex;
      sampler2D _Particle;
      half _Size;
      half _Bright;
      float _Scale;

      v2g vert(uint id : SV_VertexID) {
        float4 uv;
        uv.x = (id / 512) / 512.0;
        uv.y = (id % 512) / 512.0;
        uv.z = 0;
        uv.w = 0;

        float4 position = tex2Dlod(_MainTex, uv);
        float3 worldPos = mul(unity_ObjectToWorld, position) * _Scale;

        v2g o;
        o.toCam = normalize(_WorldSpaceCameraPos - worldPos);
        o.pos = float4(worldPos, 1);
        return o;
      }

      [maxvertexcount(4)]
      void geom(point v2g input[1], inout TriangleStream<g2f> triStream) {
        g2f o;

        float3 toCam = input[0].toCam.xyz;
        float3 up = float3(0, 1, 0);

        float4 quadRight = float4(normalize(cross(toCam, up)) * _Size * _Scale, 0);
        float4 quadUp = float4(normalize(cross(toCam, quadRight)) * _Size * _Scale, 0);

        o.pos = mul(UNITY_MATRIX_VP, input[0].pos + quadRight - quadUp);
        o.uv = float4(1, 0, 0, 0);
        triStream.Append(o);

        o.pos = mul(UNITY_MATRIX_VP, input[0].pos - quadRight - quadUp);
        o.uv = float4(0, 0, 0, 0);
        triStream.Append(o);

        o.pos = mul(UNITY_MATRIX_VP, input[0].pos + quadRight + quadUp);
        o.uv = float4(1, 1, 0, 0);
        triStream.Append(o);

        o.pos = mul(UNITY_MATRIX_VP, input[0].pos - quadRight + quadUp);
        o.uv = float4(0, 1, 0, 0);
        triStream.Append(o);
      }
			
			fixed4 frag (g2f i) : SV_Target {
        float4 color = tex2D(_Particle, i.uv);
        color.w *= _Bright;
        return float4(1, 1, 1, color.w);
			}
			ENDCG
		}
	}
}
