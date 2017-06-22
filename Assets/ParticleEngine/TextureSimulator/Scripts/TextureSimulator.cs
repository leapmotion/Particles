using System.Collections.Generic;
using UnityEngine;
using Leap;
using Leap.Unity;
using Leap.Unity.Query;
using Leap.Unity.Attributes;

public class TextureSimulator : MonoBehaviour {
  //These constants match the shader implementation, very important not to change!
  public const int MAX_PARTICLES = 4096;
  public const int MAX_FORCE_STEPS = 64;
  public const int MAX_SPECIES = 31;

  public const string BY_SPECIES = "COLOR_SPECIES";
  public const string BY_SPECIES_WITH_VELOCITY = "COLOR_SPECIES_MAGNITUDE";
  public const string BY_VELOCITY = "COLOR_VELOCITY";

  public const int PASS_INTEGRATE_VELOCITIES = 0;
  public const int PASS_UPDATE_COLLISIONS = 1;
  public const int PASS_GLOBAL_FORCES = 2;
  public const int PASS_DAMP_VELOCITIES_APPLY_SOCIAL_FORCES = 3;
  public const int PASS_RANDOMIZE_PARTICLES = 4;
  public const int PASS_STEP_SOCIAL_QUEUE = 5;

  #region INSPECTOR
  [SerializeField]
  public LeapProvider _provider;

  [Header("Hand Collision")]
  [SerializeField]
  private bool _handCollisionEnabled = true;
  public bool handCollisionEnabled {
    get { return _handCollisionEnabled; }
    set { _handCollisionEnabled = value; }
  }

  [Range(0, 0.1f)]
  [SerializeField]
  private float _handCollisionRadius = 0.04f;
  public float handCollisionRadius {
    get { return _handCollisionRadius; }
    set { _handCollisionRadius = value; }
  }

  [Range(0, 0.02f)]
  [SerializeField]
  private float _handCollisionForce = 0.001f;
  public float handCollisionForce {
    get { return _handCollisionForce; }
    set { _handCollisionForce = value; }
  }

  [Header("Hand Influence")]
  [SerializeField]
  private bool _handInfluenceEnabled = true;
  public bool handInfluenceEnabled {
    get { return _handInfluenceEnabled; }
    set { _handInfluenceEnabled = value; }
  }

  [SerializeField]
  private Material _influenceMat;
  public Material influenceMat {
    get { return _influenceMat; }
  }

  [SerializeField]
  private Mesh _influenceMesh;

  [Range(0, 0.2f)]
  [SerializeField]
  private float _influenceRadius = 0.1f;
  public float influenceRadius {
    get { return _influenceRadius; }
    set { _influenceRadius = value; }
  }

  [Range(0, 1)]
  [SerializeField]
  private float _grabThreshold = 0.35f;
  public float grabThreshold {
    get { return _grabThreshold; }
    set { _grabThreshold = value; }
  }

  [Range(1, 20)]
  [SerializeField]
  private int _grabDelay = 5;
  public int grabDelay {
    get { return _grabDelay; }
    set { _grabDelay = value; }
  }

  [Range(0, 0.2f)]
  [SerializeField]
  private float _influenceNormalOffset = 0.1f;
  public float influenceNormalOffset {
    get { return _influenceNormalOffset; }
    set { _influenceNormalOffset = value; }
  }

  [Range(0, 0.2f)]
  [SerializeField]
  private float _influenceForwardOffset = 0.03f;
  public float influenceForwardOffset {
    get { return _influenceForwardOffset; }
    set { _influenceForwardOffset = value; }
  }

  [Header("Social Hand")]
  [SerializeField]
  private bool _socialHandEnabled = false;
  public bool socialHandEnabled {
    get { return _socialHandEnabled; }
    set { _socialHandEnabled = value; }
  }

  [Range(0, MAX_SPECIES)]
  [SerializeField]
  private int _socialHandSpecies = 0;
  public int socialHandSpecies {
    get { return _socialHandSpecies; }
    set { _socialHandSpecies = value; }
  }

  [Range(0, 100)]
  [SerializeField]
  private float _socialHandForceFactor = 0.5f;
  public float socialHandForceFactor {
    get { return _socialHandForceFactor; }
    set { _socialHandForceFactor = value; }
  }

  [Header("Field")]
  [SerializeField]
  private Transform _fieldCenter;
  public Transform fieldCenter {
    get { return _fieldCenter; }
  }

  [Range(0, 2)]
  [SerializeField]
  private float _fieldRadius = 1;
  public float fieldRadius {
    get { return _fieldRadius; }
    set { _fieldRadius = value; }
  }

  [Range(0, 0.001f)]
  [SerializeField]
  private float _fieldForce = 0.0005f;
  public float fieldForce {
    get { return _fieldForce; }
    set { _fieldForce = value; }
  }

  [Header("Simulation")]
  [SerializeField]
  private RenderTextureFormat _textureFormat = RenderTextureFormat.ARGBFloat;

  [SerializeField]
  private Material _simulationMat;
  public Material simulationMat {
    get { return _simulationMat; }
  }

  [SerializeField]
  private KeyCode _resetParticlePositionsKey = KeyCode.P;

  [Header("Display")]
  [SerializeField]
  private Mesh _particleMesh;

  [SerializeField]
  private Material _particleMat;
  public Material particleMat {
    get { return _particleMat; }
  }

  [OnEditorChange("colorMode")]
  [SerializeField]
  private ColorMode _colorMode = ColorMode.BySpecies;
  public ColorMode colorMode {
    get { return _colorMode; }
    set {
      _particleMat.DisableKeyword(BY_SPECIES);
      _particleMat.DisableKeyword(BY_SPECIES_WITH_VELOCITY);
      _particleMat.DisableKeyword(BY_VELOCITY);

      switch (value) {
        case ColorMode.BySpecies:
          _particleMat.EnableKeyword(BY_SPECIES);
          break;
        case ColorMode.BySpeciesWithMagnitude:
          _particleMat.EnableKeyword(BY_SPECIES_WITH_VELOCITY);
          break;
        case ColorMode.ByVelocity:
          _particleMat.EnableKeyword(BY_VELOCITY);
          break;
      }
      _colorMode = value;
    }
  }

  [Header("Preset Ecosystems")]
  [SerializeField]
  private EcosystemPreset _ecosystemPreset = EcosystemPreset.Fluidy;
  public EcosystemPreset ecosystemPreset {
    get { return _ecosystemPreset; }
    set { _ecosystemPreset = value; }
  }

  [SerializeField]
  private string _ecosystemSeed;

  [SerializeField]
  private KeyCode _loadPresetEcosystemKey = KeyCode.R;

  [SerializeField]
  private KeyCode _randomizeEcosystemKey = KeyCode.Space;

  [SerializeField]
  private KeyCode _loadEcosystemSeedKey = KeyCode.L;

  [Header("Random Ecosystems")]
  [Range(1, MAX_SPECIES)]
  [SerializeField]
  private int _maxSpecies = MAX_SPECIES;
  public int maxSpecies {
    get { return _maxSpecies; }
    set { _maxSpecies = value; }
  }

  [Range(1, MAX_FORCE_STEPS)]
  [SerializeField]
  private int _maxForceSteps = MAX_FORCE_STEPS;
  public int maxForceSteps {
    get { return _maxForceSteps; }
    set { _maxForceSteps = value; }
  }

  [Range(0, 2)]
  [SerializeField]
  private float _spawnRadius = 1;
  public float spawnRadius {
    get { return _spawnRadius; }
    set { _spawnRadius = value; }
  }

  [Range(0, 0.01f)]
  [SerializeField]
  private float _maxSocialForce = 0.003f;
  public float maxSocialForce {
    get { return _maxSocialForce; }
    set { _maxSocialForce = value; }
  }

  [Range(0, 1f)]
  [SerializeField]
  private float _maxSocialRange = 0.5f;
  public float maxSocialRange {
    get { return _maxSocialRange; }
    set { _maxSocialRange = value; }
  }

  [MinMax(0, 1)]
  [SerializeField]
  private Vector2 _dragRange = new Vector2(0.05f, 0.3f);
  public float minDrag {
    get {
      return _dragRange.x;
    }
    set {
      _dragRange.x = value;
    }
  }

  public float maxDrag {
    get {
      return _dragRange.y;
    }
    set {
      _dragRange.y = value;
    }
  }

  [MinMax(0, 1)]
  [SerializeField]
  private Vector2 _hueRange = new Vector2(0, 1);
  public float randomHueMin {
    get {
      return _hueRange.x;
    }
    set {
      _hueRange.x = value;
    }
  }

  public float randomHueMax {
    get {
      return _hueRange.y;
    }
    set {
      _hueRange.y = value;
    }
  }

  [MinMax(0, 1)]
  [SerializeField]
  private Vector2 _valueRange = new Vector2(0, 1);
  public float randomValueMin {
    get {
      return _valueRange.x;
    }
    set {
      _valueRange.x = value;
    }
  }

  public float randomValueMax {
    get {
      return _valueRange.y;
    }
    set {
      _valueRange.y = value;
    }
  }

  [MinMax(0, 1)]
  [SerializeField]
  private Vector2 _saturationRange = new Vector2(0, 1);
  public float randomSaturationMin {
    get {
      return _saturationRange.x;
    }
    set {
      _saturationRange.x = value;
    }
  }

  public float randomSaturationMax {
    get {
      return _saturationRange.y;
    }
    set {
      _saturationRange.y = value;
    }
  }

  [Range(0, 1)]
  [SerializeField]
  private float _randomColorThreshold = 0.15f;
  public float randomColorThreshold {
    get { return _randomColorThreshold; }
    set { _randomColorThreshold = value; }
  }

  [Header("Debug")]
  [SerializeField]
  private Renderer _positionDebug;

  [SerializeField]
  private Renderer _velocityDebug;

  [SerializeField]
  private Renderer _socialDebug;
  #endregion

  //Simulation
  private int stepsPerFrame = 1;
  private RenderTexture _frontPos, _frontVel, _backPos, _backVel;
  private RenderTexture _frontSocial, _backSocial;
  private RenderTexture _socialTemp;

  //Display
  private List<Mesh> _meshes = new List<Mesh>();

  //Hand interaction
  private Vector4[] _capsuleA = new Vector4[64];
  private Vector4[] _capsuleB = new Vector4[64];

  private Vector4[] _spheres = new Vector4[2];
  private Vector4[] _sphereVels = new Vector4[2];

  private HandActor[] _handActors = new HandActor[2];

  #region PUBLIC API

  public enum ColorMode {
    BySpecies,
    BySpeciesWithMagnitude,
    ByVelocity,
  }

  public void SetStepsPerFrame(float value) {
    stepsPerFrame = Mathf.RoundToInt(value * 10);
  }

  public void ResetPositions() {
    GL.LoadPixelMatrix(0, 1, 1, 0);
    blitPos(PASS_RANDOMIZE_PARTICLES);
  }

  #endregion

  #region UNITY MESSAGES
  void Start() {
    _frontPos = createTexture();
    _frontVel = createTexture();
    _backPos = createTexture();
    _backVel = createTexture();
    _socialTemp = createTexture();
    _frontSocial = createTexture(MAX_FORCE_STEPS);
    _backSocial = createTexture(MAX_FORCE_STEPS);

    _simulationMat.SetTexture("_SocialTemp", _socialTemp);
    _simulationMat.SetTexture("_Position", _frontPos);
    _simulationMat.SetTexture("_Velocity", _frontVel);
    _simulationMat.SetTexture("_SocialForce", _frontSocial);

    generateMeshes();

    LoadPresetEcosystem(_ecosystemPreset);

    updateShaderData();

    ResetPositions();

    _handActors.Fill(() => new HandActor() { sim = this });
  }

  void Update() {
    updateShaderData();

    handleUserInput();

    if (_provider != null) {
      doHandCollision();

      doHandInfluence();
    }

    GL.LoadPixelMatrix(0, 1, 1, 0);
    for (int i = 0; i < stepsPerFrame; i++) {
      blitVel(PASS_GLOBAL_FORCES);

      doParticleInteraction();

      blit("_SocialForce", ref _frontSocial, ref _backSocial, PASS_STEP_SOCIAL_QUEUE, 1);

      blitVel(PASS_DAMP_VELOCITIES_APPLY_SOCIAL_FORCES);
      blitPos(PASS_INTEGRATE_VELOCITIES);
    }

    _particleMat.mainTexture = _frontPos;
    _particleMat.SetTexture("_Velocity", _frontVel);
    foreach (var mesh in _meshes) {
      Graphics.DrawMesh(mesh, transform.localToWorldMatrix, _particleMat, 0);
    }

    _positionDebug.material.mainTexture = _frontPos;
    _velocityDebug.material.mainTexture = _frontVel;
    _socialDebug.material.mainTexture = _backSocial;
  }
  #endregion

  #region ECOSYSTEMS
  public const int SPECIES_CAP_FOR_PRESETS = 10;

  public enum EcosystemPreset {
    RedMenace,
    Chase,
    Mitosis,
    Fluidy
  }

  public void LoadPresetEcosystem(EcosystemPreset preset) {
    Color[] colors = new Color[MAX_SPECIES];
    Vector4[,] socialData = new Vector4[MAX_SPECIES, MAX_SPECIES];
    Vector4[] speciesData = new Vector4[MAX_SPECIES];

    //Default colors are greyscale 0 to 1
    for (int i = 0; i < _maxSpecies; i++) {
      float p = i / (_maxSpecies - 1.0f);
      colors[i] = new Color(p, p, p, 1);
    }

    //Default social interactions are zero force with max range
    for (int i = 0; i < MAX_SPECIES; i++) {
      for (int j = 0; j < MAX_SPECIES; j++) {
        socialData[i, j] = new Vector2(0, _maxSocialRange);
      }
    }

    //Default species always have max drag and 0 extra social steps
    for (int i = 0; i < MAX_SPECIES; i++) {
      speciesData[i] = new Vector2(_dragRange.x, 0);
    }

    switch (preset) {
      case EcosystemPreset.RedMenace:
        colors[0] = new Color(1.0f, 0.0f, 0.0f);
        colors[1] = new Color(0.3f, 0.2f, 0.0f);
        colors[2] = new Color(0.3f, 0.3f, 0.0f);
        colors[3] = new Color(0.0f, 0.3f, 0.0f);
        colors[4] = new Color(0.0f, 0.0f, 0.3f);
        colors[5] = new Color(0.3f, 0.0f, 0.3f);
        colors[6] = new Color(0.3f, 0.3f, 0.3f);
        colors[7] = new Color(0.3f, 0.4f, 0.3f);
        colors[8] = new Color(0.3f, 0.4f, 0.3f);
        colors[9] = new Color(0.3f, 0.2f, 0.3f);

        int redSpecies = 0;

        float normalLove = _maxSocialForce * 0.04f;
        float fearOfRed = _maxSocialForce * -1.0f;
        float redLoveOfOthers = _maxSocialForce * 2.0f;
        float redLoveOfSelf = _maxSocialForce * 0.9f;

        float normalRange = _maxSocialRange * 0.4f;
        float fearRange = _maxSocialRange * 0.3f;
        float loveRange = _maxSocialRange * 0.3f;
        float redSelfRange = _maxSocialRange * 0.4f;

        for (int s = 0; s < SPECIES_CAP_FOR_PRESETS; s++) {
          speciesData[s] = new Vector2(Mathf.Lerp(_dragRange.x, _dragRange.y, 0.1f), 0);

          for (int o = 0; o < SPECIES_CAP_FOR_PRESETS; o++) {
            socialData[s, o] = new Vector2(normalLove, normalRange);
          }

          //------------------------------------
          // everyone fears red except for red
          // and red loves everyone
          //------------------------------------
          socialData[s, redSpecies] = new Vector2(fearOfRed, fearRange * ((float)s / (float)SPECIES_CAP_FOR_PRESETS));

          socialData[redSpecies, redSpecies] = new Vector2(redLoveOfSelf, redSelfRange);

          socialData[redSpecies, s] = new Vector2(redLoveOfOthers, loveRange);
        }
        break;
      case EcosystemPreset.Chase:
        for (int i = 0; i < SPECIES_CAP_FOR_PRESETS; i++) {
          speciesData[i] = new Vector2(_dragRange.x, 0);
          socialData[i, i] = new Vector2(_maxSocialForce * 0.1f, _maxSocialRange);
        }

        colors[0] = new Color(0.7f, 0.0f, 0.0f);
        colors[1] = new Color(0.7f, 0.3f, 0.0f);
        colors[2] = new Color(0.7f, 0.7f, 0.0f);
        colors[3] = new Color(0.0f, 0.7f, 0.0f);
        colors[4] = new Color(0.0f, 0.0f, 0.7f);
        colors[5] = new Color(0.4f, 0.0f, 0.7f);
        colors[6] = new Color(1.0f, 0.3f, 0.3f);
        colors[7] = new Color(1.0f, 0.6f, 0.3f);
        colors[8] = new Color(1.0f, 1.0f, 0.3f);
        colors[9] = new Color(0.3f, 1.0f, 0.3f);

        float chase = 0.9f;
        socialData[0, 1] = new Vector2(_maxSocialForce * chase, _maxSocialRange);
        socialData[1, 2] = new Vector2(_maxSocialForce * chase, _maxSocialRange);
        socialData[2, 3] = new Vector2(_maxSocialForce * chase, _maxSocialRange);
        socialData[3, 4] = new Vector2(_maxSocialForce * chase, _maxSocialRange);
        socialData[4, 5] = new Vector2(_maxSocialForce * chase, _maxSocialRange);
        socialData[5, 6] = new Vector2(_maxSocialForce * chase, _maxSocialRange);
        socialData[6, 7] = new Vector2(_maxSocialForce * chase, _maxSocialRange);
        socialData[7, 8] = new Vector2(_maxSocialForce * chase, _maxSocialRange);
        socialData[8, 9] = new Vector2(_maxSocialForce * chase, _maxSocialRange);
        socialData[9, 0] = new Vector2(_maxSocialForce * chase, _maxSocialRange);

        float flee = -0.6f;
        float range = 0.8f * _maxSocialRange;
        socialData[0, 9] = new Vector2(_maxSocialForce * flee, range);
        socialData[1, 0] = new Vector2(_maxSocialForce * flee, range);
        socialData[2, 1] = new Vector2(_maxSocialForce * flee, range);
        socialData[3, 2] = new Vector2(_maxSocialForce * flee, range);
        socialData[4, 3] = new Vector2(_maxSocialForce * flee, range);
        socialData[5, 4] = new Vector2(_maxSocialForce * flee, range);
        socialData[6, 5] = new Vector2(_maxSocialForce * flee, range);
        socialData[7, 6] = new Vector2(_maxSocialForce * flee, range);
        socialData[8, 7] = new Vector2(_maxSocialForce * flee, range);
        socialData[9, 8] = new Vector2(_maxSocialForce * flee, range);
        break;
      case EcosystemPreset.Mitosis:
        for (int i = 0; i < SPECIES_CAP_FOR_PRESETS; i++) {
          speciesData[i] = new Vector2(Mathf.Lerp(_dragRange.x, _dragRange.y, 0.1f), 0);

          for (var j = 0; j < SPECIES_CAP_FOR_PRESETS; j++) {
            float a = (j / (float)SPECIES_CAP_FOR_PRESETS * 0.9f) * _maxSocialForce * 1.0f;
            float b = (i / (float)SPECIES_CAP_FOR_PRESETS * 1.2f) * _maxSocialForce * 0.4f;

            socialData[i, j] = new Vector2(a - b, _maxSocialRange * 0.7f);
          }
        }

        colors[9] = new Color(0.9f, 0.9f, 0.9f);
        colors[8] = new Color(0.9f, 0.7f, 0.3f);
        colors[7] = new Color(0.9f, 0.4f, 0.2f);
        colors[6] = new Color(0.9f, 0.3f, 0.3f);
        colors[5] = new Color(0.6f, 0.3f, 0.6f);
        colors[4] = new Color(0.5f, 0.3f, 0.7f);
        colors[3] = new Color(0.2f, 0.2f, 0.3f);
        colors[2] = new Color(0.1f, 0.1f, 0.3f);
        colors[1] = new Color(0.0f, 0.0f, 0.3f);
        colors[0] = new Color(0.0f, 0.0f, 0.0f);
        break;
      case EcosystemPreset.Fluidy:
        for (var i = 0; i < SPECIES_CAP_FOR_PRESETS; i++) {
          for (var j = 0; j < SPECIES_CAP_FOR_PRESETS; j++) {
            socialData[i, j] = new Vector2(0, 0);
          }

          socialData[i, i] = new Vector2(0.2f * _maxSocialForce, _maxSocialRange * 0.1f);
        }

        for (var i = 0; i < SPECIES_CAP_FOR_PRESETS; i++) {
          for (var j = i + 1; j < SPECIES_CAP_FOR_PRESETS; j++) {
            socialData[i, j] = new Vector2(0.15f * _maxSocialForce, _maxSocialRange);
            socialData[j, i] = new Vector2(-0.1f * _maxSocialForce, _maxSocialRange * 0.3f);
          }
        }
        break;
    }

    //Invert drag before we upload to the GPU
    for (int i = 0; i < MAX_SPECIES; i++) {
      Vector2 species = speciesData[i];
      species.x = 1 - species.x;
      speciesData[i] = species;
    }

    var packedSocialData = new Vector4[MAX_SPECIES * MAX_SPECIES];
    for (int i = 0; i < MAX_SPECIES; i++) {
      for (int j = 0; j < MAX_SPECIES; j++) {
        packedSocialData[i * MAX_SPECIES + j] = socialData[i, j];
      }
    }

    _simulationMat.SetVectorArray("_SocialData", packedSocialData);
    _simulationMat.SetVectorArray("_SpeciesData", speciesData);
    _particleMat.SetColorArray("_Colors", colors);
  }

  public void LoadRandomEcosystem(string seed) {
    Random.InitState(seed.GetHashCode());

    Vector4[] _socialData = new Vector4[MAX_SPECIES * MAX_SPECIES];

    for (int s = 0; s < MAX_SPECIES; s++) {
      for (int o = 0; o < MAX_SPECIES; o++) {
        _socialData[s * MAX_SPECIES + o] = new Vector2(Random.Range(-_maxSocialForce, _maxSocialForce), Random.value * _maxSocialRange);
      }
    }

    Vector4[] speciesData = new Vector4[MAX_SPECIES];
    for (int i = 0; i < MAX_SPECIES; i++) {
      Vector4 data = new Vector4();
      data.x = Random.Range(_dragRange.x, _dragRange.y);
      data.y = Random.Range(0, _maxForceSteps);
      speciesData[i] = data;
    }

    //Perform color randomization last so that it has no effect on particle interaction
    List<Color> colors = new List<Color>();
    for (int i = 0; i < MAX_SPECIES; i++) {
      Color newColor;
      int maxTries = 1000;
      while (true) {
        float h = Random.Range(_hueRange.x, _hueRange.y);
        float s = Random.Range(_saturationRange.x, _saturationRange.y);
        float v = Random.Range(_valueRange.x, _valueRange.y);

        bool alreadyExists = false;
        foreach (var color in colors) {
          float existingH, existingS, existingV;
          Color.RGBToHSV(color, out existingH, out existingS, out existingV);

          if (Mathf.Abs(h - existingH) < _randomColorThreshold &&
              Mathf.Abs(s - existingS) < _randomColorThreshold) {
            alreadyExists = true;
            break;
          }
        }

        maxTries--;
        if (!alreadyExists || maxTries < 0) {
          newColor = Color.HSVToRGB(h, s, v);
          break;
        }
      }

      colors.Add(newColor);
    }

    //Invert drag before we upload to the GPU
    for (int i = 0; i < MAX_SPECIES; i++) {
      Vector2 species = speciesData[i];
      species.x = 1 - species.x;
      speciesData[i] = species;
    }

    _particleMat.SetColorArray("_Colors", colors.ToArray());
    _simulationMat.SetVectorArray("_SpeciesData", speciesData);
    _simulationMat.SetVectorArray("_SocialData", _socialData);
  }
  #endregion

  #region HAND INTERACTION

  private void doHandInfluence() {
    if (!_handInfluenceEnabled) {
      _simulationMat.SetInt("_SphereCount", 0);
      return;
    }

    _handActors[0].Update(Hands.Left);
    _handActors[1].Update(Hands.Right);

    int sphereCount = 0;
    if (_handActors[0].active) {
      _spheres[sphereCount] = _handActors[0].sphere;
      _sphereVels[sphereCount] = _handActors[0].velocity;
      sphereCount++;
    }

    if (_handActors[1].active) {
      _spheres[sphereCount] = _handActors[1].sphere;
      _sphereVels[sphereCount] = _handActors[1].velocity;
      sphereCount++;
    }

    _simulationMat.SetInt("_SphereCount", sphereCount);
    _simulationMat.SetVectorArray("_Spheres", _spheres);
    _simulationMat.SetVectorArray("_SphereVelocities", _sphereVels);
  }

  private void doHandCollision() {
    int capsuleCount = 0;
    foreach (var hand in _provider.CurrentFrame.Hands) {
      foreach (var finger in hand.Fingers) {
        foreach (var bone in finger.bones) {
          _capsuleA[capsuleCount] = bone.PrevJoint.ToVector3();
          _capsuleB[capsuleCount] = bone.NextJoint.ToVector3();
          capsuleCount++;
        }
      }
    }

    _simulationMat.SetFloat("_HandCollisionForce", _handCollisionEnabled ? _handCollisionForce : 0);
    _simulationMat.SetFloat("_HandCollisionRadius", _handCollisionRadius);

    _simulationMat.SetInt("_SocialHandSpecies", _socialHandSpecies);
    _simulationMat.SetFloat("_SocialHandForceFactor", _socialHandEnabled ? _socialHandForceFactor : 0);

    _simulationMat.SetInt("_CapsuleCount", capsuleCount);
    _simulationMat.SetVectorArray("_CapsuleA", _capsuleA);
    _simulationMat.SetVectorArray("_CapsuleB", _capsuleB);
  }

  private class HandActor {
    public TextureSimulator sim;

    public Vector3 position, prevPosition;
    public int frameCount = 0;
    public bool active;

    public Vector4 sphere {
      get {
        Vector4 s = prevPosition;
        s.w = sim._influenceRadius;
        return s;
      }
    }

    public Vector3 velocity {
      get {
        return position - prevPosition;
      }
    }

    public void Update(Hand hand) {
      if (hand != null && hand.GrabAngle > sim._grabThreshold) {
        frameCount = Mathf.Min(sim._grabDelay, frameCount + 1);
      } else {
        frameCount = Mathf.Max(0, frameCount - 1);
      }

      if (hand != null) {
        prevPosition = position;
        position = hand.PalmPosition.ToVector3() + hand.PalmarAxis() * sim._influenceNormalOffset + hand.DistalAxis() * sim._influenceForwardOffset;
      }

      if (active && frameCount == 0) {
        active = false;
      } else if (!active && frameCount == sim._grabDelay) {
        active = true;
        prevPosition = position;
      }

      if (active) {
        Graphics.DrawMesh(sim._influenceMesh, Matrix4x4.TRS(position, Quaternion.identity, Vector3.one * sim._influenceRadius * 2), sim._influenceMat, 0);
      }
    }
  }

  #endregion

  #region PRIVATE IMPLEMENTATION
  private void updateShaderData() {
    _simulationMat.SetVector("_FieldCenter", _fieldCenter.localPosition);
    _simulationMat.SetFloat("_FieldRadius", _fieldRadius);
    _simulationMat.SetFloat("_FieldForce", _fieldForce);

    _simulationMat.SetFloat("_SpawnRadius", _spawnRadius);
    _simulationMat.SetInt("_MaxSpecies", _maxSpecies);
  }

  private void handleUserInput() {
    if (Input.GetKeyDown(_loadPresetEcosystemKey)) {
      LoadPresetEcosystem(_ecosystemPreset);
      ResetPositions();
    }

    if (Input.GetKeyDown(_loadEcosystemSeedKey)) {
      LoadRandomEcosystem(_ecosystemSeed);
      ResetPositions();
    }

    if (Input.GetKeyDown(_randomizeEcosystemKey)) {
      Random.InitState(Time.realtimeSinceStartup.GetHashCode());

      var gen = GetComponent<NameGenerator>();
      string name;
      if (gen == null) {
        name = Random.Range(0, 1000).ToString();
      } else {
        name = gen.GenerateName();
      }
      Debug.Log(name);

      LoadRandomEcosystem(name);

      ResetPositions();
    }

    if (Input.GetKeyDown(_resetParticlePositionsKey)) {
      ResetPositions();
    }
  }

  private void generateMeshes() {
    var sourceVerts = _particleMesh.vertices;
    var sourceTris = _particleMesh.triangles;

    List<Vector3> bakedVerts = new List<Vector3>();
    List<int> bakedTris = new List<int>();
    List<Vector2> bakedUvs = new List<Vector2>();

    Mesh bakedMesh = null;
    for (int i = 0; i < MAX_PARTICLES; i++) {
      if (bakedVerts.Count + sourceVerts.Length > 60000) {
        bakedMesh.SetVertices(bakedVerts);
        bakedMesh.SetTriangles(bakedTris, 0);
        bakedMesh.SetUVs(0, bakedUvs);
        bakedMesh.RecalculateNormals();
        bakedMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 10000);
        bakedMesh = null;

        bakedVerts.Clear();
        bakedTris.Clear();
        bakedUvs.Clear();
      }

      if (bakedMesh == null) {
        sourceTris = _particleMesh.triangles;
        bakedMesh = new Mesh();
        bakedMesh.hideFlags = HideFlags.HideAndDontSave;
        _meshes.Add(bakedMesh);
      }

      bakedVerts.AddRange(sourceVerts);
      bakedTris.AddRange(sourceTris);

      for (int k = 0; k < sourceVerts.Length; k++) {
        bakedUvs.Add(new Vector2((i + 0.5f) / MAX_PARTICLES, 0));
      }

      for (int k = 0; k < sourceTris.Length; k++) {
        sourceTris[k] += sourceVerts.Length;
      }
    }

    bakedMesh.hideFlags = HideFlags.HideAndDontSave;
    bakedMesh.SetVertices(bakedVerts);
    bakedMesh.SetTriangles(bakedTris, 0);
    bakedMesh.SetUVs(0, bakedUvs);
    bakedMesh.RecalculateNormals();
    bakedMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 10000);
  }

  private RenderTexture createTexture(int height = 1) {
    RenderTexture tex = new RenderTexture(MAX_PARTICLES, height, 0, _textureFormat, RenderTextureReadWrite.Linear);
    tex.wrapMode = TextureWrapMode.Clamp;
    tex.filterMode = FilterMode.Point;

    RenderTexture.active = tex;
    GL.Clear(clearDepth: false, clearColor: true, backgroundColor: Color.blue);
    RenderTexture.active = null;
    return tex;
  }

  private void blit(string propertyName, ref RenderTexture front, ref RenderTexture back, int pass, float height) {
    RenderTexture.active = front;
    front.DiscardContents();

    _simulationMat.SetPass(pass);

    GL.Begin(GL.QUADS);

    GL.TexCoord2(0, 1);
    GL.Vertex3(0, 0, 0);

    GL.TexCoord2(1, 1);
    GL.Vertex3(1, 0, 0);

    GL.TexCoord2(1, 0);
    GL.Vertex3(1, height, 0);

    GL.TexCoord2(0, 0);
    GL.Vertex3(0, height, 0);

    GL.End();

    _simulationMat.SetTexture(propertyName, front);

    Utils.Swap(ref front, ref back);
  }

  private RenderBuffer[] _colorBuffers = new RenderBuffer[2];
  private void doParticleInteraction() {
    _colorBuffers[0] = _frontVel.colorBuffer;
    _colorBuffers[1] = _socialTemp.colorBuffer;

    Graphics.SetRenderTarget(_colorBuffers, _frontVel.depthBuffer);
    _frontVel.DiscardContents();
    _socialTemp.DiscardContents();

    _simulationMat.SetPass(1);

    quad();

    _simulationMat.SetTexture("_Velocity", _frontVel);

    Utils.Swap(ref _frontVel, ref _backVel);
  }

  private void blitVel(int pass) {
    blit("_Velocity", ref _frontVel, ref _backVel, pass, 1);
  }

  private void blitPos(int pass) {
    blit("_Position", ref _frontPos, ref _backPos, pass, 1);
  }

  private void quad(float height = 1) {
    GL.Begin(GL.QUADS);

    GL.TexCoord2(0, 0);
    GL.Vertex3(0, 0, 0);

    GL.TexCoord2(1, 0);
    GL.Vertex3(1, 0, 0);

    GL.TexCoord2(1, 1);
    GL.Vertex3(1, height, 0);

    GL.TexCoord2(0, 1);
    GL.Vertex3(0, height, 0);

    GL.End();
  }
  #endregion

}