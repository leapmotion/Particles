using UnityEngine;
using Leap.Unity;
using Leap.Unity.Attributes;

public abstract partial class ParticleEngine {

  public const int MAX_SPECIES = 10;
  public const float PARTICLE_RADIUS = 0.02f;
  public const float PARTICLE_DIAMETER = PARTICLE_RADIUS * 2;
  public const float PARTICLE_DIAMETER_SQUARED = PARTICLE_DIAMETER * PARTICLE_DIAMETER;
  //public const float MAX_SOCIAL_RADIUS = 0;
  public const float BOUNDARY_FORCE = 0.01f;
  public const float ENVIRONMENT_RADIUS = 1.0f;

  //---------------------------------------------------------
  // These parameters are critical for clustering behavior
  //---------------------------------------------------------
  public const float MIN_DRAG = 0.05f;
  public const float MAX_DRAG = 0.3f;
  public const float MIN_COLLISION_FORCE = 0.01f;
  public const float MAX_COLLISION_FORCE = 0.2f;
  public const float MAX_SOCIAL_FORCE = 0.003f;
  public const float MIN_SOCIAL_RANGE = 0.0f;
  public const float MAX_SOCIAL_RANGE = 0.5f;
  public const int MIN_FORCE_STEPS = 1;
  public const int MAX_FORCE_STEPS = 7;

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

public class ParticleEngineImplementation : ParticleEngine {

  [Header("Species")]
  [MinMax(MIN_DRAG, MAX_DRAG)]
  [SerializeField]
  private Vector2 _dragRange = new Vector2(MIN_DRAG, MAX_DRAG);

  [Units("Meters")]
  [MinMax(MIN_COLLISION_FORCE, MAX_COLLISION_FORCE)]
  [SerializeField]
  private Vector2 _collisionForceRange = new Vector2(MIN_COLLISION_FORCE, MAX_COLLISION_FORCE);

  [MinValue(0)]
  [SerializeField]
  private float _maxSocialForce = MAX_SOCIAL_FORCE;

  [Units("Meters")]
  [MinMax(MIN_SOCIAL_RANGE, MAX_SOCIAL_RANGE)]
  [SerializeField]
  private Vector2 _socialRange = new Vector2(MIN_SOCIAL_RANGE, MAX_SOCIAL_RANGE);

  [Header("Environment")]
  [Units("Meters")]
  [MinValue(0)]
  [SerializeField]
  private float _environmentRadius = ENVIRONMENT_RADIUS;

  [MinValue(0)]
  [SerializeField]
  private float _boundaryForce = BOUNDARY_FORCE;

  [SerializeField]
  private Transform _homeTransform;

  [Header("Hand Collisions")]
  [SerializeField]
  private LeapProvider _provider;

  [Header("Global Collisions")]
  [SerializeField]
  private Transform _globalCollidersAnchor;

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
        _socialData[s, o].socialForce = Random.Range(-_maxSocialForce, _maxSocialForce);
        _socialData[s, o].socialRange = Random.Range(_socialRange.x, _socialRange.y);
      }
    }
  }

  public enum ParticleSystemPreset {
    EcosystemChase,
    EcosystemMitosis,
    EcosystemRedMenace
  }

  //--------------------------------------
  // set ecosystem to preset
  //--------------------------------------
  private void setPresetEcosystem(ParticleSystemPreset preset) {
    if (preset == ParticleSystemPreset.EcosystemChase) {
      for (int s = 0; s < MAX_SPECIES; s++) {
        //_speciesData[s].steps = MIN_FORCE_STEPS;
        _speciesData[s].collisionForce = MIN_COLLISION_FORCE;
        _speciesData[s].drag = MIN_DRAG;

        for (int o = 0; o < MAX_SPECIES; o++) {
          _socialData[s, o].socialForce = 0.0f;
          _socialData[s, o].socialRange = MAX_SOCIAL_RANGE;
        }

        _socialData[s, s].socialForce = MAX_SOCIAL_FORCE * 0.1f;
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

      float chase = 0.9f * MAX_SOCIAL_FORCE;
      _socialData[0, 1].socialForce = chase;
      _socialData[1, 2].socialForce = chase;
      _socialData[2, 3].socialForce = chase;
      _socialData[3, 4].socialForce = chase;
      _socialData[4, 5].socialForce = chase;
      _socialData[5, 6].socialForce = chase;
      _socialData[6, 7].socialForce = chase;
      _socialData[7, 8].socialForce = chase;
      _socialData[8, 9].socialForce = chase;
      _socialData[9, 0].socialForce = chase;

      float flee = -0.6f * MAX_SOCIAL_FORCE;
      _socialData[0, 9].socialForce = flee;
      _socialData[1, 0].socialForce = flee;
      _socialData[2, 1].socialForce = flee;
      _socialData[3, 2].socialForce = flee;
      _socialData[4, 3].socialForce = flee;
      _socialData[5, 4].socialForce = flee;
      _socialData[6, 5].socialForce = flee;
      _socialData[7, 6].socialForce = flee;
      _socialData[8, 7].socialForce = flee;
      _socialData[9, 8].socialForce = flee;

      float range = 0.8f * MAX_SOCIAL_FORCE;
      _socialData[0, 9].socialRange = range;
      _socialData[1, 0].socialRange = range;
      _socialData[2, 1].socialRange = range;
      _socialData[3, 2].socialRange = range;
      _socialData[4, 3].socialRange = range;
      _socialData[5, 4].socialRange = range;
      _socialData[6, 5].socialRange = range;
      _socialData[7, 6].socialRange = range;
      _socialData[8, 7].socialRange = range;
      _socialData[9, 8].socialRange = range;
    }



    //-----------------------------------------
    // Mitosis
    //-----------------------------------------
	else if ( preset == ParticleSystemPreset.EcosystemMitosis )
	{
		for (int s=0; s<MAX_SPECIES; s++) 
		{
			//_species[s].steps = MIN_FORCE_STEPS;     
			_speciesData[s].collisionForce 	= MIN_COLLISION_FORCE + ( MAX_COLLISION_FORCE - MIN_COLLISION_FORCE ) * 0.05f;
			_speciesData[s].drag = MIN_DRAG + ( MAX_DRAG - MIN_DRAG ) * 0.1f;

            for (var o=0; o<MAX_SPECIES; o++)
            {
				float a = ( (float)o / (float)MAX_SPECIES * 0.9f ) * MAX_SOCIAL_FORCE * 1.0f;
				float b = ( (float)s / (float)MAX_SPECIES * 1.2f ) * MAX_SOCIAL_FORCE * 0.4f;

				_socialData[s, o].socialForce = a - b;
                _socialData[s, o].socialRange = MAX_SOCIAL_RANGE * 0.7f;
            }
        }

		_speciesData[9].color = new Color( 0.9f, 0.9f, 0.9f );
		_speciesData[8].color = new Color( 0.9f, 0.7f, 0.3f );
		_speciesData[7].color = new Color( 0.9f, 0.4f, 0.2f );
		_speciesData[6].color = new Color( 0.9f, 0.3f, 0.3f );
		_speciesData[5].color = new Color( 0.6f, 0.3f, 0.6f );
		_speciesData[4].color = new Color( 0.5f, 0.3f, 0.7f );
		_speciesData[3].color = new Color( 0.2f, 0.2f, 0.3f );
		_speciesData[2].color = new Color( 0.1f, 0.1f, 0.3f );
		_speciesData[1].color = new Color( 0.0f, 0.0f, 0.3f );
		_speciesData[0].color = new Color( 0.0f, 0.0f, 0.0f );
    }



    //-----------------------------------------------
    // Red Menace
    //-----------------------------------------------
    else if (preset == ParticleSystemPreset.EcosystemRedMenace) {
      _speciesData[0].color = new Color(1.0f, 0.0f, 0.0f);
      _speciesData[1].color = new Color(0.3f, 0.2f, 0.0f);
      _speciesData[2].color = new Color(0.3f, 0.3f, 0.0f);
      _speciesData[3].color = new Color(0.0f, 0.3f, 0.0f);
      _speciesData[4].color = new Color(0.0f, 0.0f, 0.3f);
      _speciesData[5].color = new Color(0.3f, 0.0f, 0.3f);
      _speciesData[6].color = new Color(0.3f, 0.3f, 0.3f);
      _speciesData[7].color = new Color(0.3f, 0.4f, 0.3f);
      _speciesData[8].color = new Color(0.3f, 0.4f, 0.3f);
      _speciesData[9].color = new Color(0.3f, 0.2f, 0.3f);


      int redSpecies = 0;

      float normalLove = MAX_SOCIAL_FORCE * 0.04f;
      float fearOfRed = MAX_SOCIAL_FORCE * -1.0f;
      float redLoveOfOthers = MAX_SOCIAL_FORCE * 2.0f;
      float redLoveOfSelf = MAX_SOCIAL_FORCE * 0.9f;

      float normalRange = MAX_SOCIAL_RANGE * 0.4f;
      float fearRange = MAX_SOCIAL_RANGE * 0.3f;
      float loveRange = MAX_SOCIAL_RANGE * 0.3f;
      float redSelfRange = MAX_SOCIAL_RANGE * 0.4f;

      float drag = 0.1f;
      float collision = 0.3f;

      for (int s = 0; s < MAX_SPECIES; s++) {
        //_species[s].steps = MIN_FORCE_STEPS;     
        _speciesData[s].collisionForce = MIN_COLLISION_FORCE + collision * (MAX_COLLISION_FORCE - MIN_COLLISION_FORCE);
        _speciesData[s].drag = MIN_DRAG + drag * (MAX_DRAG - MIN_DRAG);

        for (int o = 0; o < MAX_SPECIES; o++) {
          _socialData[s, o].socialForce = normalLove;
          _socialData[s, o].socialRange = normalRange;
        }

        //------------------------------------
        // everyone fears red except for red
        // and red loves everyone
        //------------------------------------
        _socialData[s, redSpecies].socialForce = fearOfRed;
        _socialData[s, redSpecies].socialRange = fearRange * ((float)s / (float)MAX_SPECIES);

        _socialData[redSpecies, redSpecies].socialForce = redLoveOfSelf;
        _socialData[redSpecies, redSpecies].socialRange = redSelfRange;

        _socialData[redSpecies, s].socialForce = redLoveOfOthers;
        _socialData[redSpecies, s].socialRange = loveRange;
      }
    }
  }

  #endregion

  /// <summary>
  /// Perform any initialization before particle simulation is started
  /// right here.
  /// </summary>
  protected override void OnInitializeSimulation() {

    setPresetEcosystem(ParticleSystemPreset.EcosystemMitosis);

    //randomizeSpecies();
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
          position = Random.insideUnitSphere * 0.05f,
          velocity = Vector3.zero,
          species = Random.Range(0, MAX_SPECIES)
        });
      }
    }
  }

  /// <summary>
  /// Use this method to define how one particle should collider with another.  You should
  /// directly modify the particle argument's velocity.  This method is called twice every 
  /// frame for each pair of particles that could be potentially colliding.
  /// </summary>
  protected override void DoParticleCollisionInteraction(ref Particle particle,
                                                         ref SpeciesData speciesData,
                                                         ref Particle other,
                                                         ref SpeciesData otherSpeciesData) {
    Vector3 vectorToOther = other.position - particle.position;
    float distanceSquared = vectorToOther.sqrMagnitude;

    //If the distance between particles is less than the particle diameter, we are colliding!
    if (distanceSquared < PARTICLE_DIAMETER_SQUARED && distanceSquared > Mathf.Epsilon) {
      float distance = Mathf.Sqrt(distanceSquared);
      Vector3 directionToOther = vectorToOther / distance;

      float penetration = 1 - distance / PARTICLE_DIAMETER;
      float averageCollisionForce = (speciesData.collisionForce + otherSpeciesData.collisionForce) * 0.5f;

      //Collision force is a product of penetration (0-1) and the average collision force
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

      //If we are close enough for a social force, add the new force to the total force sum
      //and increment the number of total social interactions.
      totalSocialforce += socialData.socialForce * directionToOther;
      totalSocialInteractions++;
    }
  }

  #region GLOBAL COLLISIONS

  public struct Capsule {
    public Vector3 v0, v1;
    public float radius;

    public void CollideWith(ref Particle particle) {
      //TODO
    }
  }

  public struct Sphere {
    public Vector3 center;
    public float radius;

    public void CollideWith(ref Particle particle) {
      //TODO
    }
  }

  private Capsule[] _collisionCapsules = new Capsule[1024];
  private int _numCollisionCapsules = 0;
  private Sphere[] _spheres = new Sphere[1024];
  private int _numCollisionSpheres = 0;

  /// <summary>
  /// Use this method to apply global particle constraints.
  /// </summary>
  protected override void DoParticleConstraints(ref Particle particle, ref SpeciesData speciesData) {

  }
  #endregion

  /// <summary>
  /// Use this method to apply global forces that affect all particles.
  /// </summary>
  protected override void DoParticleGlobalForces(ref Particle particle, ref SpeciesData speciesData) {
    Vector3 vectorFromHome = particle.position - _homePosition;
    float distanceFromHome = vectorFromHome.magnitude;

    if (distanceFromHome > _environmentRadius) {
      Vector3 directionFromHome = vectorFromHome / distanceFromHome;

      float force = (distanceFromHome - _environmentRadius) * _boundaryForce;

      //Force applied gets stronger as the particle gets farther from home
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
    //Currently particles never die
    return false;
  }
}
