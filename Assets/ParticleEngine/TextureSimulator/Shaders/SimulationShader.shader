Shader "Unlit/Simulation" {
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
	}

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

	sampler2D _MainTex;
  float2 _SocialData[100];
			
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
    return float4(tex2D(_MainTex, i.uv).xyz, 0);
	}
			
  float4 forceTowardsOrigin (v2f i) : SV_Target {
    fixed3 accel = float3(0,0,0);
    float4 particle = tex2D(_MainTex, i.uv);

    //attraction towards the origin
    float3 toOrigin = -particle.xyz;
    float dist = length(toOrigin);
    if (dist > 0) {
      accel = normalize(toOrigin) * 0.00004;
    }

		return float4(accel, 0);
	}

  float4 dampVelocities (v2f i) : SV_Target {
    float damp = 0.95;
    return float4(damp, damp, damp, 1);
	}

	float4 updateCollisionVelocities (v2f i) : SV_Target {
    fixed3 accel = float3(0,0,0);
    float4 particle = tex2D(_MainTex, i.uv);

    float socialOffset = (int)(particle.w * 10);
    float4 totalSocialForce = float4(0, 0, 0, 0);

    float side = 64.0;
    for (float x = 0; x < 1; x += 1.0/ side) {
      for (float y = 0; y < 1; y += 1.0 / side) {
        float4 other = tex2D(_MainTex, float2(x, y));
        float3 fromOther = particle.xyz - other.xyz;
        float distance = length(fromOther);
        float zeroMult = distance < 0.0001 ? 0 : (1.0 / distance);

        float collisionForce = 1 - distance / 0.02;
        collisionForce = distance > 0.02 ? 0 : collisionForce;
        accel += fromOther * (collisionForce * zeroMult * 0.005);

        float2 socialData = _SocialData[(int)(socialOffset + other.w)];
        float socialForce = socialData.x;
        socialForce = distance > socialData.y ? 0 : socialForce;

        totalSocialForce += float4(fromOther * socialForce, 1) * zeroMult;
      }
    }

    float zeroMult = totalSocialForce.w < 0.5 ? 0 : (1.0 / totalSocialForce.w);
    accel -= totalSocialForce.xyz * zeroMult;

		return float4(accel, 0);
	}

  float4 randomParticles (v2f i) : SV_Target {
    float x = nrand(i.uv);
    float y = nrand(i.uv * 2 + float2(0.2f, 0.9f));
    float z = nrand(i.uv * 3 + float2(2.2f, 33.9f));
    float w = floor(nrand(i.uv * 4 + float2(23, 54)) * 10);
    return float4(x - 0.5, y - 0.5, z - 0.5, w);
	}
  ENDCG

	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 100

    //Pass 0: integrate velocities
    Pass {
      Blend One One

      CGPROGRAM
      #pragma vertex vert
      #pragma fragment integratePositions
      ENDCG
    }

    //Pass 1: update collisions
		Pass {
      Blend One One

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment updateCollisionVelocities
			ENDCG
		}

    //Pass 2: force towards origin
    Pass {
      Blend One One

      CGPROGRAM
      #pragma vertex vert
      #pragma fragment forceTowardsOrigin
      ENDCG
    }

    //Pass 3: damp velocity
    Pass {
      Blend Zero SrcColor

      CGPROGRAM
      #pragma vertex vert
      #pragma fragment dampVelocities
      ENDCG
    }

    //Pass 4: random particles
    Pass {
      Blend One Zero

      CGPROGRAM
      #pragma vertex vert
      #pragma fragment randomParticles
      ENDCG
    }
	}
}
