Shader "Unlit/PointSimShader" {
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
    _Force   ("Force", Float) = 0.01
	}

  CGINCLUDE
  #include "UnityCG.cginc"

  sampler2D_half _Positions;
  sampler2D_half _Velocities;
  half _Seed;
  half _Force;

  half4 _Targets[10];
  

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

  float nrand(half2 n) {
    return frac(sin(dot(n.xy, half2(12.9898, 78.233)))* 43758.5453);
  }

  half4 integrateVelocity(v2f i) : SV_Target {
    half4 velocity = tex2D(_Velocities, i.uv);
    half4 position = tex2D(_Positions, i.uv);

    velocity.xyz *= 0.999;

    for (uint j = 0; j < 2; j++) {
      half4 target = _Targets[j];
      half3 toTarget = target.xyz - position.xyz;
      velocity.xyz += _Force * toTarget / (0.1 + dot(toTarget, toTarget));
      velocity.xyz -= _Force * toTarget / (0.02 + 9 * dot(toTarget, toTarget) * dot(toTarget, toTarget));
    }

    velocity.x += 0.001 * (nrand(i.uv.xy * half2(2.123 * _Time.x, 4.1238 * _Time.y)) - 0.5);
    velocity.y += 0.001 * (nrand(i.uv.yx * half2(1.545 * _Time.y, 8.123 * _Time.x)) - 0.5);
    velocity.z += 0.001 * (nrand(i.uv.xx * half2(9.182 + _Time.x, 1.999 + _Time.y)) - 0.5);

    return velocity;
  }

  half4 integratePositions(v2f i) : SV_Target{
    half4 velocity = tex2D(_Velocities, i.uv);
    half4 position = tex2D(_Positions, i.uv);

    position += velocity * 0.01;
    //position.z = 0;
    return position;
  }

  float4 initPositions(v2f i) : SV_Target {
    float4 pos;
    pos.x = 0.3 * (nrand(i.uv + float2(2.123 + _Seed, 4.123 + _Seed)) - 0.5);
    pos.y = 0.3 * (nrand(i.uv + float2(1.545 + _Seed, 8.123 + _Seed)) - 0.5);
    pos.z = 0.3 * (nrand(i.uv + float2(9.182 + _Seed, 1.999 + _Seed)) - 0.5);
    pos.w = 0;
    return pos;
  }

  float4 initVelocities(v2f i) : SV_Target{
    return float4(0.3, 0.2, 0, 0);
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
