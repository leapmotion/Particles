Shader "Unlit/PointSimShader" {
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
    _Noise   ("Noise", 2D) = "white" {}
    _Force   ("Force", Float) = 0.01
	}

  CGINCLUDE
  #include "UnityCG.cginc"

  sampler2D_float _PrevPositions;
  sampler2D_float _CurrPositions;

  sampler2D_float _Noise;
  sampler2D_float _RadiusDistribution;

  float _Force;

  float _PrevTimestep;
  float _Timestep;
  float _FuzzValue;

  float4x4 _PlanetRotations[100];
  float4 _Planets[100];
  float4 _PlanetVelocities[100];

  float _PlanetSizes[100];
  float _PlanetDensities[100];

  uint _PlanetCount;
  float _TotalDensity;

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

  float4 integratePositions(v2f i) : SV_Target {
    float4 prevPos = tex2D(_PrevPositions, i.uv);
    float4 currPos = tex2D(_CurrPositions, i.uv);

    float3 accel = float3(0, 0, 0);
    for (uint j = 0; j < _PlanetCount; j++) {
      float4 target = _Planets[j];
      float3 toTarget = target.xyz - currPos.xyz;
      accel += target.w * normalize(toTarget) / (_FuzzValue + dot(toTarget, toTarget));
    }

    return float4(currPos.xyz + (currPos.xyz - prevPos.xyz) * (_Timestep / _PrevTimestep) + accel * _Timestep * _Timestep * _Force, currPos.w);
  }

  fragOut initDisc(v2f i) {
    float4 rand = tex2D(_Noise, i.uv);
    float4 rand2 = tex2D(_Noise, i.uv + float2(0.5, 0.5)) * 2 - 1;

    float randomDensity = rand.x * _TotalDensity;
    uint index = 0;
    for (uint i = 0; i < _PlanetCount; i++) {
      randomDensity -= _PlanetDensities[i];
      if (randomDensity <= 0) {
        index = i;
        break;
      }
    }

    float planetSize = _PlanetSizes[index];

    float4 planetPos = _Planets[index];
    float4x4 planetRot = _PlanetRotations[index];

    float radiusValue = tex2D(_RadiusDistribution, rand.y).r;
    float discRadius = sqrt(lerp(_MinDiscRadius * _MinDiscRadius, _MaxDiscRadius * _MaxDiscRadius, radiusValue));
    float discHeight = ((rand.z * 2) - 1) * _MaxDiscHeight;
    float discAngle = rand.w * 3.14159 * 2;

    float dx = rand2.x * 0.01 + discRadius * cos(discAngle);
    float dy = rand2.y * 0.01 + discHeight;
    float dz = rand2.z * 0.01 + discRadius * sin(discAngle);

    float vx = sin(discAngle);
    float vy = -dy * 0.5;
    float vz = -cos(discAngle);

    float3 discPos = float3(dx, dy, dz) * planetSize;
    float3 discVel = float3(vx, vy, vz);

    float velocityMul = sqrt(planetPos.w * _Force / length(discPos));
    discVel = normalize(discVel) * velocityMul;

    discPos = mul(planetRot, float4(discPos, 1));
    discVel = mul(planetRot, float4(discVel, 1));
    discVel += _PlanetVelocities[index];

    discPos += planetPos;

    float planetIndex = index / (float)_PlanetCount;

    fragOut o;
    o.dest0 = float4(discPos - discVel * _Timestep, planetIndex);
    o.dest1 = float4(discPos, planetIndex);
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
			#pragma fragment integratePositions
      ENDCG
		}

    //Pass 1: init galaxy states
		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment initDisc
      ENDCG
		}
	}
}
