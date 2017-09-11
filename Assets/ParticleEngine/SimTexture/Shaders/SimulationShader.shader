Shader "Unlit/Simulation" {
  Properties{
  }

  CGINCLUDE
  #include "UnityCG.cginc"

  #define PARTICLE_DIM 64
  #define MAX_PARTICLES (PARTICLE_DIM * PARTICLE_DIM)
  #define MAX_FORCE_STEPS 64
  #define PARTICLE_RADIUS 0.01
  #define PARTICLE_DIAMETER (PARTICLE_RADIUS * 2)
  #define COLLISION_FORCE 0.002

  #define tex2Dlod0(s, c) tex2Dlod(s, half4(c, 0, 0))

  struct appdata_p {
    float4 vertex : POSITION;
    float4 uv : TEXCOORD0;
  };

  struct appdata_i {
    float4 vertex : POSITION;
    float4 uv : TEXCOORD0; 
    float4 social : TEXCOORD1;
#ifdef GENERAL_SAMPLING
    float4 sampleRect : TEXCOORD2;
#endif
  };

  struct v2f_p {
    float4 uv : TEXCOORD0;
    float4 vertex : SV_Position;
  };

  struct v2f_i {
    float4 uv : TEXCOORD0;
    float4 social : TEXCOORD1;
#ifdef GENERAL_SAMPLING
    float4 sampleRect : TEXCOORD2;
#endif
    float4 vertex : SV_Position;
  };

  struct FragmentOutput {
    float4 dest0 : SV_Target0;
    float4 dest1 : SV_Target1;
  };

  sampler2D_half _CopySource;
  sampler2D_half _ParticleVelocities;
  sampler2D_half _ParticlePositions;

  sampler2D_half _SocialTemp;
  sampler2D_half _ParticleSocialForces;

  sampler2D_half _StochasticCoordinates;
  uniform int _StochasticCount;
  uniform half _StochasticOffset;

  uniform uint _SampleWidth;
  uniform uint _SampleHeight;
  uniform half _SampleFraction;

  uniform half _ResetPercent;
  uniform half _ResetForce;
  uniform half _ResetRange;

  uniform half3 _FieldCenter;
  uniform half _FieldRadius;
  uniform half _FieldForce;
  uniform float3 _HeadPos;
  uniform half _HeadRadius;

  uniform half _HandCollisionInverseThickness;
  uniform half _HandCollisionExtraForce;

  uniform half4 _CapsuleA[128];
  uniform half4 _CapsuleB[128];
  uniform int _CapsuleCount;

  uniform half4 _Spheres[2];
  uniform half4x4 _SphereDeltas[2];
  uniform int _SphereCount;
  uniform half _SphereForce;

  uniform int _DebugMode;
  uniform half4 _DebugData;

  half nrand(half2 n) {
    return frac(sin(dot(n.xy, float2(12.9898, 78.233)))* 43758.5453);
  }

  float digitBin(const in int x) {
    return x == 0 ? 480599.0 : x == 1 ? 139810.0 : x == 2 ? 476951.0 : x == 3 ? 476999.0 : x == 4 ? 350020.0 : x == 5 ? 464711.0 : x == 6 ? 464727.0 : x == 7 ? 476228.0 : x == 8 ? 481111.0 : x == 9 ? 481095.0 : 0.0;
  }

  float printValue(float2 fragCoord, float2 pixelCoord, float2 fontSize, float value, float digits, float decimals) {
    float2 charCoord = (fragCoord - pixelCoord) / fontSize;
    if (charCoord.y < 0.0 || charCoord.y >= 1.0) return 0.0;
    float bits = 0.0;
    float digitIndex1 = digits - floor(charCoord.x) + 1.0;
    if (-digitIndex1 <= decimals) {
      float pow1 = pow(10.0, digitIndex1);
      float absValue = abs(value);
      float pivot = max(absValue, 1.5) * 10.0;
      if (pivot < pow1) {
        if (value < 0.0 && pivot >= pow1 * 0.1) bits = 1792.0;
      }
      else if (digitIndex1 == 0.0) {
        if (decimals > 0.0) bits = 2.0;
      }
      else {
        value = digitIndex1 < 0.0 ? frac(absValue) : absValue * 10.0;
        bits = digitBin(int(fmod(value / pow1, 10.0)));
      }
    }
    return floor(fmod(bits / pow(2.0, floor(frac(charCoord.x) * 4.0) + floor(charCoord.y * 5.0) * 4.0), 2.0));
  }

  v2f_p vert_p(appdata_p v) {
    v2f_p o;
    o.vertex = UnityObjectToClipPos(v.vertex);
    o.uv = v.uv;
    return o;
  }

  v2f_i vert_i(appdata_i v) {
    v2f_i o;
    o.vertex = UnityObjectToClipPos(v.vertex);
    o.uv = v.uv;
    o.social = v.social;
#ifdef GENERAL_SAMPLING
    o.sampleRect = v.sampleRect;
#endif
    return o;
  }

  half4 integratePositions(v2f_p i) : SV_Target{
    half4 pos = tex2Dlod0(_ParticlePositions, i.uv.xy);
    half4 vel = tex2Dlod0(_ParticleVelocities, i.uv.xy);
    pos.xyz += vel.xyz * 0.01;

    //Dont hit the head pls
    half3 fromHead = pos.xyz - _HeadPos;
    half distToHead = length(fromHead);
    if (distToHead < _HeadRadius) {
      pos.xyz = _HeadPos + fromHead / distToHead * _HeadRadius;
    }

    return pos;
  }

  half4 globalForces(v2f_p i) : SV_Target{
    half4 velocity = tex2Dlod0(_ParticleVelocities, i.uv.xy);
    half4 particle = tex2Dlod0(_ParticlePositions, i.uv.xy);

    //Attraction towards the origin
    half3 toFieldCenter = _FieldCenter - particle.xyz;
    half dist = length(toFieldCenter);
    if (dist > _FieldRadius) {
      velocity.xyz += toFieldCenter * _FieldForce;
    }

    //Grasping by spheres
    {
      half4 sphereForce = half4(0, 0, 0, 0);
      half spheres = 0;
      for (int i = 0; i < _SphereCount; i++) {
        half3 toSphere = _Spheres[i] - particle.xyz;
        if (length(toSphere) < _Spheres[i].w) {
#ifdef SPHERE_MODE_STASIS
          //sphereForce += _SphereVelocities[i];
          float3 newPos = mul(_SphereDeltas[i], float4(particle.xyz, 1));
          sphereForce += float4(newPos.xyz - particle.xyz, 1);
          //sphereForce += mul(_SphereDeltas[i], float4(0, 0, 0, 1));
#else
          //sphereForce.w += _SphereVelocities[i].w;
#endif
          sphereForce.xyz += toSphere * _SphereForce;
          spheres++;
        }
      }

      if (spheres > 0.5) {
        sphereForce /= spheres;
#ifdef SPHERE_MODE_STASIS
        //Hack here for now
        sphereForce.w = 1;

        velocity.xyz = sphereForce.xyz * sphereForce.w * 100;
        velocity.w *= lerp(1, 0, sphereForce.w);
#else
        velocity.xyz += sphereForce.xyz * sphereForce.w * 100;
#endif
      } else {
        velocity.w = lerp(velocity.w, 1, 0.05);
      }
    }

    //Collision with capsules and social hands
    {
      for (int i = 0; i < _CapsuleCount; i++) {
        half3 a = _CapsuleA[i];
        half3 b = _CapsuleB[i];
        half radius = _CapsuleA[i].w;

        half3 pa = particle.xyz - a;
        half3 ba = b - a;
        half h = saturate(dot(pa, ba) / dot(ba, ba));

        half3 forceVector = pa - ba * h;
        half3 vel = -ba;

        half dist = length(forceVector);
        forceVector /= dist;

        half soft = 1 - saturate((dist - radius) * _HandCollisionInverseThickness);

        half3 relVel = vel + velocity.xyz;
        half dir = saturate(dot(normalize(relVel), normalize(pa)));
        velocity.xyz = lerp(velocity.xyz, vel * 100, soft * dir);
        velocity.xyz += forceVector * _HandCollisionExtraForce * soft * 100;

        //half2 socialData = _SocialData[(int)(socialOffset + _SocialHandSpecies)];
        //if (dist < socialData.y) {
        //  totalSocialForce += half4(-socialData.x * forceVector, 1) * _SocialHandForceFactor;
        //}
      }
    }

    return velocity;
  }

  half4 dampVelocities(v2f_p i) : SV_Target{
    half4 velocity = tex2Dlod0(_ParticleVelocities, i.uv.xy);
    half4 particle = tex2Dlod0(_ParticlePositions, i.uv.xy);

    //Step offset for social forces
    i.uv.y = i.uv.y / MAX_FORCE_STEPS + i.uv.z / MAX_FORCE_STEPS;
    half4 socialForce = tex2Dlod0(_ParticleSocialForces, i.uv.xy);
    velocity.xyz += socialForce.xyz * 0.1;

    //Damping
    //velocity.xyz *= lerp(1, i.uv.w, velocity.w);
    velocity.xyz *= lerp(1, lerp(i.uv.w, 0.95, _ResetPercent), velocity.w);

    return velocity;
  }

  FragmentOutput updateCollisionVelocities(v2f_i i) {
    half4 particle = tex2Dlod0(_ParticlePositions, i.uv.xy);
    half4 velocity = tex2Dlod0(_ParticleVelocities, i.uv.xy) * _SampleFraction;

    half4 totalSocialForce = half4(0, 0, 0, -_SampleFraction);

    half socialForce = lerp(i.social.x, _ResetForce, _ResetPercent);
    half socialRange = lerp(i.social.y, _ResetRange, _ResetPercent);

    //half4 neighborA = tex2Dlod0(_ParticlePositions, i.uv - half2(1.0 / MAX_PARTICLES, 0));
    //half4 neighborB = tex2Dlod0(_ParticlePositions, i.uv + half2(1.0 / MAX_PARTICLES, 0));

    //velocity.xyz += (neighborA.xyz - particle.xyz) * _SpringForce;
    //velocity.xyz += (neighborB.xyz - particle.xyz) * _SpringForce;

    half2 otherUv = half2(0, 0);
    {{

#ifdef STOCHASTIC_SAMPLING
    }}
    //Chose a specific subset of the given stochastic coordinates
    {
      for(int j=0; j<_StochasticCount; j++){
        otherUv = tex2Dlod0(_StochasticCoordinates, half2(j / 256.0, _StochasticOffset)) + i.uv.zw;
#endif

#ifdef UNIFORM_SAMPLE_RECT
    }}
    //Chose a set of uvs that exist within a specific uniform rect
    for (uint y = 0; y < _SampleHeight; y++) {
      for (uint x = 0; x < _SampleWidth; x++) {
        otherUv = half2(x / 64.0, y / 64.0) + i.uv.zw;
#endif

#ifdef GENERAL_SAMPLING
    }}
    //Chose a set of uvs that lies within a custom rect passed in as uv coordinates
    for (half y = i.sampleRect.y; y < i.sampleRect.w; y += (1.0 / 64.0)) {
      for (half x = i.sampleRect.x; x < i.sampleRect.z; x += (1.0 / 64.0)) {
        otherUv = half2(x, y);
#endif

        half4 other = tex2Dlod0(_ParticlePositions, otherUv);

        half3 toOther = other.xyz - particle.xyz;
        half distance = length(toOther);
        toOther = distance < 0.0001 ? half3(0, 0, 0) : toOther / distance;

        if (distance < PARTICLE_DIAMETER) {
          half penetration = 1 - distance / PARTICLE_DIAMETER;
          velocity.xyz -= toOther * penetration * i.social.z;
        }

        if (distance < socialRange) {
          totalSocialForce += half4(socialForce * toOther, 1);
        }
      }
    }

    FragmentOutput output;
    output.dest0 = velocity;
    output.dest1 = totalSocialForce;
    return output;
  }

  half4 stepSocialQueue(v2f_p i) : SV_Target{
    half2 shiftedUv = i.uv.xy - half2(0, 1.0 / MAX_FORCE_STEPS);

    half4 newForce = tex2Dlod0(_SocialTemp, i.uv.xy * half2(1, MAX_FORCE_STEPS));
    if (newForce.w > 0.5) {
      newForce.xyz /= newForce.w;
    }

    half4 shiftedForce = tex2Dlod0(_ParticleSocialForces, shiftedUv);

    half4 result;

    if (i.uv.y <= 1.0 / MAX_FORCE_STEPS) {
      result = newForce;
    } else {
      result = shiftedForce;
    }

    return result;
  }

  float4 debugOutput(v2f_p i) : SV_Target {
    float4 values = _DebugData;
    float4 particle = tex2Dlod0(_ParticlePositions, _DebugData.xx);
    float4 particle2 = tex2Dlod0(_ParticlePositions, _DebugData.yy);

    //if (_DebugMode == 10) {
    //  values = particle;
    //} else if (_DebugMode == 11) {
    //  float socialOffset = (int)(particle.w * MAX_SPECIES);
    //  values.x = socialOffset;
    //  values.y = (int)(socialOffset + particle2.w);
    //} else if (_DebugMode == 12) {
    //  values.xy = _SocialData[(int)_DebugData.x];
    //  values.zw = _SpeciesData[(int)_DebugData.y].xy;
    //}

    float color = 0;
    color += printValue(i.uv, float2(0, 0.1), float2(0.1, 0.1), values.w, 2, 6);
    color += printValue(i.uv, float2(0, 0.35), float2(0.1, 0.1), values.z, 2, 6);
    color += printValue(i.uv, float2(0, 0.6), float2(0.1, 0.1), values.y, 2, 6);
    color += printValue(i.uv, float2(0, 0.85), float2(0.1, 0.1), values.x, 2, 6);
    return color;
  }

  half4 copy(v2f_p i) : SV_Target{
    return tex2Dlod0(_CopySource, i.uv.xy);
  }

  half4 randomizeInit(v2f_p i) : SV_Target{
    float4 particle = tex2Dlod0(_ParticlePositions, i.uv.xy);
    if (!any(particle)) {
      particle = tex2Dlod0(_CopySource, i.uv.xy);
    }
    return particle;
  }

  ENDCG

  SubShader {
  Tags{ "RenderType" = "Opaque" }
    LOD 100
    Cull Off
    ZTest Off
    ZWrite Off
    Blend One Zero

    //Pass 0: integrate velocities
    Pass {
      CGPROGRAM
      #pragma vertex vert_p
      #pragma fragment integratePositions
      ENDCG
    }

    //Pass 1: update collisions
    Pass {
      Blend One One
      CGPROGRAM
      #pragma multi_compile GENERAL_SAMPLING UNIFORM_SAMPLE_RECT STOCHASTIC_SAMPLING
      #pragma vertex vert_i
      #pragma fragment updateCollisionVelocities
      ENDCG
    }

    //Pass 2: global forces
    Pass {
      CGPROGRAM
      #pragma multi_compile SPHERE_MODE_STASIS SPHERE_MODE_FORCE
      #pragma vertex vert_p
      #pragma fragment globalForces
      ENDCG
    }

    //Pass 3: damp velocity and add offset social forces
    Pass {
      CGPROGRAM
      #pragma vertex vert_p
      #pragma fragment dampVelocities
      ENDCG
    }

    //Pass 4: step social queue
    Pass {
      CGPROGRAM
      #pragma vertex vert_p
      #pragma fragment stepSocialQueue
      ENDCG
    }

    //Pass 5: debug output
    Pass {
      CGPROGRAM
      #pragma vertex vert_p
      #pragma fragment debugOutput
      ENDCG
    }

    //Pass 6: copy positions
    Pass {
      CGPROGRAM
      #pragma vertex vert_p
      #pragma fragment copy
      ENDCG
    }

    //Pass 7: randomize init
    Pass {
      CGPROGRAM
      #pragma vertex vert_p
      #pragma fragment randomizeInit
      ENDCG
    }
  }
}
