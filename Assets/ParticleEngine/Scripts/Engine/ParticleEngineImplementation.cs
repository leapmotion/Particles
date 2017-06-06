using UnityEngine;
using Leap.Unity.RuntimeGizmos;

public abstract partial class ParticleEngine {
  private const float PARTICLE_RADIUS = 0.05f;
  private const float MAX_SOCIAL_RADIUS = 0;

  public struct Particle {
    public Vector3 position;
    public Vector3 prevPosition;
    public int species;

    public Vector3 velocity {
      get {
        return position - prevPosition;
      }
      set {
        position = prevPosition + velocity;
      }
    }

    public void AddForce(Vector3 force) {
      position += force;
    }
  }

  public struct SpeciesData { }

  public struct SocialData { }
}

public partial class ParticleEngineImplementation : ParticleEngine {

  /// <summary>
  /// Perform any initialization before particle simulation is started
  /// right here.
  /// </summary>
  protected override void OnInitializeSimulation() {

  }

  /// <summary>
  /// Called every frame right before the particle system is simulated.
  /// You can use this time to do calculations, or emit new particles.
  /// </summary>
  protected override void BeforeParticleUpdate() {
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

  /// <summary>
  /// 
  /// </summary>
  protected override void DoParticleConstraints(ref Particle particle, ref SpeciesData speciesData) {

  }

  /// <summary>
  /// 
  /// </summary>
  protected override void DoParticleGlobalForces(ref Particle particle, ref SpeciesData speciesData) {

  }

  /// <summary>
  /// 
  /// </summary>
  protected override bool DoParticleInteraction(ref Particle particle, ref SpeciesData speciesData, ref Particle other, ref SpeciesData otherSpeciesData, ref Vector3 particleDisplacement) {
    return false;
  }

  /// <summary>
  /// 
  /// </summary>
  protected override bool ShouldKillParticle(ref Particle particle) {
    return false;
  }

  /// <summary>
  /// 
  /// </summary>
  public override void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {

  }
}
