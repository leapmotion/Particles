Shader "Unlit/GammaBlit" {
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
    _Pow     ("Power", Float) = 1
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 100

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
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
      half _Pow;
			
			v2f vert (appdata v) {
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
        o.uv = v.uv;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target {
        fixed4 col = pow(tex2D(_MainTex, i.uv), _Pow);
				return col;
			}
			ENDCG
		}
	}
}
