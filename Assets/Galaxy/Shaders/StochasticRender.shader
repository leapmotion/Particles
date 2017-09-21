Shader "Unlit/StochasticRender" {
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
    _Noise   ("Noise", 2D) = "white" {}
    _NoiseOffset("Noise Offset", Int) = 0
	}
	SubShader {
    Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" }
		LOD 100

    ZWrite Off
    Cull Off
    Blend One One

		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct v2f {
				float4 vertex : SV_POSITION;
        float4 color : COLOR;
			};
			
      sampler2D _MainTex;

      float _Scale;
      float _Bright;
      float _Size;
      uint _NoiseOffset;
			
			v2f vert (uint id : SV_VertexID) {
        float4 uv;
        uv.x = (id / 512) / 512.0;
        uv.y = (id % 512) / 512.0;
        uv.z = 0;
        uv.w = 0;

        float4 position = tex2Dlod(_MainTex, uv);
        float4 worldPosition = mul(unity_ObjectToWorld, position) * _Scale;

        uint id2 = (id + _NoiseOffset) % (32 * 32);
        uv.x = (id2 / 32) / 32.0;
        uv.y = (id2 % 32) / 32.0;
        uv.z = 0;
        uv.w = 0;
        worldPosition += tex2Dlod(_MainTex, uv) * _Size;

        float distToCamera = length(_WorldSpaceCameraPos - worldPosition);

        float screenSpaceWidth = 2000 * _Bright * _Size / distToCamera;
        float brightness = screenSpaceWidth * screenSpaceWidth;

				v2f o;
        o.vertex = mul(UNITY_MATRIX_VP, worldPosition); 
        o.color = brightness;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target {
        return fixed4(i.color);
			}
			ENDCG
		}
	}
}
