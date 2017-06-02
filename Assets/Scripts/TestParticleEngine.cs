using UnityEngine;

public class TestParticleEngine : ParticleEngineBase {

  [Range(0, 1)]
  public float boxSize = 1;

  protected override void Update() {
    base.Update();

    if (Input.GetKey(KeyCode.Mouse0)) {
      for (int i = 0; i < 50; i++) {
        Vector3 position = Random.insideUnitSphere * 0.5f;
        TryEmit(new Particle() {
          position = position,
          prevPosition = position * 0.99f,
          species = 0
        });
      }
    }
  }

  /*
  protected override void DoParticleCollision(ref Particle particle,
                                              ref SpeciesData speciesData,
                                              ref Particle other,
                                              ref SpeciesData otherSpeciesData,
                                              ref Vector3 totalDisplacement,
                                              ref int totalCollisions) {
    Vector3 fromOther = particle.position - other.position;
    float sqrDist = fromOther.sqrMagnitude;

    if (sqrDist < 0.05f * 0.05f) {
      float dist = Mathf.Sqrt(sqrDist);
      totalDisplacement += fromOther * -0.5f * (dist - 0.05f) / dist;
      totalCollisions++;
    }
  }
  */

  protected override void DoParticleConstraints(ref Particle particle, ref SpeciesData speciesData) {
    //if (particle.position.sqrMagnitude > 1) {
    //  particle.position = particle.position.normalized;
    //}
    
    Vector3 pos = particle.position;
    if (pos.x < -boxSize) {
      pos.x = -boxSize;
    }
    if (pos.x > boxSize) {
      pos.x = boxSize;
    }

    if (pos.z < -boxSize) {
      pos.z = -boxSize;
    }
    if (pos.z > boxSize) {
      pos.z = boxSize;
    }

    if (pos.y < -boxSize) {
      pos.y = -boxSize;
    }
    if (pos.y > boxSize) {
      pos.y = boxSize;
    }

    particle.position = pos;
  }

  protected override void DoParticleGlobalForces(ref Particle particle, ref SpeciesData speciesData) {
    particle.position -= particle.position.normalized * 0.0002f;
    particle.position += Vector3.Cross(particle.position, Vector3.up) * 0.0004f;
    //particle.position += Vector3.down * 0.001f;
  }

  protected override bool DoParticleInteraction(ref Particle particle, ref SpeciesData speciesData, ref Particle other, ref SpeciesData otherSpeciesData, ref Vector3 particleDisplacement) {
    return false;
  }

  protected override bool ShouldKillParticle(ref Particle particle) {
    return false;
  }
}
