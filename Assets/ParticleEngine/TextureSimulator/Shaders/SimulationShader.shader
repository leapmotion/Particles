Shader "Unlit/Simulation" {
	Properties { }

  CGINCLUDE
  #include "UnityCG.cginc"

  #define MAX_PARTICLES 4096
  #define MAX_FORCE_STEPS 20
  #define MAX_SPECIES 10
  #define PARTICLE_RADIUS 0.01
  #define PARTICLE_DIAMETER (PARTICLE_RADIUS * 2)
  #define COLLISION_FORCE 0.002

  #define SPHERE_ATTRACTION 0.02

  #define CAPSULE_RADIUS 0.04
  #define CAPSULE_FORCE 0.001

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

  float3 _FieldCenter;
  float _FieldRadius;
  float _FieldForce;

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

  float4 globalForces (v2f i) : SV_Target {
    float4 velocity = tex2D(_Velocity, i.uv);
    float4 particle = tex2D(_Position, i.uv);

    //Attraction towards the origin
    float3 toFieldCenter = _FieldCenter - particle.xyz;
    float dist = length(toFieldCenter);
    if (dist > _FieldRadius) {
      velocity.xyz += toFieldCenter * _FieldForce;
    }

    //Grasping by spheres
    {
      float3 sphereForce = float3(0, 0, 0);
      float spheres = 0;
      for (int i = 0; i < _SphereCount; i++) {
        float3 toSphere = _Spheres[i] - particle.xyz;
        if (length(toSphere) < _Spheres[i].w) {
          sphereForce.xyz += _SphereVelocities[i];
          sphereForce.xyz += toSphere * SPHERE_ATTRACTION;
          spheres++;
        }
      }

      if (spheres > 0.5) {
        velocity.xyz = sphereForce / spheres;
        velocity.w *= 0.5;
      } else {
        velocity.w = lerp(velocity.w, 1, 0.05);
      }
    }

    //Collision with capsules
    {
      for (int i = 0; i < _CapsuleCount; i++) {
        float3 a = _CapsuleA[i];
        float3 b = _CapsuleB[i];

        float3 pa = particle.xyz - a;
        float3 ba = b - a;
        float h = saturate(dot(pa, ba) / dot(ba, ba));

        float3 forceVector = pa - ba * h;
        float dist = length(forceVector);
        if (dist < CAPSULE_RADIUS) {
          velocity.xyz += forceVector / dist * CAPSULE_FORCE;
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
    i.uv.y = speciesData.y / MAX_FORCE_STEPS;
    float4 socialForce = tex2D(_SocialForce, i.uv);
    velocity.xyz += socialForce.xyz;

    //Damping
    velocity.xyz *= lerp(1, speciesData.x, velocity.w);

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

      if (distance < PARTICLE_DIAMETER) {
        float collisionForce = 1 - distance / PARTICLE_DIAMETER;
        velocity.xyz -= toOther * collisionForce * COLLISION_FORCE;
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
    float4 particle;
    particle.x = nrand(i.uv) - 0.5;
    particle.y = nrand(i.uv * 2 + float2(0.2f, 0.9f)) - 0.5;
    particle.z = nrand(i.uv * 3 + float2(2.2f, 33.9f)) - 0.5;
    particle.w = floor(nrand(i.uv * 4 + float2(23, 54)) * 10);

    particle.xyz *= 2;

    return particle;
	}

  float4 stepSocialQueue(v2f i) : SV_Target {
    float2 shiftedUv = i.uv  -float2(0, 1.0 / MAX_FORCE_STEPS);

    float4 newForce = tex2D(_SocialTemp, i.uv);
    float4 shiftedForce = tex2D(_SocialForce, shiftedUv);

    float4 result;

    if (i.uv.y <= 1.0 / MAX_FORCE_STEPS) {
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

    //Pass 2: global forces
    Pass {
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment globalForces
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
