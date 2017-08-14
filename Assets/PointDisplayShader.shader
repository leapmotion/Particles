Shader "Unlit/PointDisplayShader"{
	Properties { 
    _MainTex ("Positions", 2D) = "" {}
  }
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct v2f {
				float4 vertex : SV_POSITION;
			};

      sampler2D _MainTex;
			
			v2f vert (uint i : SV_VertexID) {
				v2f o;

        uint dx = i % 256;
        uint dy = i / 512;

        float4 uv;
        uv.x = dx / 256.0;
        uv.y = dy / 512.0;
        uv.z = 0;
        uv.w = 0;

        half4 position = tex2Dlod(_MainTex, uv);

				o.vertex = UnityObjectToClipPos(position);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target {
				return fixed4(1, 1, 1, 1);
			}
			ENDCG
		}
	}
}
