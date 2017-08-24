Shader "Unlit/QuadDisplayShader"{
	Properties { 
    _MainTex ("Positions", 2D) = "" {}
    _Size    ("Size", Range(0, 0.1)) = 0.05
  }


	SubShader {
		Tags { "Queue"="Transparent" "RenderType"="Transparent" }
		LOD 100

    Cull Off
    Blend One One

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
        float4 pos : SV_POSITION;
      };

			struct g2f {
        float4 pos : SV_POSITION;
			};

      sampler2D _MainTex;
      half _Size;

      v2g vert(uint id : SV_VertexID) {
        float4 uv;
        uv.x = (id / 1024) / 1024.0;
        uv.y = (id % 1024) / 1024.0;
        uv.z = 0;
        uv.w = 0;

        float4 position = 0;// tex2Dlod(_MainTex, uv);

        v2g o;
        o.pos = UnityObjectToClipPos(position);
        return o;
      }

      [maxvertexcount(4)]
      void geom(point v2g input[1], inout TriangleStream<g2f> triStream) {
        g2f o;
        o.pos = input[0].pos + float4(_Size, -_Size, 0, 0);
        triStream.Append(o);

        o.pos = input[0].pos + float4(-_Size, -_Size, 0, 0);
        triStream.Append(o);

        o.pos = input[0].pos + float4(_Size, _Size, 0, 0);
        triStream.Append(o);

        o.pos = input[0].pos + float4(-_Size, _Size, 0, 0);
        triStream.Append(o);
      }
			
			fixed4 frag (g2f i) : SV_Target {
        return fixed4(0.01, 0.01, 0.01, 1);
			}
			ENDCG
		}
	}
}
