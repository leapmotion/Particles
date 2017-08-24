﻿Shader "Unlit/PointDisplayShader"{
	Properties { 
    _MainTex ("Positions", 2D) = "" {}
    _Size    ("Size", Range(0, 0.1)) = 0.05
    _Bright  ("Brightness", Range(0, 0.1)) = 0.1
  }

	SubShader {
		Tags { "Queue"="Transparent" "RenderType"="Transparent" }
		LOD 100

    ZWrite Off
    Cull Off
    Blend One One

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
        float4 pos : SV_POSITION;
        float size : PSIZE;
      };

      sampler2D _MainTex;
      half _Size;
      half _Bright;

      v2f vert(uint id : SV_VertexID) {
        float4 uv;
        uv.x = cos(_Time.y + id * 0.0001) * id * 0.00001 + sin(_Time.y + id * 0.1) + 0.1 * sin(_Time.y + id * 0.12);
        uv.y = sin(_Time.y + id * 0.0001) * id * 0.00001 + cos(_Time.y + id * 0.1) + 0.1 * cos(_Time.y + id * 0.123);
        uv.z = 0;
        uv.w = 0;

        float4 position = uv;// tex2Dlod(_MainTex, uv);

        v2f o;
        o.pos = UnityObjectToClipPos(position);
        o.size = _Size;
        return o;
      }
			
			fixed4 frag (v2f i) : SV_Target {
        return fixed4(_Bright, _Bright, _Bright, 1);
			}
			ENDCG
		}
	}
}
