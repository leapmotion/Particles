Shader "Galaxy/ExpandEffect" {
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader {
		Cull Off ZWrite Off ZTest Always

		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
        float2 uv[9] : TEXCOORD0;
			};

      sampler2D _MainTex;
      float4 _MainTex_TexelSize;

			v2f vert (appdata v) {
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);

        float2 dx = float2(_MainTex_TexelSize.x, 0);
        float2 dy = float2(0, _MainTex_TexelSize.y);

        o.uv[0] = v.uv;

        o.uv[1] = v.uv + dx;
        o.uv[2] = v.uv - dx;
        o.uv[3] = v.uv + dy;
        o.uv[4] = v.uv - dy;

        o.uv[5] = v.uv + dx + dy;
        o.uv[6] = v.uv - dx + dy;
        o.uv[7] = v.uv + dx - dy;
        o.uv[8] = v.uv - dx - dy;

				return o;
			}

			fixed4 frag (v2f i) : SV_Target {
        fixed4 col = tex2D(_MainTex, i.uv[0]);

        col = max(col, saturate(tex2D(_MainTex, i.uv[1])) * 0.75);
        col = max(col, saturate(tex2D(_MainTex, i.uv[2])) * 0.75);
        col = max(col, saturate(tex2D(_MainTex, i.uv[3])) * 0.75);
        col = max(col, saturate(tex2D(_MainTex, i.uv[4])) * 0.75);

        col = max(col, saturate(tex2D(_MainTex, i.uv[5])) * 0.5);
        col = max(col, saturate(tex2D(_MainTex, i.uv[6])) * 0.5);
        col = max(col, saturate(tex2D(_MainTex, i.uv[7])) * 0.5);
        col = max(col, saturate(tex2D(_MainTex, i.uv[8])) * 0.5);

				return col;
			}
			ENDCG
		}
	}
}
