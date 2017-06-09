using UnityEngine;
using Leap.Unity;
using Leap.Unity.Attributes;

public abstract partial class ParticleEngine {
  public const int MAX_SPECIES = 12;
  public const float PARTICLE_RADIUS = 0.01f;
  public const float PARTICLE_DIAMETER = PARTICLE_RADIUS * 2;
  public const float PARTICLE_DIAMETER_SQUARED = PARTICLE_DIAMETER * PARTICLE_DIAMETER;
  public const float MAX_SOCIAL_RADIUS = 0;

  public struct Particle {
    public Vector3 position;
    public Vector3 velocity;
    public int species;
  }

  public struct SpeciesData {
    public float drag;
    public SocialData[] socialData;
    public float collisionForce;
    public Vector4 color;
  }

  public struct SocialData {
    public float socialForce;
    public float socialRangeSquared;

    public float socialRange {
      get {
        return Mathf.Sqrt(socialRangeSquared);
      }
      set {
        socialRangeSquared = value * value;
      }
    }
  }
}

public partial class ParticleEngineImplementation : ParticleEngine {

  [Header("Species")]
  [MinMax(0, 1)]
  [SerializeField]
  private Vector2 _dragRange = new Vector2(0.05f, 0.3f);

  [MinMax(0, 1)]
  [SerializeField]
  private Vector2 _collisionForceRange = new Vector2(0.01f, 0.2f);

  [MinValue(0)]
  [SerializeField]
  private float _maxSocialForce = 0.003f;

  [Units("Meters")]
  [MinMax(0, 1)]
  [SerializeField]
  private Vector2 _socialRange = new Vector2(0, 0.5f);

  [Header("Environment")]
  [Units("Meters")]
  [MinValue(0)]
  [SerializeField]
  private float _environmentRadius = 1.0f;

  [MinValue(0)]
  [SerializeField]
  private float _boundaryForce = 0.1f;

  [SerializeField]
  private Transform _homeTransform;

  private Vector3 _homePosition;

  #region ECOSYSTEM

  //----------------------------------
  // randomize species
  //----------------------------------
  private void randomizeSpecies() {
    for (int s = 0; s < MAX_SPECIES; s++) {
      //_speciesData[s].steps = MIN_FORCE_STEPS + (int)((MAX_FORCE_STEPS - MIN_FORCE_STEPS) * Random.value);
      _speciesData[s].drag = Random.Range(_dragRange.x, _dragRange.y);
      _speciesData[s].collisionForce = Random.Range(_collisionForceRange.x, _collisionForceRange.y);
      _speciesData[s].color = new Color(Random.value, Random.value, Random.value, 1);

      for (int o = 0; o < MAX_SPECIES; o++) {
        _socialData[s, o].socialForce = Random.Range(-MAX_SPECIES, MAX_SPECIES);
        _socialData[s, o].socialForce = Random.Range(-_maxSocialForce, _maxSocialForce);
        _socialData[s, o].socialRange = Random.Range(_socialRange.x, _socialRange.y);
      }
    }
  }

  public enum ParticleSystemPreset {
    EcosystemChase
  }

  //--------------------------------------
  // set ecosystem to preset
  //--------------------------------------
  private void setPresetEcosystem(ParticleSystemPreset preset) {
    switch (preset) {
      case ParticleSystemPreset.EcosystemChase:
        for (int s = 0; s < MAX_SPECIES; s++) {
          //_speciesData[s].steps = MIN_FORCE_STEPS;
          _speciesData[s].collisionForce = _collisionForceRange.x;
          _speciesData[s].drag = _dragRange.x;

          for (int o = 0; o < MAX_SPECIES; o++) {
            _socialData[s, o].socialForce = 0.0f;
            _socialData[s, o].socialRange = _socialRange.y;
          }

          _socialData[s, s].socialForce = _maxSocialForce * 0.1f;
        }

        _speciesData[0].color = new Color(0.7f, 0.0f, 0.0f);
        _speciesData[1].color = new Color(0.7f, 0.3f, 0.0f);
        _speciesData[2].color = new Color(0.7f, 0.7f, 0.0f);
        _speciesData[3].color = new Color(0.0f, 0.7f, 0.0f);
        _speciesData[4].color = new Color(0.0f, 0.0f, 0.7f);
        _speciesData[5].color = new Color(0.4f, 0.0f, 0.7f);
        _speciesData[6].color = new Color(1.0f, 0.3f, 0.3f);
        _speciesData[7].color = new Color(1.0f, 0.6f, 0.3f);
        _speciesData[8].color = new Color(1.0f, 1.0f, 0.3f);
        _speciesData[9].color = new Color(0.3f, 1.0f, 0.3f);

        float chase = 0.9f * _maxSocialForce;
        _socialData[0, 1].socialForce = chase;
        _socialData[1, 2].socialForce = chase;
        _socialData[2, 3].socialForce = chase;
        _socialData[3, 4].socialForce = chase;
        _socialData[4, 5].socialForce = chase;
        _socialData[5, 6].socialForce = chase;
        _socialData[6, 7].socialForce = chase;
        _socialData[7, 8].socialForce = chase;
        _socialData[8, 9].socialForce = chase;
        _socialData[8, 0].socialForce = chase;

        float flee = -0.6f * _maxSocialForce;
        _socialData[0, 9].socialForce = flee;
        _socialData[1, 0].socialForce = flee;
        _socialData[2, 1].socialForce = flee;
        _socialData[3, 2].socialForce = flee;
        _socialData[4, 3].socialForce = flee;
        _socialData[5, 4].socialForce = flee;
        _socialData[6, 5].socialForce = flee;
        _socialData[7, 6].socialForce = flee;
        _socialData[8, 7].socialForce = flee;
        _socialData[8, 8].socialForce = flee;

        float range = 0.8f * _maxSocialForce;
        _socialData[0, 9].socialRange = range;
        _socialData[1, 0].socialRange = range;
        _socialData[2, 1].socialRange = range;
        _socialData[3, 2].socialRange = range;
        _socialData[4, 3].socialRange = range;
        _socialData[5, 4].socialRange = range;
        _socialData[6, 5].socialRange = range;
        _socialData[7, 6].socialRange = range;
        _socialData[8, 7].socialRange = range;
        _socialData[8, 8].socialRange = range;
        break;
    }
  }
  #endregion

  #region SIMULATION
  /// <summary>
  /// Perform any initialization before particle simulation is started
  /// right here.
  /// </summary>
  protected override void OnInitializeSimulation() {
    randomizeSpecies();
  }

  /// <summary>
  /// Called every frame right before the particle system is simulated.
  /// You can use this time to do calculations, or emit new particles.
  /// </summary>
  protected override void BeforeParticleUpdate() {
    _homePosition = _homeTransform.position;

    if (Input.GetKeyDown(KeyCode.R)) {
      randomizeSpecies();
    }

    if (Input.GetKey(KeyCode.Space)) {
      for (int i = 0; i < 50; i++) {
        TryEmit(new Particle() {
          position = Random.insideUnitSphere * 0.5f,
          velocity = Vector3.zero,
          species = Random.Range(0, MAX_SPECIES)
        });
      }
    }
  }

  protected override void DoParticleCollisionInteraction(ref Particle particle,
                                                         ref SpeciesData speciesData,
                                                         ref Particle other,
                                                         ref SpeciesData otherSpeciesData) {
    Vector3 vectorToOther = other.position - particle.position;
    float distanceSquared = vectorToOther.sqrMagnitude;

    if (distanceSquared < PARTICLE_DIAMETER_SQUARED && distanceSquared > Mathf.Epsilon) {
      float distance = Mathf.Sqrt(distanceSquared);
      Vector3 directionToOther = vectorToOther / distance;

      float penetration = 1 - distance / PARTICLE_DIAMETER;
      float averageCollisionForce = (speciesData.collisionForce + otherSpeciesData.collisionForce) * 0.5f;
      particle.velocity -= _deltaTime * averageCollisionForce * directionToOther * penetration;
    }
  }

  /// <summary>
  /// Use this method to define how one particle would interact with another.  You should
  /// not modify either this particle or the other particle.  If you want to create a social
  /// force, add it to totalSocialForce and increment totalSocialInteractions.  At the end
  /// of the particle step all social forces will be averaged.
  /// </summary>
  protected override void DoParticleSocialInteraction(ref Particle particle,
                                                      ref SpeciesData speciesData,
                                                      ref SocialData socialData,
                                                      ref Particle other,
                                                      ref SpeciesData otherSpeciesData,
                                                      ref Vector3 totalSocialforce,
                                                      ref int totalSocialInteractions) {
    Vector3 vectorToOther = other.position - particle.position;
    float distanceSquared = vectorToOther.sqrMagnitude;

    if (distanceSquared < socialData.socialRangeSquared) {
      float distance = Mathf.Sqrt(distanceSquared);
      Vector3 directionToOther = vectorToOther / distance;

      totalSocialforce += socialData.socialForce * directionToOther;
      totalSocialInteractions++;
    }
  }


  /// <summary>
  /// Use this method to apply global particle constraints.
  /// </summary>
  protected override void DoParticleConstraints(ref Particle particle, ref SpeciesData speciesData) {

  }

  /// <summary>
  /// Use this method to apply global forces that affect all particles.
  /// </summary>
  protected override void DoParticleGlobalForces(ref Particle particle, ref SpeciesData speciesData) {
    Vector3 vectorFromHome = particle.position - _homePosition;
    float distanceFromHome = vectorFromHome.magnitude;

    if (distanceFromHome > _environmentRadius) {
      Vector3 directionFromHome = vectorFromHome / distanceFromHome;
      float force = (distanceFromHome - _environmentRadius) * _boundaryForce;
      particle.velocity -= force * directionFromHome * _deltaTime;
    }

    particle.velocity *= 1 - speciesData.drag;
  }

  /// <summary>
  /// Called every frame for every particle to determine if it should be killed.  If you
  /// return true from this method, the particle will be removed before the start of the next
  /// simulation step.
  /// </summary>
  protected override bool ShouldKillParticle(ref Particle particle) {
    return false;
  }
  #endregion
}
