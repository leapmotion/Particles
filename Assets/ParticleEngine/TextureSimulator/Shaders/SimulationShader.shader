Shader "Unlit/Simulation" {
	Properties { }

  CGINCLUDE
  #include "UnityCG.cginc"

	struct appdata {
		float4 vertex : POSITION;
		float2 uv : TEXCOORD0;
	};

	struct v2f {
		float2 uv : TEXCOORD0;
		float4 vertex : SV_POSITION;
	};

  struct FragmentOutput {
    float4 dest0 : SV_Target0;
    float4 dest1 : SV_Target1;
  };

  sampler2D _Velocity;
  sampler2D _Position;

  sampler2D _SocialTemp;
  sampler2D _SocialForce;

  float4 _SpeciesData[10];
  float2 _SocialData[100];

  float3 _CapsuleA[64];
  float3 _CapsuleB[64];
  int _CapsuleCount;

  float4 _Spheres[2];
  float3 _SphereVelocities[2];
  int _SphereCount;
			
  float nrand(float2 n){
    return frac(sin(dot(n.xy, float2(12.9898, 78.233)))* 43758.5453);
  }

	v2f vert (appdata v) {
		v2f o;
		o.vertex = UnityObjectToClipPos(v.vertex);
    o.uv = v.uv;
		return o;
	}

  float4 integratePositions (v2f i) : SV_Target {
    float4 pos = tex2D(_Position, i.uv);
    float4 vel = tex2D(_Velocity, i.uv);
    pos.xyz += vel.xyz;
    return pos;
	}

  float4 forceTowardsOrigin (v2f i) : SV_Target {
    float4 velocity = tex2D(_Velocity, i.uv);
    float4 particle = tex2D(_Position, i.uv);
    float lerpAmount = 1;

    //attraction towards the origin
    float3 toOrigin = -particle.xyz;
    float dist = length(toOrigin);
    if (dist > 1) {
      velocity.xyz += toOrigin * 0.0005;
    }

    {
      for (int i = 0; i < _SphereCount; i++) {
        float3 toSphere = _Spheres[i] - particle.xyz;
        if (length(toSphere) < _Spheres[i].w) {
          lerpAmount = 0;
          velocity.xyz += _SphereVelocities[i];
          velocity.xyz += toSphere * 0.02;
        }
      }
    }

    {
      for (int i = 0; i < _CapsuleCount; i++) {
        float3 a = _CapsuleA[i];
        float3 b = _CapsuleB[i];

        float3 pa = particle.xyz - a;
        float3 ba = b - a;
        float h = saturate(dot(pa, ba) / dot(ba, ba));

        float3 forceVector = pa - ba * h;
        float dist = length(forceVector);
        if (dist < 0.02) {
          velocity.xyz += forceVector / dist * 0.001;
        }
      }
    }

		return velocity;
	}

  float4 dampVelocities (v2f i) : SV_Target {
    float4 velocity = tex2D(_Velocity, i.uv);
    float4 particle = tex2D(_Position, i.uv);
    float4 speciesData = _SpeciesData[(int)particle.w];

    //Step offset for social forces
    float maxStep = 8;
    i.uv.y = speciesData.y / maxStep;
    float4 socialForce = tex2D(_SocialForce, i.uv);
    velocity.xyz += socialForce.xyz;

    //Damping
    velocity.xyz *= speciesData.x;

    return velocity;
	}

  FragmentOutput updateCollisionVelocities (v2f i) {
    float4 particle = tex2D(_Position, i.uv);
    float4 velocity = tex2D(_Velocity, i.uv);
    float socialOffset = (int)(particle.w * 10);

    //We are going to count our own social force, so start with -1
    float4 totalSocialForce = float4(0, 0, 0, -1);

    for (int i = 0; i < 4096; i++){
      float4 other = tex2D(_Position, float2(i / 4096.0, 0));
      float3 toOther = other.xyz - particle.xyz;
      float distance = length(toOther);
      toOther = distance < 0.0001 ? float3(0, 0, 0) : toOther / distance;

      if (distance < 0.02) {
        float collisionForce = 1 - distance / 0.02;
        velocity.xyz -= toOther * (collisionForce * 0.005);
      }

      float2 socialData = _SocialData[(int)(socialOffset + other.w)];
      if (distance < socialData.y) {
        totalSocialForce += float4(socialData.x * toOther, 1);
      }
    }

    FragmentOutput output;
    output.dest0 = velocity;
    output.dest1 = float4(0, 0, 0, 0);
    if (totalSocialForce.w > 0.5) {
      output.dest1 = float4(totalSocialForce.xyz / totalSocialForce.w, 0);
    }

		return output;
	}

  float4 randomParticles (v2f i) : SV_Target {
    float x = 2 * nrand(i.uv);
    float y = 2 * nrand(i.uv * 2 + float2(0.2f, 0.9f));
    float z = 2 * nrand(i.uv * 3 + float2(2.2f, 33.9f));
    float w = floor(nrand(i.uv * 4 + float2(23, 54)) * 10);
    return float4(x - 1, y - 1, z - 1, w);
	}

  float4 stepSocialQueue(v2f i) : SV_Target {
    float maxSocialStep = 8;

    float2 shiftedUv = i.uv  -float2(0, 1 / maxSocialStep);

    float4 newForce = tex2D(_SocialTemp, i.uv);
    float4 shiftedForce = tex2D(_SocialForce, shiftedUv);

    float4 result;

    if (i.uv.y <= 1.0 / maxSocialStep) {
      result = newForce;
    } else {
      result = shiftedForce;
    }

    return result;
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

    //Pass 1: update collisions
		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment updateCollisionVelocities
			ENDCG
		}

    //Pass 2: force towards origin
    Pass {
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment forceTowardsOrigin
      ENDCG
    }

    //Pass 3: damp velocity and add offset social forces
    Pass {
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment dampVelocities
      ENDCG
    }

    //Pass 4: random particles
    Pass {
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment randomParticles
      ENDCG
    }

    //Pass 5: step social queue
    Pass {
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment stepSocialQueue
      ENDCG
    }
	}
}
