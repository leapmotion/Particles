// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Unlit/PointDisplayShader"{
	Properties { 
    _MainTex ("Positions", 2D) = "" {}
    _Velocity ("Velocity", 2D) = "" {}
    _Size    ("Size", Range(0, 10)) = 0.05
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
        float3 color : COLOR;
      };

      sampler2D _MainTex;
      sampler2D _Velocity;
      half _Size;
      half _Bright;
      float _Scale;

      v2f vert(uint id : SV_VertexID) {
        float4 uv;
        uv.x = (id / 512) / 512.0;
        uv.y = (id % 512) / 512.0;
        //uv.x = cos(_Time.y + id * 0.0001) * id * 0.00001 + sin(_Time.y + id * 0.1) + 0.1 * sin(_Time.y + id * 0.12);
        //uv.y = sin(_Time.y + id * 0.0001) * id * 0.00001 + cos(_Time.y + id * 0.1) + 0.1 * cos(_Time.y + id * 0.123);
        uv.z = 0;
        uv.w = 0;

        float4 position = tex2Dlod(_MainTex, uv);
        float4 velocity = tex2Dlod(_Velocity, uv);

        float4 worldPos = mul(unity_ObjectToWorld, position) * _Scale;
        float distToCamera = length(_WorldSpaceCameraPos - worldPos);

        v2f o;
        o.pos = UnityObjectToClipPos(position);
        o.size = _Size / distToCamera;
        o.color = _Bright;
        return o;
      }
			
			fixed4 frag (v2f i) : SV_Target {
        return fixed4(i.color, 1);
			}
			ENDCG
		}
	}
}
