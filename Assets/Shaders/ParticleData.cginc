
#define MAX_FORCE_STEPS 5
#define SPECIES_COUNT 8

#define FRICTION 0.001
#define DAMP_CONSTANT (1.0 - FRICTION)

#define RADIUS 0.0175
#define SQR_RADIUS (RADIUS * RADIUS)

#define MAX_SOCIAL_RANGE (RADIUS * 20)
#define MAX_COLLISION_FORCE 2
#define MAX_SOCIAL_FORCE 0.5

struct Particle {
  float3 position;
  float3 prevPosition;
  float3 color;
};

struct SpeciesData {
  float drag;
  float collisionForce;
  float4 color;
};

struct SocialData {
  float socialForce;
  float socialRange;
};

struct Capsule {
  float3 a;
  float3 b;
  float radius;
};

float noise(float2 n) {
  return frac(sin(dot(n.xy, float2(12.9898, 78.233)))* 43758.5453);
}

void doParticleOnParticleCollision(Particle particle, Particle other, inout float4 totalDepenetration) {
  float3 fromOther = (particle.position - other.position);
  float distSqrd = dot(fromOther, fromOther);

  if (distSqrd < SQR_RADIUS) {
    float deltalength = sqrt(distSqrd);
    fromOther *= -0.5 * (deltalength - RADIUS) / deltalength;
    totalDepenetration += float4(fromOther, 1);
  }
}

void doParticleOnParticleForces(Particle particle, Particle other, inout float4 totalForce) {
  /*
  float3 toOther = (other.position - particle.position);
  float dist = length(toOther);

  SocialData socialData = GetSocialData(particle, other);
  if (dist < socialData.socialRange) {
    float3 directionToOther = toOther / dist;
    totalForce += float4(directionToOther * socialData.socialForce, 1);
  }
  */
}
