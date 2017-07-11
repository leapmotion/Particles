Shader "Unlit/Simulation" {
  Properties {
  }

  CGINCLUDE
  #include "UnityCG.cginc"
#pragma target 4.0

  #define MAX_PARTICLES 4096
  #define MAX_FORCE_STEPS 64
  #define MAX_SPECIES 10
  #define PARTICLE_RADIUS 0.01
  #define PARTICLE_DIAMETER (PARTICLE_RADIUS * 2)
  #define COLLISION_FORCE 0.002

  #define CLUSTER_COUNT 2

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

  struct Cluster {
    float3 center;
    float radius;
    uint count;
    uint start;
    uint end;
  };

  sampler2D _CopySource;
  sampler2D _Velocity;
  sampler2D _Position;

  sampler2D _SocialTemp;
  sampler2D _SocialForce;

  StructuredBuffer<Cluster> _Clusters;
  sampler2D _ClusteredParticles;

  uniform int _ParticleCount;

  float3 _FieldCenter;
  float _FieldRadius;
  float _FieldForce;
  float3 _HeadPos;
  float _HeadRadius;

  float _SpeciesCount;
  float _SpawnRadius;

  float _HandCollisionInverseThickness;
  float _HandCollisionExtraForce;

  int _SocialHandSpecies;
  float _SocialHandForceFactor;

  float4 _SpeciesData[MAX_SPECIES];
  float4 _SocialData[MAX_SPECIES * MAX_SPECIES];

  float4 _CapsuleA[128];
  float4 _CapsuleB[128];
  int _CapsuleCount;

  float4 _Spheres[2];
  float4 _SphereVelocities[2];
  int _SphereCount;
  float _SphereForce;

  int _DebugMode;
  float4 _DebugData;

  float nrand(float2 n) {
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

  v2f vert(appdata v) {
    v2f o;
    o.vertex = UnityObjectToClipPos(v.vertex);
    o.uv = v.uv;
    return o;
  }

  float4 integratePositions(v2f i) : SV_Target{
    float4 pos = tex2Dlod(_Position, float4(i.uv, 0, 0));
    float4 vel = tex2Dlod(_Velocity, float4(i.uv, 0, 0));
    pos.xyz += vel.xyz;

    //Dont hit the head pls
    float3 fromHead = pos.xyz - _HeadPos;
    float distToHead = length(fromHead);
    if (distToHead < _HeadRadius) {
      pos.xyz = _HeadPos + fromHead / distToHead * _HeadRadius;
    }

    if (!isfinite(pos.x)) {
      pos.x = nrand(i.uv) * 0.1;
    }

    if (!isfinite(pos.y)) {
      pos.y = nrand(i.uv * 2) * 0.1;
    }

    if (!isfinite(pos.z)) {
      pos.z = nrand(i.uv * 3) * 0.1;
    }

    return pos;
  }

  float4 globalForces(v2f i) : SV_Target{
    float4 velocity = tex2Dlod(_Velocity, float4(i.uv, 0, 0));
    float4 particle = tex2Dlod(_Position, float4(i.uv, 0, 0));

    //Attraction towards the origin
    float3 toFieldCenter = _FieldCenter - particle.xyz;
    float dist = length(toFieldCenter);
    if (dist > _FieldRadius) {
      velocity.xyz += toFieldCenter * _FieldForce;
    }

    //Grasping by spheres
    {
      float4 sphereForce = float4(0, 0, 0, 0);
      float spheres = 0;
      for (int i = 0; i < _SphereCount; i++) {
        float3 toSphere = _Spheres[i] - particle.xyz;
        if (length(toSphere) < _Spheres[i].w) {
#ifdef SPHERE_MODE_STASIS
          sphereForce += _SphereVelocities[i];
#else
          sphereForce.w += _SphereVelocities[i].w;
#endif
          sphereForce.xyz += toSphere * _SphereForce;
          spheres++;
        }
      }

      if (spheres > 0.5) {
        sphereForce /= spheres;
#ifdef SPHERE_MODE_STASIS
        velocity.xyz = sphereForce.xyz * sphereForce.w;
        velocity.w *= lerp(1, 0.5, sphereForce.w);
#else
        velocity.xyz += sphereForce.xyz * sphereForce.w;
#endif
      } else {
        velocity.w = lerp(velocity.w, 1, 0.05);
      }
    }

    if (!isfinite(velocity.x)) {
      velocity.x = 0;
    }

    if (!isfinite(velocity.y)) {
      velocity.y = 0;
    }

    if (!isfinite(velocity.z)) {
      velocity.z = 0;
    }

    return velocity;
  }

  float4 dampVelocities(v2f i) : SV_Target{
    float4 velocity = tex2Dlod(_Velocity, float4(i.uv, 0, 0));
    float4 particle = tex2Dlod(_Position, float4(i.uv, 0, 0));
    float4 speciesData = _SpeciesData[(int)particle.w];

    //Step offset for social forces
    i.uv.y = speciesData.y / MAX_FORCE_STEPS;
    float4 socialForce = tex2Dlod(_SocialForce, float4(i.uv, 0, 0));
    velocity.xyz += socialForce.xyz;

    //Damping
    velocity.xyz *= lerp(1, speciesData.x, velocity.w);

    return velocity;
  }

  void doParticleParticleInteraction(inout float4 particle, 
                                     inout float4 velocity, 
                                     float4 other,
                                     float socialOffset,
                                     float collisionForce,
                                     inout float4 totalSocialForce) {

  }

  FragmentOutput updateCollisionVelocities(v2f i) {
    float4 particle = tex2Dlod(_Position, float4(i.uv, 0, 0));
    float4 velocity = tex2Dlod(_Velocity, float4(i.uv, 0, 0));
    float socialOffset = (int)(particle.w * MAX_SPECIES);
    float collisionForce = _SpeciesData[0].z;

    //We are going to count our own social force, so start with -1
    float4 totalSocialForce = float4(0, 0, 0, -1);

    //float4 neighborA = tex2D(_Position, i.uv - float2(1.0 / MAX_PARTICLES, 0));
    //float4 neighborB = tex2D(_Position, i.uv + float2(1.0 / MAX_PARTICLES, 0));

    //velocity.xyz += (neighborA.xyz - particle.xyz) * _SpringForce;
    //velocity.xyz += (neighborB.xyz - particle.xyz) * _SpringForce;

#ifdef USE_CLUSTERS
    //for (uint i = 0; i < CLUSTER_COUNT; i++) {
    //  Cluster cluster = _Clusters[i];
    //  float distToCluster = length(particle.xyz - cluster.center);
    //  if (distToCluster > (cluster.radius + 0.5)) {
    //    continue;
    //  }

    //  for (uint j = cluster.start; j < cluster.end; j++) {
    //    float4 other = tex2Dlod(_ClusteredParticles, float4(j / (float)MAX_PARTICLES, 0, 0, 0));
    {
      for (int i = 0; i < _ParticleCount; i++) {
        float4 other = tex2Dlod(_ClusteredParticles, float4(i / (float)MAX_PARTICLES, 0, 0, 0));
#else
    {
      for (int i = 0; i < _ParticleCount; i++) {
        float4 other = tex2Dlod(_Position, float4(i / (float)MAX_PARTICLES, 0, 0, 0));
#endif
        float3 toOther = other.xyz - particle.xyz;
        float distance = length(toOther);
        toOther = distance < 0.0001 ? float3(0, 0, 0) : toOther / distance;

        float otherCollisionForce = _SpeciesData[(int)other.w].z;
        float totalCollisionForce = (collisionForce + otherCollisionForce) * 0.5;

        if (distance < PARTICLE_DIAMETER) {
          float penetration = 1 - distance / PARTICLE_DIAMETER;
          velocity.xyz -= toOther * penetration * totalCollisionForce;
        }

        float2 socialData = _SocialData[(int)(socialOffset + other.w)];

        if (distance < socialData.y) {
          totalSocialForce += float4(socialData.x * toOther, 1);
        }
      }
    }

    //Collision with capsules and social hands
    {
      for (int i = 0; i < _CapsuleCount; i++) {
        float3 a = _CapsuleA[i];
        float3 b = _CapsuleB[i];
        float radius = _CapsuleA[i].w;

        float3 pa = particle.xyz - a;
        float3 ba = b - a;
        float h = saturate(dot(pa, ba) / dot(ba, ba));

        float3 forceVector = pa - ba * h;
        float3 vel = -ba;

        float dist = length(forceVector);
        forceVector /= dist;

        /*
        if (dist < _HandCollisionRadius) {
          float3 normal = normalize(particle.xyz - b);
          float mag = max(0, dot(normal, a - b));

          float3 relativeVel = velocity.xyz - normal * mag;
          float3 reflectedVel = relativeVel - 2 * dot(relativeVel, normal) * normal;
          velocity.xyz = reflectedVel + normal * mag;
          velocity.w = 0;
        }
        */

        float soft = 1 - saturate((dist - radius) * _HandCollisionInverseThickness);

        float3 relVel = vel + velocity.xyz;
        float dir = saturate(dot(normalize(relVel), normalize(pa)));
        velocity.xyz = lerp(velocity.xyz, vel, soft * dir);
        velocity.xyz += forceVector * _HandCollisionExtraForce * soft;


        float2 socialData = _SocialData[(int)(socialOffset + _SocialHandSpecies)];
        if (dist < socialData.y) {
          totalSocialForce += float4(-socialData.x * forceVector, 1) * _SocialHandForceFactor;
        }
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

  float4 randomParticles(v2f i) : SV_Target{
    float4 particle;
    particle.x = nrand(i.uv) - 0.5;
    particle.y = nrand(i.uv * 2 + float2(0.2f, 0.9f)) - 0.5;
    particle.z = nrand(i.uv * 3 + float2(2.2f, 33.9f)) - 0.5;
    particle.w = floor(nrand(i.uv * 4 + float2(23, 54)) * _SpeciesCount);
    //particle.w = i.uv.x * _SpeciesCount;

    particle.xyz *= _SpawnRadius;

    return particle;
  }

  float4 stepSocialQueue(v2f i) : SV_Target{
    float2 shiftedUv = i.uv - float2(0, 1.0 / MAX_FORCE_STEPS);

    float4 newForce = tex2Dlod(_SocialTemp, float4(i.uv, 0, 0));
    float4 shiftedForce = tex2Dlod(_SocialForce, float4(shiftedUv, 0, 0));

    float4 result;

    if (i.uv.y <= 1.0 / MAX_FORCE_STEPS) {
      result = newForce;
    } else {
      result = shiftedForce;
    }

    return result;
  }

  float4 debugOutput(v2f i) : SV_Target {
    float4 values = _DebugData;
    float4 particle = tex2D(_Position, _DebugData.xx);
    float4 particle2 = tex2D(_Position, _DebugData.yy);

    if (_DebugMode == 10) {
      values = particle;
    } else if (_DebugMode == 11) {
      float socialOffset = (int)(particle.w * MAX_SPECIES);
      values.x = socialOffset;
      values.y = (int)(socialOffset + particle2.w);
    } else if (_DebugMode == 12) {
      values.xy = _SocialData[(int)_DebugData.x];
      values.zw = _SpeciesData[(int)_DebugData.y].xy;
    }

    float color = 0;
    color += printValue(i.uv, float2(0, 0.1), float2(0.1, 0.1), values.w, 2, 6);
    color += printValue(i.uv, float2(0, 0.35), float2(0.1, 0.1), values.z, 2, 6);
    color += printValue(i.uv, float2(0, 0.6), float2(0.1, 0.1), values.y, 2, 6);
    color += printValue(i.uv, float2(0, 0.85), float2(0.1, 0.1), values.x, 2, 6);
    return color;
  }

  float4 copy(v2f i) : SV_Target{
    return tex2D(_CopySource, i.uv);
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
      #pragma vertex vert
      #pragma fragment integratePositions
      ENDCG
    }

    //Pass 1: update collisions
    Pass {
      CGPROGRAM
      #pragma multi_compile _ USE_CLUSTERS
      #pragma vertex vert
      #pragma fragment updateCollisionVelocities
      ENDCG
    }

    //Pass 2: global forces
    Pass {
      CGPROGRAM
      #pragma multi_compile SPHERE_MODE_STASIS SPHERE_MODE_FORCE
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

    //Pass 6: debug output
    Pass {
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment debugOutput
      ENDCG
    }

    //Pass 7: copy positions
    Pass {
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment copy
      ENDCG
    }
  }
}
