using UnityEngine;

public class TestParticleEngine : ParticleEngineBase {

  protected override void Update() {
    base.Update();

    if (Input.GetKey(KeyCode.Space)) {
      TryEmit(new Particle() {
        position = new Vector3(0.45f, 0, 0),
        prevPosition = new Vector3(0.45f, 0, 0) + Random.insideUnitSphere * 0.01f,
        species = 0
      });
    }
  }

  protected override void DoParticleCollision(ref Particle particle,
                                              ref SpeciesData speciesData,
                                              ref Particle other,
                                              ref SpeciesData otherSpeciesData,
                                              ref Vector3 totalDisplacement,
                                              ref int totalCollisions) {
    Vector3 fromOther = particle.position - other.position;
    float dist = fromOther.magnitude;

    if (dist < 0.05f) {
      fromOther *= -0.5f * (dist - 0.05f) / dist;
      totalDisplacement += fromOther;
      totalCollisions++;
    }
  }

  protected override void DoParticleConstraints(ref Particle particle, ref SpeciesData speciesData) {
    if (particle.position.sqrMagnitude > 1) {
      particle.position = particle.position.normalized;
    }
  }

  protected override void DoParticleGlobalForces(ref Particle particle, ref SpeciesData speciesData) {
    particle.position += Vector3.down * 0.001f;
  }

  protected override bool DoParticleInteraction(ref Particle particle, ref SpeciesData speciesData, ref Particle other, ref SpeciesData otherSpeciesData, ref Vector3 particleDisplacement) {
    return false;
  }

  protected override bool ShouldKillParticle(ref Particle particle) {
    return false;
  }
}
