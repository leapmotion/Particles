Shader "Unlit/PointSimShader" {
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
    _Noise   ("Noise", 2D) = "white" {}
    _Force   ("Force", Float) = 0.01
    _Timestep("Timestep", Float) = 1
	}

  CGINCLUDE
  #include "UnityCG.cginc"

  sampler2D_float _Positions;
  sampler2D_float _Velocities;

  sampler2D_float _Noise;

  float _Force;
  float _Timestep;

  float4x4 _PlanetRotations[10];
  float4 _Planets[10];
  int _PlanetCount;

  float _MinDiscRadius;
  float _MaxDiscRadius;
  float _MaxDiscHeight;
  

  struct appdata {
    float4 vertex : POSITION;
    float4 uv : TEXCOORD0;
  };

  struct v2f {
    float4 uv : TEXCOORD0;
    float4 vertex : SV_Position;
  };

  struct fragOut {
    float4 dest0 : SV_Target0;
    float4 dest1 : SV_Target1;
  };

  v2f vert(appdata v) {
    v2f o;
    o.vertex = UnityObjectToClipPos(v.vertex);
    o.uv = v.uv;
    return o;
  }

  float4 integrateVelocity(v2f i) : SV_Target {
    float4 velocity = tex2D(_Velocities, i.uv);
    float4 position = tex2D(_Positions, i.uv);

    for (uint j = 0; j < _PlanetCount; j++) {
      float4 target = _Planets[j];
      float3 toTarget = target.xyz - position.xyz;
      velocity.xyz += _Timestep * _Force * normalize(toTarget) / dot(toTarget, toTarget);
    }

    return velocity;
  }

  float4 integratePositions(v2f i) : SV_Target{
    float4 velocity = tex2D(_Velocities, i.uv);
    float4 position = tex2D(_Positions, i.uv);

    position += velocity * _Timestep;
    return position;
  }

  fragOut initDisc(v2f i) : SV_Target {
    float4 rand = tex2D(_Noise, i.uv);

    uint index = (uint)(rand.x * _PlanetCount);

    float4 planetPos = _Planets[index];
    float4x4 planetRot = _PlanetRotations[index];

    float discRadius = lerp(_MinDiscRadius, _MaxDiscRadius, rand.y);
    float discHeight = ((rand.z * 2) - 1) * _MaxDiscHeight;
    float discAngle = rand.w * 3.14159 * 2;

    float dx = discRadius * cos(discAngle);
    float dy = discHeight;
    float dz = discRadius * sin(discAngle);

    float vx = sin(discAngle);
    float vy = -dy * 0.5;
    float vz = -cos(discAngle);

    float3 discPos = float3(dx, dy, dz);
    float3 discVel = float3(vx, vy, vz); //TODO: correct constant

    float velocityMul = sqrt(_Force / length(discPos));
    discVel = normalize(discVel) * velocityMul;

    discPos = mul(planetRot, float4(discPos, 1));
    discVel = mul(planetRot, float4(discVel, 1));

    discPos += planetPos;

    fragOut o;
    o.dest0 = float4(discPos, 1);
    o.dest1 = float4(discVel, 1);
    return o;
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

    //Pass 2: init galaxy states
		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment initDisc
      ENDCG
		}
	}
}
