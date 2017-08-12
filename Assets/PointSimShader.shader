Shader "Unlit/PointSimShader" {
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
    _Force   ("Force", Float) = 0.01
	}

  CGINCLUDE
  #include "UnityCG.cginc"

  sampler2D _Positions;
  sampler2D _Velocities;
  half _Seed;
  half _Force;

  half4 _Target0;
  half4 _Target1;
  

  struct appdata {
    float4 vertex : POSITION;
    float4 uv : TEXCOORD0;
  };

  struct v2f {
    float4 uv : TEXCOORD0;
    float4 vertex : SV_Position;
  };

  v2f vert(appdata v) {
    v2f o;
    o.vertex = UnityObjectToClipPos(v.vertex);
    o.uv = v.uv;
    return o;
  }

  half nrand(half2 n) {
    return frac(sin(dot(n.xy, float2(12.9898, 78.233)))* 43758.5453);
  }

  half4 integrateVelocity(v2f i) : SV_Target {
    half4 velocity = tex2D(_Velocities, i.uv);
    half4 position = tex2D(_Positions, i.uv);

    velocity.xyz *= 0.999;

    float3 toTarget0 = _Target0.xyz - position.xyz;
    velocity.xyz += _Force * toTarget0 / (0.1 + dot(toTarget0, toTarget0));

    float3 toTarget1 = _Target1.xyz - position.xyz;
    velocity.xyz += _Force * toTarget1 / (0.1 + dot(toTarget1, toTarget1));

    return velocity;
  }

  half4 integratePositions(v2f i) : SV_Target{
    half4 velocity = tex2D(_Velocities, i.uv);
    half4 position = tex2D(_Positions, i.uv);

    position += velocity * 0.01;
    return position;
  }

  half4 initPositions(v2f i) : SV_Target {
    half4 pos;
    pos.x = 0.3 * (nrand(i.uv + float2(2.123 + _Seed, 4.123 + _Seed)) - 0.5);
    pos.y = 0.3 * (nrand(i.uv + float2(1.545 + _Seed, 8.123 + _Seed)) - 0.5);
    pos.z = 0.3 * (nrand(i.uv + float2(9.182 + _Seed, 1.999 + _Seed)) - 0.5);
    pos.w = 0;
    return pos;
  }

  half4 initVelocities(v2f i) : SV_Target{
    return half4(0, 0, 0, 0);
  }

  ENDCG

	SubShader {
		Tags { "RenderType"="Opaque" }
    LOD 100
    Cull Off
    ZTest Off
    ZWrite Off
    Blend One Zero

    //Pass 0: integrate velocities
		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment integrateVelocity
      ENDCG
		}

    //Pass 1: integrate velocities
		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment integratePositions
      ENDCG
		}

    //Pass 2: integrate velocities
		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment initPositions
      ENDCG
		}

    //Pass 3: integrate velocities
		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment initVelocities
      ENDCG
		}
	}
}
