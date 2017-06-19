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

  struct frag2 {
    float4 color0 : SV_Target0;
    float4 color1 : SV_Target1;
  };

  sampler2D _Velocity;
  sampler2D _Position;
  sampler2D _SocialForce;

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
    velocity *= 0.95;
    return velocity;
	}

  float _Offset;
	float4 updateCollisionVelocities (v2f i) : SV_Target {
    float4 velocity = tex2D(_Velocity, i.uv);
    float4 particle = tex2D(_Position, i.uv);

    float socialOffset = (int)(particle.w * 10);

    //We are going to count our own social force, so start with -1
    float4 totalSocialForce = float4(0, 0, 0, -1);

    for (int i = 0; i < 4096; i++){
      float4 other = tex2D(_Position, float2(i / 4096.0, 0));
      float3 fromOther = particle.xyz - other.xyz;
      float distance = length(fromOther);
      fromOther = distance < 0.0001 ? float3(0, 0, 0) : fromOther / distance;

      if (distance < 0.02) {
        float collisionForce = 1 - distance / 0.02;
        velocity.xyz += fromOther * (collisionForce * 0.005);
      }

      float2 socialData = _SocialData[(int)(socialOffset + other.w)];
      if (distance < socialData.y) {
        totalSocialForce += float4(socialData.x * fromOther, 1);
      }
    }

    if (totalSocialForce.w > 0.5) {
      velocity.xyz -= totalSocialForce.xyz / totalSocialForce.w;
    }

		return velocity;
	}

  float4 randomParticles (v2f i) : SV_Target {
    float x = 2 * nrand(i.uv);
    float y = 2 * nrand(i.uv * 2 + float2(0.2f, 0.9f));
    float z = 2 * nrand(i.uv * 3 + float2(2.2f, 33.9f));
    float w = floor(nrand(i.uv * 4 + float2(23, 54)) * 10);
    return float4(x - 1, y - 1, z - 1, w);
	}

  float4 stepSocialQueue(v2f i) : SV_Target {
    float maxSocialForce = 8;
    float4 force = tex2D(_SocialForce, i.uv - float2(1 / maxSocialForce, 0));
    return force;
  }
  ENDCG

	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 100
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

    //Pass 3: damp velocity
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
