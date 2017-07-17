using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Leap;
using Leap.Unity;
using Leap.Unity.Query;
using Leap.Unity.Attributes;
using Leap.Unity.RuntimeGizmos;

public class TextureSimulator : MonoBehaviour {
  //These constants match the shader implementation, very important not to change!
  public const int MAX_PARTICLES = 4096;
  public const int MAX_FORCE_STEPS = 64;
  public const int MAX_SPECIES = 10;

  public const string BY_SPECIES = "COLOR_SPECIES";
  public const string BY_SPECIES_WITH_VELOCITY = "COLOR_SPECIES_MAGNITUDE";
  public const string BY_VELOCITY = "COLOR_VELOCITY";

  public const string INTERPOLATION_KEYWORD = "ENABLE_INTERPOLATION";

  public const string INFLUENCE_STASIS_KEYWORD = "SPHERE_MODE_STASIS";
  public const string INFLUENCE_FORCE_KEYWORD = "SPHERE_MODE_FORCE";

  public const string STOCHASTIC_KEYWORD = "ENABLE_STOCHASTIC_SAMPLING";
  public const string STOCHASTIC_PROPERTY = "_StochasticCoordinates";

  public const string TAIL_FISH_KEYWORD = "FISH_TAIL";
  public const string TAIL_SQUASH_KEYWORD = "SQUASH_TAIL";

  public const string VELOCITY_GLOBAL = "_ParticleVelocities";
  public const string POSITION_GLOBAL = "_ParticlePositions";
  public const string SOCIAL_FORCE_GLOBAL = "_ParticleSocialForces";

  public const int PASS_INTEGRATE_VELOCITIES = 0;
  public const int PASS_UPDATE_COLLISIONS = 1;
  public const int PASS_GLOBAL_FORCES = 2;
  public const int PASS_DAMP_VELOCITIES_APPLY_SOCIAL_FORCES = 3;
  public const int PASS_RANDOMIZE_PARTICLES = 4;
  public const int PASS_STEP_SOCIAL_QUEUE = 5;
  public const int PASS_SHADER_DEBUG = 6;
  public const int PASS_COPY = 7;

  #region INSPECTOR
  [SerializeField]
  public LeapProvider _provider;

  //###########################//
  ///      Hand Collision      //
  //###########################//
  [Header("Hand Collision")]
  [SerializeField]
  private bool _handCollisionEnabled = true;
  public bool handCollisionEnabled {
    get { return _handCollisionEnabled; }
    set { _handCollisionEnabled = value; }
  }

  [SerializeField]
  private bool _disableCollisionWhenGrasping = true;
  public bool disableCollisionWhenGrasping {
    get { return _disableCollisionWhenGrasping; }
    set { _disableCollisionWhenGrasping = value; }
  }

  [SerializeField]
  private Vector2 _handCollisionRadius = new Vector2(0.02f, 0.04f);
  public float minHandCollisionRadius {
    get { return _handCollisionRadius.x; }
    set { _handCollisionRadius.x = value; }
  }

  public float maxHandCollisionRadius {
    get { return _handCollisionRadius.y; }
    set { _handCollisionRadius.y = value; }
  }

  [SerializeField]
  private float _handCollisionThickness = 0.01f;
  public float handCollisionThickness {
    get { return _handCollisionThickness; }
    set { _handCollisionThickness = value; }
  }

  [Range(0, 2)]
  [SerializeField]
  private float _handCollisionVelocityScale = 1.2f;
  public float handCollisionVelocityScale {
    get { return _handCollisionVelocityScale; }
    set { _handCollisionVelocityScale = value; }
  }

  [MinValue(0)]
  [SerializeField]
  private float _maxHandCollisionSpeed = 1;
  public float maxHandCollisionSpeed {
    get { return _maxHandCollisionSpeed; }
    set { _maxHandCollisionSpeed = value; }
  }

  [SerializeField]
  private AnimationCurve _handVelocityToCollisionRadius;
  public AnimationCurve handVelocityToCollisionRadius {
    get { return _handVelocityToCollisionRadius; }
  }

  [MinValue(0)]
  [SerializeField]
  private float _extraHandCollisionForce = 0.001f;
  public float extraHandCollisionForce {
    get { return _extraHandCollisionForce; }
    set { _extraHandCollisionForce = value; }
  }

  [Range(1, 5)]
  [SerializeField]
  private int _spheresPerBone = 1;
  public int spheresPerBone {
    get { return _spheresPerBone; }
    set { _spheresPerBone = value; }
  }

  [Range(1, 5)]
  [SerializeField]
  private int _spheresPerMetacarpal = 3;
  public int spheresPerMetacarpal {
    get { return _spheresPerMetacarpal; }
    set { _spheresPerMetacarpal = value; }
  }

  //###########################//
  ///      Hand Influence      //
  //###########################//
  [Header("Hand Influence")]
  [SerializeField]
  private bool _handInfluenceEnabled = true;
  public bool handInfluenceEnabled {
    get { return _handInfluenceEnabled; }
    set { _handInfluenceEnabled = value; }
  }

  [SerializeField]
  private bool _showHandInfluenceBubble = false;
  public bool showHandInfluenceBubble {
    get { return _showHandInfluenceBubble; }
    set { _showHandInfluenceBubble = value; }
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

  [SerializeField]
  [OnEditorChange("handInfluenceType")]
  private HandInfluenceType _handInfluenceType = HandInfluenceType.Force;
  public HandInfluenceType handInfluenceType {
    get { return _handInfluenceType; }
    set {
      _handInfluenceType = value;
      updateKeywords();
    }
  }
  public void SetHandInfluenceType(HandInfluenceType influenceType) {
    handInfluenceType = influenceType;
  }
  public void SetHandInfluenceType(int typeIdx) {
    handInfluenceType = (HandInfluenceType)typeIdx;
  }

  public void SetMaxInfluenceRadius(float radius) {
    stasisInfluenceSettings.maxRadius = radius;
    forceInfluenceSettings.maxRadius = radius;
  }

  public void SetMaxInfluenceForce(float force) {
    stasisInfluenceSettings.force = force;
    forceInfluenceSettings.force = force;
  }

  public void SetInfluenceRadiusSmoothing(float smoothing) {
    forceInfluenceSettings.grabStrengthSmoothing = smoothing;
    stasisInfluenceSettings.grabStrengthSmoothing = smoothing;
  }

  [SerializeField]
  private StasisInfluenceSettings _stasisInfluenceSettings;
  public StasisInfluenceSettings stasisInfluenceSettings {
    get { return _stasisInfluenceSettings; }
  }

  [System.Serializable]
  public class StasisInfluenceSettings {
    [Range(0, 1)]
    [SerializeField]
    private float _maxRadius = 0.07f;
    public float maxRadius {
      get { return _maxRadius; }
      set { _maxRadius = value; }
    }

    [SerializeField]
    private float _force = 0.02f;
    public float force {
      get { return _force; }
      set { _force = value; }
    }

    [MinValue(0.01f)]
    [SerializeField]
    private float _grabStrengthSmoothing = 0.01f;
    public float grabStrengthSmoothing {
      get { return _grabStrengthSmoothing; }
      set { _grabStrengthSmoothing = value; }
    }
  }

  [SerializeField]
  private ForceInfluenceSettings _forceInfluenceSettings;
  public ForceInfluenceSettings forceInfluenceSettings {
    get { return _forceInfluenceSettings; }
  }

  [System.Serializable]
  public class ForceInfluenceSettings {
    [Range(0, 1)]
    [SerializeField]
    private float _maxRadius = 0.3f;
    public float maxRadius {
      get { return _maxRadius; }
      set { _maxRadius = value; }
    }

    [SerializeField]
    private float _force = 0.02f;
    public float force {
      get { return _force; }
      set { _force = value; }
    }

    [MinValue(0.01f)]
    [SerializeField]
    private float _grabStrengthSmoothing = 0.01f;
    public float grabStrengthSmoothing {
      get { return _grabStrengthSmoothing; }
      set { _grabStrengthSmoothing = value; }
    }
  }

  [SerializeField]
  private HandInfluenceMode _handInfluenceMode = HandInfluenceMode.Binary;
  public HandInfluenceMode handInfluenceMode {
    get { return _handInfluenceMode; }
    set { _handInfluenceMode = value; }
  }
  public void SetHandInfluenceMode(HandInfluenceMode influenceMode) {
    handInfluenceMode = influenceMode;
  }
  public void SetHandInfluenceMode(int modeIdx) {
    handInfluenceMode = (HandInfluenceMode)modeIdx;
  }

  [SerializeField]
  private InfluenceBinarySettings _influenceBinarySettings;
  public InfluenceBinarySettings influenceBinarySettings {
    get { return _influenceBinarySettings; }
  }

  [System.Serializable]
  public class InfluenceBinarySettings {
    [MinMax(0, 1)]
    [SerializeField]
    private Vector2 _grabStrengthTransition = new Vector2(0.25f, 0.3f);
    public float startGrabStrength {
      get { return _grabStrengthTransition.y; }
      set { _grabStrengthTransition.y = value; }
    }

    public float endGrabStrength {
      get { return _grabStrengthTransition.x; }
      set { _grabStrengthTransition.x = value; }
    }
  }

  [SerializeField]
  private InfluenceRadiusSettings _influenceRadiusSettings;
  public InfluenceRadiusSettings influenceRadiusSettings {
    get { return _influenceRadiusSettings; }
  }

  [System.Serializable]
  public class InfluenceRadiusSettings {
    public AnimationCurve grabStrengthToRadius;
    public AnimationCurve grabStrengthToInfluence;
  }

  [SerializeField]
  private InfluenceFadeSettings _influenceFadeSettings;
  public InfluenceFadeSettings influenceFadeSettings {
    get { return _influenceFadeSettings; }
  }

  [System.Serializable]
  public class InfluenceFadeSettings {
    public AnimationCurve grabStrengthToAlpha;
    public AnimationCurve grabStrengthToInfluence;
  }

  //########################//
  ///      Social Hand      //
  //########################//
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

  //##################//
  ///      Field      //
  //##################//
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

  [MinValue(0)]
  [SerializeField]
  private float _headRadius = 0.15f;
  public float headRadius {
    get { return _headRadius; }
    set { _headRadius = value; }
  }

  //#######################//
  ///      Simulation      //
  //#######################//
  [Header("Simulation")]
  [SerializeField]
  private bool _simulationEnabled = true;
  public bool simulationEnabled {
    get { return _simulationEnabled; }
    set { _simulationEnabled = value; }
  }

  [SerializeField]
  [OnEditorChange("dynamicTimestepEnabled")]
  private bool _dynamicTimestepEnabled = true;
  public bool dynamicTimestepEnabled {
    get { return _dynamicTimestepEnabled; }
    set {
      _dynamicTimestepEnabled = value;
      updateKeywords();
    }
  }

  [SerializeField]
  private bool _limitStepsPerFrame = true;
  public bool limitStepsPerFrame {
    get { return _limitStepsPerFrame; }
    set { _limitStepsPerFrame = value; }
  }

  [Range(10, 120)]
  [SerializeField]
  private float _simulationFPS = 60;
  public float simulationFPS {
    get { return _simulationFPS; }
    set { _simulationFPS = value; }
  }

  [Range(0, 2)]
  [SerializeField]
  private float _simulationTimescale = 1;
  public float simulationTimescale {
    get { return _simulationTimescale; }
    set { _simulationTimescale = value; }
  }

  [SerializeField]
  private RenderTextureFormat _textureFormat = RenderTextureFormat.ARGBFloat;

  [SerializeField]
  private Material _simulationMat;
  public Material simulationMat {
    get { return _simulationMat; }
  }

  [SerializeField]
  private KeyCode _resetParticlePositionsKey = KeyCode.P;

  //################################//
  ///      Stochastic Sampling      //
  //################################//
  [Header("Stochastic Sampling")]
  [OnEditorChange("stochasticSamplingEnabled")]
  [SerializeField]
  private bool _stochasticSamplingEnabled = false;
  public bool stochasticSamplingEnabled {
    get { return _stochasticSamplingEnabled; }
    set {
      _stochasticSamplingEnabled = value;
      updateKeywords();
    }
  }

  [Range(0, 1)]
  [SerializeField]
  private float _stochasticPercent = 0.5f;
  public float stochasticPercent {
    get { return _stochasticPercent; }
    set { _stochasticPercent = value; }
  }

  [Range(1, 1024)]
  [OnEditorChange("stochasticCycleCount")]
  [SerializeField]
  private int _stochasticCycleCount = 128;
  public int stochasticCycleCount {
    get { return _stochasticCycleCount; }
    set {
      _stochasticCycleCount = value;
      updateKeywords();
    }
  }

  //####################//
  ///      Display      //
  //####################//
  [SerializeField]
  private bool _displayParticles = true;
  public bool displayParticles {
    get { return _displayParticles; }
    set { _displayParticles = value; }
  }

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
      _colorMode = value;
      updateKeywords();
    }
  }

  [OnEditorChange("trailMode")]
  [SerializeField]
  private TrailMode _trailMode = TrailMode.Fish;
  public TrailMode trailMode {
    get { return _trailMode; }
    set {
      _trailMode = value;
      updateKeywords();
    }
  }

  //#######################//
  ///      Ecosystems      //
  //#######################//
  [Header("Ecosystems")]
  [Range(0, 2)]
  [SerializeField]
  private float _spawnRadius = 1;
  public float spawnRadius {
    get { return _spawnRadius; }
    set { _spawnRadius = value; }
  }

  [SerializeField]
  private string _ecosystemSeed;

  [SerializeField]
  private KeyCode _loadPresetEcosystemKey = KeyCode.R;

  [SerializeField]
  private KeyCode _randomizeEcosystemKey = KeyCode.Space;

  [SerializeField]
  private KeyCode _loadEcosystemSeedKey = KeyCode.L;

  [Space]
  [SerializeField]
  private PresetSettings _presetEcosystemSettings;
  public PresetSettings presetEcosystemSettings {
    get { return _presetEcosystemSettings; }
  }

  [System.Serializable]
  public class PresetSettings {
    [SerializeField]
    private EcosystemPreset _ecosystemPreset = EcosystemPreset.Fluidy;
    public EcosystemPreset ecosystemPreset {
      get { return _ecosystemPreset; }
      set { _ecosystemPreset = value; }
    }

    [Range(1, MAX_FORCE_STEPS)]
    [SerializeField]
    private int _maxForceSteps = 7;
    public int maxForceSteps {
      get { return _maxForceSteps; }
      set { _maxForceSteps = value; }
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
      get { return _dragRange.x; }
      set { _dragRange.x = value; }
    }

    public float maxDrag {
      get { return _dragRange.y; }
      set { _dragRange.y = value; }
    }

    [MinMax(0, 0.05f)]
    [SerializeField]
    private Vector2 _collisionForceRange = new Vector2(0.002f, 0.009f);
    public float minCollision {
      get { return _collisionForceRange.x; }
      set { _collisionForceRange.x = value; }
    }

    public float maxCollision {
      get { return _collisionForceRange.y; }
      set { _collisionForceRange.y = value; }
    }
  }

  [Space]
  [SerializeField]
  private RandomEcosystemSettings _randomEcosystemSettings;
  public RandomEcosystemSettings randomEcosystemSettings {
    get { return _randomEcosystemSettings; }
  }

  [System.Serializable]
  public class RandomEcosystemSettings {
    [Range(1, MAX_SPECIES)]
    [SerializeField]
    private int _speciesCount = 10;
    public int speciesCount {
      get { return _speciesCount; }
      set { _speciesCount = value; }
    }

    [Range(1, MAX_FORCE_STEPS)]
    [SerializeField]
    private int _maxForceSteps = MAX_FORCE_STEPS;
    public int maxForceSteps {
      get { return _maxForceSteps; }
      set { _maxForceSteps = value; }
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
      get { return _dragRange.x; }
      set { _dragRange.x = value; }
    }

    public float maxDrag {
      get { return _dragRange.y; }
      set { _dragRange.y = value; }
    }

    [MinMax(0, 0.05f)]
    [SerializeField]
    private Vector2 _collisionForceRange = new Vector2(0.002f, 0.009f);
    public float minCollision {
      get { return _collisionForceRange.x; }
      set { _collisionForceRange.x = value; }
    }

    public float maxCollision {
      get { return _collisionForceRange.y; }
      set { _collisionForceRange.y = value; }
    }

    [MinMax(0, 1)]
    [SerializeField]
    private Vector2 _hueRange = new Vector2(0, 1);
    public float minHue {
      get { return _hueRange.x; }
      set { _hueRange.x = value; }
    }

    public float maxHue {
      get { return _hueRange.y; }
      set { _hueRange.y = value; }
    }

    [MinMax(0, 1)]
    [SerializeField]
    private Vector2 _valueRange = new Vector2(0, 1);
    public float minValue {
      get { return _valueRange.x; }
      set { _valueRange.x = value; }
    }

    public float maxValue {
      get { return _valueRange.y; }
      set { _valueRange.y = value; }
    }

    [MinMax(0, 1)]
    [SerializeField]
    private Vector2 _saturationRange = new Vector2(0, 1);
    public float minSaturation {
      get { return _saturationRange.x; }
      set { _saturationRange.x = value; }
    }

    public float maxSaturation {
      get { return _saturationRange.y; }
      set { _saturationRange.y = value; }
    }

    [Range(0, 1)]
    [SerializeField]
    private float _randomColorThreshold = 0.15f;
    public float randomColorThreshold {
      get { return _randomColorThreshold; }
      set { _randomColorThreshold = value; }
    }
  }

  //##################//
  ///      Debug      //
  //##################//
  [Header("Debug")]
  [SerializeField]
  private Renderer _positionDebug;

  [SerializeField]
  private Renderer _velocityDebug;

  [SerializeField]
  private Renderer _socialDebug;

  [SerializeField]
  private Renderer _shaderDataDebug;

  [SerializeField]
  private bool _drawHandColliders = true;
  public void SetDrawHandColliders(bool shouldDraw) {
    _drawHandColliders = shouldDraw;
  }

  [SerializeField]
  private Color _handColliderColor = Color.black;

  [SerializeField]
  private bool _enableSpeciesDebugColors = false;

  [Range(0, MAX_SPECIES - 1)]
  [SerializeField]
  private int _debugSpeciesNumber = 0;

  [SerializeField]
  private ShaderDebugMode _shaderDebugMode = ShaderDebugMode.None;

  [SerializeField]
  private int _shaderDebugData0;

  [SerializeField]
  private int _shaderDebugData1;
  #endregion

  //Simulation
  private List<CommandBuffer> _simulationCommands = new List<CommandBuffer>();
  private int _commandIndex = 0;

  private int _textureDimension;

  private SimulationDescription _currentSimDescription = null;
  
  private RenderTexture _positionSrc, _velocitySrc, _positionDst, _velocityDst;
  private RenderTexture _socialQueueSrc, _socialQueueDst;
  private RenderTexture _socialTemp;

  private Mesh _blitMeshQuad;
  private Mesh _blitMeshInteraction;
  private Mesh _blitMeshParticle;

  private float _currScaledTime = 0;
  private float _currSimulationTime = 0;
  private float _prevSimulationTime = 0;

  //Display
  private List<Mesh> _renderMeshes = new List<Mesh>();
  private MaterialPropertyBlock _displayBlock;

  //Hand interaction
  private Vector4[] _capsuleA = new Vector4[128];
  private Vector4[] _capsuleB = new Vector4[128];

  private Vector4[] _spheres = new Vector4[2];
  private Vector4[] _sphereVels = new Vector4[2];

  private HandActor[] _handActors = new HandActor[2];
  private Hand _prevLeft, _prevRight;

  //Shader debug
  private RenderTexture _shaderDebugTexture;

  #region PUBLIC API

  public TextureSimulator() {
    _textureDimension = Mathf.RoundToInt(Mathf.Sqrt(MAX_PARTICLES));
    if (_textureDimension * _textureDimension != MAX_PARTICLES) {
      Debug.LogError("Max particles must be a square number.");
    }
  }

  public enum ColorMode {
    BySpecies,
    BySpeciesWithMagnitude,
    ByVelocity,
  }

  public enum ShaderDebugMode {
    None = -1,
    Raw = 0,
    RawScaled = 1,
    Particle = 10,
    SocialIndices = 11,
    SocialData = 12
  }

  public enum HandInfluenceType {
    Stasis,
    Force
  }

  public enum HandInfluenceMode {
    Binary,
    Radius,
    Fade
  }

  public enum TrailMode {
    Fish,
    Squash
  }

  public float simulationAge {
    get {
      return _currSimulationTime;
    }
  }

  public float maxInfluenceRadius {
    get {
      switch (_handInfluenceType) {
        case HandInfluenceType.Force:
          return _forceInfluenceSettings.maxRadius;
        case HandInfluenceType.Stasis:
          return _stasisInfluenceSettings.maxRadius;
        default:
          throw new System.Exception();
      }
    }
  }

  public float influenceForce {
    get {
      switch (_handInfluenceType) {
        case HandInfluenceType.Force:
          return _forceInfluenceSettings.force;
        case HandInfluenceType.Stasis:
          return _stasisInfluenceSettings.force;
        default:
          throw new System.Exception();
      }
    }
  }

  public float influenceGrabSmoothing {
    get {
      switch (_handInfluenceType) {
        case HandInfluenceType.Force:
          return _forceInfluenceSettings.grabStrengthSmoothing;
        case HandInfluenceType.Stasis:
          return _stasisInfluenceSettings.grabStrengthSmoothing;
        default:
          throw new System.Exception();
      }
    }
  }

  public MaterialPropertyBlock displayProperties {
    get {
      return _displayBlock;
    }
  }

  public Color GetSpeciesColor(int speciesIdx) {
    var colors = Pool<List<Color>>.Spawn();
    try {
      _particleMat.GetColorArray("_Colors", colors);
      if (speciesIdx > colors.Count - 1) {
        return Color.black;
      } else {
        return colors[speciesIdx];
      }
    } finally {
      colors.Clear();
      Pool<List<Color>>.Recycle(colors);
    }
  }

  public int currentSpeciesCount {
    get {
      return _currentSimDescription.toSpawn.Count;
    }
  }

  #endregion

  #region UNITY MESSAGES
  private void Awake() {
    _displayBlock = new MaterialPropertyBlock();
  }

  void Start() {
    _positionSrc = createParticleTexture();
    _positionSrc.name = "Position A";

    _positionDst = createParticleTexture();
    _positionDst.name = "Position B";

    _velocitySrc = createParticleTexture();
    _velocitySrc.name = "Velocity A";

    _velocityDst = createParticleTexture();
    _velocityDst.name = "Velocity B";

    _socialTemp = createParticleTexture();
    _socialTemp.name = "Social Temp";

    _socialQueueSrc = createQueueTexture(MAX_FORCE_STEPS);
    _socialQueueDst = createQueueTexture(MAX_FORCE_STEPS);

    _simulationMat.SetTexture("_SocialTemp", _socialTemp);

    Shader.SetGlobalTexture(POSITION_GLOBAL, _positionSrc);
    Shader.SetGlobalTexture(VELOCITY_GLOBAL, _velocitySrc);
    Shader.SetGlobalTexture(SOCIAL_FORCE_GLOBAL, _socialQueueSrc);

    buildSimulationCommands();

    RecalculateMeshesForParticles();

    LoadPresetEcosystem(_presetEcosystemSettings.ecosystemPreset);

    updateShaderData();

    _handActors.Fill(() => new HandActor(this));

    updateKeywords();
  }

  void Update() {
    if (_enableSpeciesDebugColors) {
      Color[] colors = new Color[MAX_SPECIES];
      colors.Fill(Color.black);
      colors[_debugSpeciesNumber] = Color.white;
      _particleMat.SetColorArray("_Colors", colors);
    }

    handleUserInput();

    updateShaderData();

    updateShaderDebug();

    if (_provider != null && handInfluenceEnabled) {
      _handActors[0].UpdateHand(Hands.Left);
      _handActors[1].UpdateHand(Hands.Right);
    }

    if (_simulationEnabled) {
      _currScaledTime += Time.deltaTime * _simulationTimescale;
      if (_dynamicTimestepEnabled) {
        while (_currSimulationTime < _currScaledTime) {
          _prevSimulationTime = _currSimulationTime;
          _currSimulationTime += 1.0f / _simulationFPS;

          stepSimulation(Mathf.InverseLerp(_currSimulationTime, _prevSimulationTime, _currScaledTime));

          if (_limitStepsPerFrame) {
            break;
          }
        }

        _displayBlock.SetFloat("_Lerp", Mathf.InverseLerp(_currSimulationTime, _prevSimulationTime, _currScaledTime));
      } else {
        _currSimulationTime = _prevSimulationTime = _currScaledTime;
        stepSimulation(1);
      }
    }

    if (_displayParticles) {
      displaySimulation();
    }
  }
  #endregion

  #region DESCRIPTION GENERATION
  public const int SPECIES_CAP_FOR_PRESETS = 10;

  public enum EcosystemPreset {
    RedMenace,
    Chase,
    Mitosis,
    BodyMind,
    Planets,
    Globules,
    Layers,
    Fluidy,
    TEST_OneParticle,
    TEST_TwoParticles,
    TEST_ThreeParticles,
    TEST_ThreeSpecies
  }


  private SimulationDescription getPresetDescription(EcosystemPreset preset) {
    var setting = _presetEcosystemSettings;
    Color[] colors = new Color[MAX_SPECIES];
    Vector4[,] socialData = new Vector4[MAX_SPECIES, MAX_SPECIES];
    Vector4[] speciesData = new Vector4[MAX_SPECIES];

    Vector3[] particlePositions = new Vector3[MAX_PARTICLES].Fill(() => Random.insideUnitSphere);
    Vector3[] particleVelocities = new Vector3[MAX_PARTICLES];
    int[] particleSpecies = new int[MAX_PARTICLES].Fill(-1);

    int currentSimulationSpeciesCount = MAX_SPECIES;
    int particlesToSimulate = MAX_PARTICLES;

    //Default colors are greyscale 0 to 1
    for (int i = 0; i < SPECIES_CAP_FOR_PRESETS; i++) {
      float p = i / (SPECIES_CAP_FOR_PRESETS - 1.0f);
      colors[i] = new Color(p, p, p, 1);
    }

    //Default social interactions are zero force with max range
    for (int i = 0; i < MAX_SPECIES; i++) {
      for (int j = 0; j < MAX_SPECIES; j++) {
        socialData[i, j] = new Vector2(0, setting.maxSocialRange);
      }
    }

    //Default species always have max drag, 0 extra social steps, and max collision force
    for (int i = 0; i < MAX_SPECIES; i++) {
      speciesData[i] = new Vector3(setting.minDrag, 0, setting.maxCollision);
    }

    //---------------------------------------------
    // Red Menace 
    //---------------------------------------------
    if (preset == EcosystemPreset.RedMenace) {
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

      float normalLove = setting.maxSocialForce * 0.04f;
      float fearOfRed = setting.maxSocialForce * -1.0f;
      float redLoveOfOthers = setting.maxSocialForce * 2.0f;
      float redLoveOfSelf = setting.maxSocialForce * 0.9f;

      float normalRange = setting.maxSocialRange * 0.4f;
      float fearRange = setting.maxSocialRange * 0.3f;
      float loveRange = setting.maxSocialRange * 0.3f;
      float redSelfRange = setting.maxSocialRange * 0.4f;

      for (int s = 0; s < SPECIES_CAP_FOR_PRESETS; s++) {
        speciesData[s] = new Vector3(Mathf.Lerp(setting.minDrag, setting.maxDrag, 0.1f),
                                     0,
                                     Mathf.Lerp(setting.minCollision, setting.maxCollision, 0.3f));

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
    }

    //---------------------------------------------
    // Chase 
    //---------------------------------------------
    else if (preset == EcosystemPreset.Chase) {
      for (int i = 0; i < SPECIES_CAP_FOR_PRESETS; i++) {
        speciesData[i] = new Vector3(setting.minDrag, 0, setting.minCollision);
        socialData[i, i] = new Vector2(setting.maxSocialForce * 0.1f, setting.maxSocialRange);
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

      float chase = 0.9f * setting.maxSocialForce;
      socialData[0, 1] = new Vector2(chase, setting.maxSocialRange);
      socialData[1, 2] = new Vector2(chase, setting.maxSocialRange);
      socialData[2, 3] = new Vector2(chase, setting.maxSocialRange);
      socialData[3, 4] = new Vector2(chase, setting.maxSocialRange);
      socialData[4, 5] = new Vector2(chase, setting.maxSocialRange);
      socialData[5, 6] = new Vector2(chase, setting.maxSocialRange);
      socialData[6, 7] = new Vector2(chase, setting.maxSocialRange);
      socialData[7, 8] = new Vector2(chase, setting.maxSocialRange);
      socialData[8, 9] = new Vector2(chase, setting.maxSocialRange);
      socialData[9, 0] = new Vector2(chase, setting.maxSocialRange);

      float flee = -0.6f * setting.maxSocialForce;
      float range = 0.8f * setting.maxSocialRange;
      socialData[0, 9] = new Vector2(flee, range);
      socialData[1, 0] = new Vector2(flee, range);
      socialData[2, 1] = new Vector2(flee, range);
      socialData[3, 2] = new Vector2(flee, range);
      socialData[4, 3] = new Vector2(flee, range);
      socialData[5, 4] = new Vector2(flee, range);
      socialData[6, 5] = new Vector2(flee, range);
      socialData[7, 6] = new Vector2(flee, range);
      socialData[8, 7] = new Vector2(flee, range);
      socialData[9, 8] = new Vector2(flee, range);
    }

    //---------------------------------------------
    // Mitosis 
    //---------------------------------------------
    else if (preset == EcosystemPreset.Mitosis) {
      for (int i = 0; i < SPECIES_CAP_FOR_PRESETS; i++) {
        speciesData[i] = new Vector3(Mathf.Lerp(setting.minDrag, setting.maxDrag, 0.1f),
                                     0,
                                     Mathf.Lerp(setting.minCollision, setting.maxCollision, 0.05f));

        for (var j = 0; j < SPECIES_CAP_FOR_PRESETS; j++) {
          float a = (j / (float)SPECIES_CAP_FOR_PRESETS * 0.9f) * setting.maxSocialForce * 1.0f;
          float b = (i / (float)SPECIES_CAP_FOR_PRESETS * 1.2f) * setting.maxSocialForce * 0.4f;

          socialData[i, j] = new Vector2(a - b, setting.maxSocialRange * 0.7f);
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
    }

    //---------------------------------------------
    // Planets 
    //---------------------------------------------
    else if (preset == EcosystemPreset.Planets) {
      currentSimulationSpeciesCount = 9;

      for (int i = 0; i < SPECIES_CAP_FOR_PRESETS; i++) {
        speciesData[i] = new Vector3(Mathf.Lerp(setting.minDrag, setting.maxDrag, 0.2f),
                                     3,
                                     Mathf.Lerp(setting.minCollision, setting.maxCollision, 0.2f));

        for (var j = 0; j < SPECIES_CAP_FOR_PRESETS; j++) {
          socialData[i, j] = new Vector2(-setting.maxSocialForce, setting.maxSocialRange * 0.5f);
        }
      }

      float f = setting.maxSocialForce * 0.6f;
      float r = setting.maxSocialRange * 0.8f;

      socialData[0, 0] = new Vector2(f, r); socialData[0, 1] = new Vector2(f, r); socialData[0, 2] = new Vector2(f, r);
      socialData[1, 1] = new Vector2(f, r); socialData[1, 0] = new Vector2(f, r); socialData[1, 2] = new Vector2(f, r);
      socialData[2, 2] = new Vector2(f, r); socialData[2, 0] = new Vector2(f, r); socialData[2, 1] = new Vector2(f, r);

      socialData[3, 3] = new Vector2(f, r); socialData[3, 4] = new Vector2(f, r); socialData[3, 5] = new Vector2(f, r);
      socialData[4, 4] = new Vector2(f, r); socialData[4, 3] = new Vector2(f, r); socialData[4, 5] = new Vector2(f, r);
      socialData[5, 5] = new Vector2(f, r); socialData[5, 3] = new Vector2(f, r); socialData[5, 4] = new Vector2(f, r);

      socialData[6, 6] = new Vector2(f, r); socialData[6, 7] = new Vector2(f, r); socialData[6, 8] = new Vector2(f, r);
      socialData[7, 7] = new Vector2(f, r); socialData[7, 8] = new Vector2(f, r); socialData[7, 6] = new Vector2(f, r);
      socialData[8, 8] = new Vector2(f, r); socialData[8, 6] = new Vector2(f, r); socialData[8, 7] = new Vector2(f, r);

      colors[0] = new Color(0.9f, 0.0f, 0.0f);
      colors[1] = new Color(0.9f, 0.5f, 0.0f);
      colors[2] = new Color(0.4f, 0.2f, 0.1f);

      colors[3] = new Color(0.8f, 0.8f, 0.1f);
      colors[4] = new Color(0.1f, 0.8f, 0.1f);
      colors[5] = new Color(0.4f, 0.3f, 0.1f);

      colors[6] = new Color(0.0f, 0.0f, 0.9f);
      colors[7] = new Color(0.4f, 0.0f, 0.9f);
      colors[8] = new Color(0.2f, 0.1f, 0.5f);
    }

    //---------------------------------------------
    // This is a controlled test scenario with
    // only ONE particle! (Yea, boring, I know).
    //---------------------------------------------
    else if (preset == EcosystemPreset.TEST_OneParticle) {
      currentSimulationSpeciesCount = 1;
      particlesToSimulate = 1;

      int ONE_species = 0;
      int ONE_steps = 1;
      float ONE_drag = 0.1f;
      float ONE_collision = 0.0f;
      float ONE_force = 0.0f;
      float ONE_range = 0.0f;
      float ONE_red = 0.0f;
      float ONE_green = 0.0f;
      float ONE_blue = 0.0f;

      colors[ONE_species] = new Color(ONE_red, ONE_green, ONE_blue);
      speciesData[ONE_species] = new Vector3(ONE_drag, ONE_steps, ONE_collision);
      socialData[ONE_species, ONE_species] = new Vector2(setting.maxSocialForce * ONE_force, setting.maxSocialRange * ONE_range);

      particlePositions[0] = new Vector3(0.0f, 0.0f, 0.0f);
    }

    //---------------------------------------------
    // This is a controlled test scenario with
    // two particles that have mutual attraction
    //---------------------------------------------
    else if (preset == EcosystemPreset.TEST_TwoParticles) {
      currentSimulationSpeciesCount = 2;
      particlesToSimulate = 2;

      int Test2_ONE_species = 0;
      int Test2_ONE_steps = 0;
      float Test2_ONE_drag = 0.1f;
      float Test2_ONE_collision = 0.02f;
      float Test2_ONE_force = 0.0f;
      float Test2_ONE_range = 0.5f;
      float Test2_ONE_red = 1.0f;
      float Test2_ONE_green = 0.0f;
      float Test2_ONE_blue = 0.0f;
      float Test2_ONE_love = 0.0005f;

      int Test2_TWO_species = 1;
      int Test2_TWO_steps = 0;
      float Test2_TWO_drag = 0.1f;
      float Test2_TWO_collision = 0.02f;
      float Test2_TWO_force = 0.0f;
      float Test2_TWO_range = 0.5f;
      float Test2_TWO_red = 0.0f;
      float Test2_TWO_green = 1.0f;
      float Test2_TWO_blue = 0.0f;
      float Test2_TWO_love = 0.0005f;

      colors[Test2_ONE_species] = new Color(Test2_ONE_red, Test2_ONE_green, Test2_ONE_blue);
      colors[Test2_TWO_species] = new Color(Test2_TWO_red, Test2_TWO_green, Test2_TWO_blue);

      speciesData[Test2_ONE_species] = new Vector3(Test2_ONE_drag, Test2_ONE_steps, Test2_ONE_collision);
      speciesData[Test2_TWO_species] = new Vector3(Test2_TWO_drag, Test2_TWO_steps, Test2_TWO_collision);

      socialData[Test2_ONE_species, Test2_ONE_species] = new Vector2(Test2_ONE_force, Test2_ONE_range);
      socialData[Test2_ONE_species, Test2_TWO_species] = new Vector2(Test2_ONE_love, Test2_ONE_range);

      socialData[Test2_TWO_species, Test2_TWO_species] = new Vector2(Test2_TWO_force, Test2_TWO_range);
      socialData[Test2_TWO_species, Test2_ONE_species] = new Vector2(Test2_TWO_love, Test2_ONE_range);

      particlePositions[0] = new Vector3(-0.2f, 0.0f, 0.0f);
      particlePositions[1] = new Vector3(0.2f, 0.0f, 0.0f);

      //float selfLove   	=  0.3f;
      //float selfRange  	=  0.3f;

      particleVelocities[0] = Vector3.zero;
      particleVelocities[1] = Vector3.zero;

      particleSpecies[0] = Test2_ONE_species;
      particleSpecies[1] = Test2_TWO_species;
    }

    //---------------------------------------------
    // This is a controlled test scenario with
    // three particles that have mutual attraction
    //---------------------------------------------
    else if (preset == EcosystemPreset.TEST_ThreeParticles) {
      currentSimulationSpeciesCount = 3;
      particlesToSimulate = 3;

      colors[0] = new Color(0.7f, 0.2f, 0.2f);
      colors[1] = new Color(0.6f, 0.6f, 0.0f);
      colors[2] = new Color(0.1f, 0.2f, 0.7f);

      for (int s = 0; s < currentSimulationSpeciesCount; s++) {
        speciesData[s] = new Vector3(0.1f, 1, 0.1f);

        float epsilon = 0.0001f; //Alex: this is a bandaid for a side effect

        for (int o = 0; o < currentSimulationSpeciesCount; o++) {
          socialData[s, o] = new Vector2(0.0f, epsilon);
        }
      }

      float nextLove = 0.001f;
      float nextRange = 0.5f;

      socialData[0, 1] = new Vector2(nextLove, nextRange);
      socialData[1, 2] = new Vector2(nextLove, nextRange);
      socialData[2, 0] = new Vector2(nextLove, nextRange);

      particlePositions[0] = new Vector3(-0.2f, -0.17f, 0.0f);
      particlePositions[1] = new Vector3(0.2f, -0.17f, 0.0f);
      particlePositions[2] = new Vector3(0.0f, 0.20f, 0.0f);

      for (int p = 0; p < particlesToSimulate; p++) {
        particleSpecies[p] = p;
        particleVelocities[p] = Vector3.zero;
      }
    }


    //----------------------------------------------------------------
    // This is a controlled test scenario which is the same as
    // Test3 in terms of species, but it has lots of particles
    //----------------------------------------------------------------
    else if (preset == EcosystemPreset.TEST_ThreeSpecies) {
      currentSimulationSpeciesCount = 3;
      particlesToSimulate = 3000;

      colors[0] = new Color(0.7f, 0.2f, 0.2f);
      colors[1] = new Color(0.4f, 0.4f, 0.0f);
      colors[2] = new Color(0.1f, 0.2f, 0.7f);

      for (int s = 0; s < currentSimulationSpeciesCount; s++) {
        speciesData[s] = new Vector3(0.1f, 1, 0.01f);
      }

      float Test4_selfForce = 0.001f;
      float Test4_selfRange = 0.3f;

      float Test4_loveForce = 0.002f;
      float Test4_loveRange = 0.5f;

      float Test4_hateForce = -0.0022f;
      float Test4_hateRange = 0.8f;

      socialData[0, 0] = new Vector2(Test4_selfForce, Test4_selfRange);
      socialData[1, 1] = new Vector2(Test4_selfForce, Test4_selfRange);
      socialData[2, 2] = new Vector2(Test4_selfForce, Test4_selfRange);

      socialData[0, 1] = new Vector2(Test4_loveForce, Test4_loveRange);
      socialData[1, 2] = new Vector2(Test4_loveForce, Test4_loveRange);
      socialData[2, 0] = new Vector2(Test4_loveForce, Test4_loveRange);

      socialData[0, 2] = new Vector2(Test4_hateForce, Test4_hateRange);
      socialData[1, 0] = new Vector2(Test4_hateForce, Test4_hateRange);
      socialData[2, 1] = new Vector2(Test4_hateForce, Test4_hateRange);

      for (int p = 0; p < particlesToSimulate; p++) {
        float fraction = (float)p / (float)particlesToSimulate;
        if (fraction < (1.0f / currentSimulationSpeciesCount)) { particleSpecies[p] = 0; } else if (fraction < (2.0f / currentSimulationSpeciesCount)) { particleSpecies[p] = 1; } else { particleSpecies[p] = 2; }

        particleVelocities[p] = Vector3.zero;
      }
    }

    //-----------------------------------------------------------------------------------
    // This is a test to see if we can simulate (somewhat and somehow) a bilayer lipid!
    //-----------------------------------------------------------------------------------
    else if (preset == EcosystemPreset.Layers) {
      currentSimulationSpeciesCount = 4;

      int s0 = 0;
      int s1 = 1;
      int s2 = 2;
      int s3 = 3;

      int rez = 30;

      particlesToSimulate = currentSimulationSpeciesCount * rez * rez;

      colors[s0] = new Color(0.3f, 0.4f, 0.6f);
      colors[s1] = new Color(0.3f, 0.2f, 0.1f);
      colors[s2] = new Color(0.7f, 0.6f, 0.5f);
      colors[s3] = new Color(0.5f, 0.4f, 0.3f);

      float drag = 0.1f;
      float collision = 0.01f;
      float epsilon = 0.0001f; //Alex: this is a bandaid for a side effect

      for (int s = 0; s < currentSimulationSpeciesCount; s++) {
        int steps = s * 2;
        speciesData[s] = new Vector3(drag, steps, collision);

        for (int o = 0; o < currentSimulationSpeciesCount; o++) {
          socialData[s, o] = new Vector2(0.0f, epsilon);
        }
      }

      float minForce = -0.002f;
      float maxForce = 0.002f;

      float minRange = 0.01f;
      float maxRange = 0.6f;

      float f_0_0 = minForce + (maxForce - minForce) * Random.value;
      float f_0_1 = minForce + (maxForce - minForce) * Random.value;
      float f_0_2 = minForce + (maxForce - minForce) * Random.value;
      float f_0_3 = minForce + (maxForce - minForce) * Random.value;

      float f_1_0 = minForce + (maxForce - minForce) * Random.value;
      float f_1_1 = minForce + (maxForce - minForce) * Random.value;
      float f_1_2 = minForce + (maxForce - minForce) * Random.value;
      float f_1_3 = minForce + (maxForce - minForce) * Random.value;

      float f_2_0 = minForce + (maxForce - minForce) * Random.value;
      float f_2_1 = minForce + (maxForce - minForce) * Random.value;
      float f_2_2 = minForce + (maxForce - minForce) * Random.value;
      float f_2_3 = minForce + (maxForce - minForce) * Random.value;

      float f_3_0 = minForce + (maxForce - minForce) * Random.value;
      float f_3_1 = minForce + (maxForce - minForce) * Random.value;
      float f_3_2 = minForce + (maxForce - minForce) * Random.value;
      float f_3_3 = minForce + (maxForce - minForce) * Random.value;



      float r_0_0 = minRange + (maxRange - minRange) * Random.value;
      float r_0_1 = minRange + (maxRange - minRange) * Random.value;
      float r_0_2 = minRange + (maxRange - minRange) * Random.value;
      float r_0_3 = minRange + (maxRange - minRange) * Random.value;

      float r_1_0 = minRange + (maxRange - minRange) * Random.value;
      float r_1_1 = minRange + (maxRange - minRange) * Random.value;
      float r_1_2 = minRange + (maxRange - minRange) * Random.value;
      float r_1_3 = minRange + (maxRange - minRange) * Random.value;

      float r_2_0 = minRange + (maxRange - minRange) * Random.value;
      float r_2_1 = minRange + (maxRange - minRange) * Random.value;
      float r_2_2 = minRange + (maxRange - minRange) * Random.value;
      float r_2_3 = minRange + (maxRange - minRange) * Random.value;

      float r_3_0 = minRange + (maxRange - minRange) * Random.value;
      float r_3_1 = minRange + (maxRange - minRange) * Random.value;
      float r_3_2 = minRange + (maxRange - minRange) * Random.value;
      float r_3_3 = minRange + (maxRange - minRange) * Random.value;




      Debug.Log("");
      Debug.Log("data  -----------------------------------------------------");

      Debug.Log("float f_0_0 = " + f_0_0 + "f; ");
      Debug.Log("float f_0_1 = " + f_0_1 + "f; ");
      Debug.Log("float f_0_2 = " + f_0_2 + "f; ");
      Debug.Log("float f_0_3 = " + f_0_3 + "f; ");

      Debug.Log("float f_1_0 = " + f_1_0 + "f; ");
      Debug.Log("float f_1_1 = " + f_1_1 + "f; ");
      Debug.Log("float f_1_2 = " + f_1_2 + "f; ");
      Debug.Log("float f_1_3 = " + f_1_3 + "f; ");

      Debug.Log("float f_2_0 = " + f_2_0 + "f; ");
      Debug.Log("float f_2_1 = " + f_2_1 + "f; ");
      Debug.Log("float f_2_2 = " + f_2_2 + "f; ");
      Debug.Log("float f_2_3 = " + f_2_3 + "f; ");

      Debug.Log("float f_3_0 = " + f_3_0 + "f; ");
      Debug.Log("float f_3_1 = " + f_3_1 + "f; ");
      Debug.Log("float f_3_2 = " + f_3_2 + "f; ");
      Debug.Log("float f_3_3 = " + f_3_3 + "f; ");


      Debug.Log("float r_0_0 = " + r_0_0 + "f; ");
      Debug.Log("float r_0_1 = " + r_0_1 + "f; ");
      Debug.Log("float r_0_2 = " + r_0_2 + "f; ");
      Debug.Log("float r_0_3 = " + r_0_3 + "f; ");

      Debug.Log("float r_1_0 = " + r_1_0 + "f; ");
      Debug.Log("float r_1_1 = " + r_1_1 + "f; ");
      Debug.Log("float r_1_2 = " + r_1_2 + "f; ");
      Debug.Log("float r_1_3 = " + r_1_3 + "f; ");

      Debug.Log("float r_2_0 = " + r_2_0 + "f; ");
      Debug.Log("float r_2_1 = " + r_2_1 + "f; ");
      Debug.Log("float r_2_2 = " + r_2_2 + "f; ");
      Debug.Log("float r_2_3 = " + r_2_3 + "f; ");

      Debug.Log("float r_3_0 = " + r_3_0 + "f; ");
      Debug.Log("float r_3_1 = " + r_3_1 + "f; ");
      Debug.Log("float r_3_2 = " + r_3_2 + "f; ");
      Debug.Log("float r_3_3 = " + r_3_3 + "f; ");

      /*
      float f_0_0 = -0.001361714f; 
      float f_0_1 = -0.001863675f; 
      float f_0_2 = -0.0006116494f; 
      float f_0_3 = -0.0009556326f; 
      float f_1_0 = -0.000519999f; 
      float f_1_1 = 0.0006196692f; 
      float f_1_2 = -0.0007936339f; 
      float f_1_3 = -0.00107222f; 
      float f_2_0 = -0.001001807f; 
      float f_2_1 = 0.0007801288f; 
      float f_2_2 = -0.001814131f; 
      float f_2_3 = -0.0005873627f; 
      float f_3_0 = 0.0005874083f; 
      float f_3_1 = 0.0008533328f; 
      float f_3_2 = 0.001345f; 
      float f_3_3 = -0.0003365405f; 


      float r_0_0 = 0.2570884f; 
      float r_0_1 = 0.5648767f; 
      float r_0_2 = 0.3039016f; 
      float r_0_3 = 0.4649104f; 
      float r_1_0 = 0.2592408f; 
      float r_1_1 = 0.1084508f; 
      float r_1_2 = 0.05279962f; 
      float r_1_3 = 0.1394664f; 
      float r_2_0 = 0.4481683f; 
      float r_2_1 = 0.2992772f; 
      float r_2_2 = 0.01796358f; 
      float r_2_3 = 0.04451307f; 
      float r_3_0 = 0.5427676f; 
      float r_3_1 = 0.1953885f; 
      float r_3_2 = 0.05868421f; 
      float r_3_3 = 0.03309977f; 
      */


      socialData[s0, s0] = new Vector2(f_0_0, r_0_0);
      socialData[s0, s1] = new Vector2(f_0_1, r_0_1);
      socialData[s0, s2] = new Vector2(f_0_2, r_0_2);
      socialData[s0, s3] = new Vector2(f_0_3, r_0_3);

      socialData[s1, s0] = new Vector2(f_1_0, r_1_0);
      socialData[s1, s1] = new Vector2(f_1_1, r_1_1);
      socialData[s1, s2] = new Vector2(f_1_2, r_1_2);
      socialData[s1, s3] = new Vector2(f_1_3, r_1_3);

      socialData[s2, s0] = new Vector2(f_2_0, r_2_0);
      socialData[s2, s1] = new Vector2(f_2_1, r_2_1);
      socialData[s2, s2] = new Vector2(f_2_2, r_2_2);
      socialData[s2, s3] = new Vector2(f_2_3, r_2_3);

      socialData[s3, s0] = new Vector2(f_3_0, r_3_0);
      socialData[s3, s1] = new Vector2(f_3_1, r_3_1);
      socialData[s3, s2] = new Vector2(f_3_2, r_3_2);
      socialData[s3, s3] = new Vector2(f_3_3, r_3_3);

      float width = 0.7f;
      float height = 0.7f;
      float depth = 0.3f;
      float jitter = 0.0001f;

      int p = 0;

      for (int i = 0; i < rez; i++) {
        float xFraction = (float)i / (float)rez;
        float x = -width * 0.5f + xFraction * width;

        for (int j = 0; j < currentSimulationSpeciesCount; j++) {
          float yFraction = (float)j / (float)currentSimulationSpeciesCount;
          float y = -depth * 0.5f + yFraction * depth;

          for (int k = 0; k < rez; k++) {
            float zFraction = (float)k / (float)rez;
            float z = -height * 0.5f + zFraction * height;

            particleSpecies[p] = j;

            particlePositions[p] = new Vector3
            (
              x + Random.value * jitter,
              y + Random.value * jitter,
              z + Random.value * jitter
            );
            p++;
          }
        }
      }
    }

    //------------------------------------------------
    // Bodymind!
    //------------------------------------------------
    else if (preset == EcosystemPreset.BodyMind) {
      currentSimulationSpeciesCount = 3;

      int blue = 0;
      int purple = 1;
      int black = 2;

      float blueDrag = 0.0f;
      float purpleDrag = 0.0f;
      float blackDrag = 0.0f;

      float blueCollision = 0.0f;
      float purpleCollision = 0.0f;
      float blackCollision = 0.0f;

      int blueSteps = 0;
      int purpleSteps = 0;
      int blackSteps = 0;

      float blueToBlueForce = 0.1f;
      float blueToBlueRange = 1.0f;

      float purpleToPurpleForce = 0.2f;
      float purpleToPurpleRange = 1.0f;

      float blueToPurpleForce = 1.0f;
      float blueToPurpleRange = 1.0f;

      float purpleToBlueForce = -1.0f;
      float purpleToBlueRange = 0.4f;

      float blackToBlueForce = 0.2f;
      float blackToBlueRange = 1.0f;

      float blackToPurpleForce = 0.2f;
      float blackToPurpleRange = 1.0f;

      colors[blue] = new Color(0.2f, 0.2f, 0.8f);
      colors[purple] = new Color(0.3f, 0.2f, 0.8f);
      colors[black] = new Color(0.1f, 0.0f, 0.4f);

      float bd = Mathf.Lerp(setting.minDrag, setting.maxDrag, blueDrag);
      float pd = Mathf.Lerp(setting.minDrag, setting.maxDrag, purpleDrag);
      float gd = Mathf.Lerp(setting.minDrag, setting.maxDrag, blackDrag);

      float bc = Mathf.Lerp(setting.minCollision, setting.maxCollision, blueCollision);
      float pc = Mathf.Lerp(setting.minCollision, setting.maxCollision, purpleCollision);
      float gc = Mathf.Lerp(setting.minCollision, setting.maxCollision, blackCollision);

      speciesData[blue] = new Vector3(bd, blueSteps, bc);
      speciesData[purple] = new Vector3(pd, purpleSteps, pc);
      speciesData[black] = new Vector3(gd, blackSteps, gc);

      socialData[blue, blue] = new Vector2(setting.maxSocialForce * blueToBlueForce, setting.maxSocialRange * blueToBlueRange);
      socialData[purple, purple] = new Vector2(setting.maxSocialForce * purpleToPurpleForce, setting.maxSocialRange * purpleToPurpleRange);
      socialData[blue, purple] = new Vector2(setting.maxSocialForce * blueToPurpleForce, setting.maxSocialRange * blueToPurpleRange);
      socialData[purple, blue] = new Vector2(setting.maxSocialForce * purpleToBlueForce, setting.maxSocialRange * purpleToBlueRange);
      socialData[black, blue] = new Vector2(setting.maxSocialForce * blackToBlueForce, setting.maxSocialRange * blackToBlueRange);
      socialData[black, purple] = new Vector2(setting.maxSocialForce * blackToPurpleForce, setting.maxSocialRange * blackToPurpleRange);
    }

    //------------------------------------------------
    // Globules
    //------------------------------------------------
    else if (preset == EcosystemPreset.Globules) {
      currentSimulationSpeciesCount = 3;

      for (int i = 0; i < SPECIES_CAP_FOR_PRESETS; i++) {
        speciesData[i] = new Vector3(Mathf.Lerp(setting.minDrag, setting.maxDrag, 0.2f),
                                     1,
                                     Mathf.Lerp(setting.minCollision, setting.maxCollision, 0.2f));

      }

      float globuleChaseForce = setting.maxSocialForce * 0.2f;
      float globuleChaseRange = setting.maxSocialRange * 0.8f;

      float globuleFleeForce = -setting.maxSocialForce * 0.3f;
      float globuleFleeRange = setting.maxSocialRange * 0.4f;

      float globuleAvoidForce = -setting.maxSocialForce * 0.2f;
      float globuleAvoidRange = setting.maxSocialRange * 0.1f;


      socialData[0, 1] = new Vector2(globuleChaseForce * 1.5f, globuleChaseRange);
      socialData[1, 2] = new Vector2(globuleChaseForce, globuleChaseRange);
      socialData[2, 0] = new Vector2(globuleChaseForce, globuleChaseRange);

      socialData[1, 0] = new Vector2(globuleFleeForce, globuleFleeRange);
      socialData[2, 1] = new Vector2(globuleFleeForce, globuleFleeRange);
      socialData[0, 2] = new Vector2(globuleFleeForce, globuleFleeRange);

      socialData[0, 0] = new Vector2(globuleAvoidForce, globuleAvoidRange);
      socialData[1, 1] = new Vector2(globuleAvoidForce, globuleAvoidRange);
      socialData[2, 2] = new Vector2(globuleAvoidForce, globuleAvoidRange);

      colors[0] = new Color(0.1f, 0.1f, 0.3f);
      colors[1] = new Color(0.3f, 0.2f, 0.5f);
      colors[2] = new Color(0.4f, 0.1f, 0.1f);
    }

    //------------------------------------------------
    // Fluidy
    //------------------------------------------------
    else if (preset == EcosystemPreset.Fluidy) {
      for (var i = 0; i < SPECIES_CAP_FOR_PRESETS; i++) {
        for (var j = 0; j < SPECIES_CAP_FOR_PRESETS; j++) {
          socialData[i, j] = new Vector2(0, 0);
        }

        socialData[i, i] = new Vector2(0.2f * setting.maxSocialForce, setting.maxSocialRange * 0.1f);
      }

      for (var i = 0; i < SPECIES_CAP_FOR_PRESETS; i++) {
        for (var j = i + 1; j < SPECIES_CAP_FOR_PRESETS; j++) {
          socialData[i, j] = new Vector2(0.15f * setting.maxSocialForce, setting.maxSocialRange);
          socialData[j, i] = new Vector2(-0.1f * setting.maxSocialForce, setting.maxSocialRange * 0.3f);
        }
      }

      for (int i = 0; i < MAX_PARTICLES; i++) {
        float percent = Mathf.InverseLerp(0, MAX_PARTICLES, i);
        float percent2 = percent * 12.123123f + Random.value;
        particlePositions[i] = new Vector3(Mathf.Lerp(-1, 1, percent2 - (int)percent2), Mathf.Lerp(-1, 1, percent), Random.Range(-0.01f, 0.01f));
        particleSpecies[i] = Mathf.FloorToInt(percent * currentSimulationSpeciesCount);
      }
    }


    SimulationDescription description = new SimulationDescription();
    description.name = preset.ToString();
    description.socialData = new SocialData[MAX_SPECIES, MAX_SPECIES];
    description.speciesData = new SpeciesData[MAX_SPECIES];
    description.toSpawn = new List<ParticleSpawn>();

    for (int i = 0; i < MAX_SPECIES; i++) {
      for (int j = 0; j < MAX_SPECIES; j++) {
        description.socialData[i, j] = new SocialData() {
          socialForce = socialData[i, j].x,
          socialRange = socialData[i, j].y
        };
      }

      description.speciesData[i] = new SpeciesData() {
        drag = speciesData[i].x,
        forceSteps = Mathf.RoundToInt(speciesData[i].y),
        collisionForce = speciesData[i].z,
        color = colors[i]
      };
    }

    for (int i = 0; i < particlesToSimulate; i++) {
      int species = particleSpecies[i];
      if (species < 0) {
        species = (i % currentSimulationSpeciesCount);
      }

      description.toSpawn.Add(new ParticleSpawn() {
        position = particlePositions[i],
        velocity = particleVelocities[i],
        species = species
      });
    }

    return description;
  }

  private SimulationDescription getRandomEcosystemDescription() {
    Random.InitState(Time.realtimeSinceStartup.GetHashCode());

    var gen = GetComponent<NameGenerator>();
    string name;
    if (gen == null) {
      name = Random.Range(0, 1000).ToString();
    } else {
      name = gen.GenerateName();
    }
    Debug.Log(name);

    return getRandomEcosystemDescription(name);
  }

  private SimulationDescription getRandomEcosystemDescription(string seed) {
    var setting = _randomEcosystemSettings;

    SimulationDescription desc = new SimulationDescription() {
      socialData = new SocialData[MAX_SPECIES, MAX_SPECIES],
      speciesData = new SpeciesData[MAX_SPECIES],
      toSpawn = new List<ParticleSpawn>()
    };

    Random.InitState(seed.GetHashCode());

    for (int s = 0; s < MAX_SPECIES; s++) {
      for (int o = 0; o < MAX_SPECIES; o++) {
        desc.socialData[s, o] = new SocialData() {
          socialForce = Random.Range(-setting.maxSocialForce, setting.maxSocialForce),
          socialRange = Random.value * setting.maxSocialRange
        };
      }
    }
    
    for (int i = 0; i < MAX_SPECIES; i++) {
      desc.speciesData[i] = new SpeciesData() {
        drag = Random.Range(setting.minDrag, setting.maxDrag),
        forceSteps = Random.Range(0, setting.maxForceSteps),
        collisionForce = Random.Range(setting.minCollision, setting.maxCollision)
      };
    }

    // Perform color randomization last so that it has no effect on particle interaction.
    randomizeDescriptionColors(desc);

    return desc;
  }

  private void randomizeDescriptionColors(SimulationDescription desc) {
    var setting = _randomEcosystemSettings;

    List<Color> colors = new List<Color>();
    for (int i = 0; i < MAX_SPECIES; i++) {
      Color newColor;
      int maxTries = 1000;
      while (true) {
        float h = Random.Range(setting.minHue, setting.maxHue);
        float s = Random.Range(setting.minSaturation, setting.maxSaturation);
        float v = Random.Range(setting.minValue, setting.maxValue);

        bool alreadyExists = false;
        foreach (var color in colors) {
          float existingH, existingS, existingV;
          Color.RGBToHSV(color, out existingH, out existingS, out existingV);

          if (Mathf.Abs(h - existingH) < setting.randomColorThreshold &&
              Mathf.Abs(s - existingS) < setting.randomColorThreshold) {
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
      desc.speciesData[i].color = newColor;
    }
  }
  #endregion

  #region RESET LOGIC
  private SimulationDescription _currentDescription = null;

  public struct SocialData {
    public float socialForce;
    public float socialRange;
  }

  public struct SpeciesData {
    public int forceSteps;
    public float drag;
    public float collisionForce;
    public Color color;
  }

  public struct ParticleSpawn {
    public Vector3 position;
    public Vector3 velocity;
    public int species;
  }

  public struct SpeciesRect {
    public int x, y, width, height;
    public int species;
  }

  public class SimulationDescription {
    public string name;
    public SocialData[,] socialData;
    public SpeciesData[] speciesData;
    public List<ParticleSpawn> toSpawn;
  }

  /// <summary>
  /// Restarts the simulation to whatever state it was in when it was
  /// most recently restarted.
  /// </summary>
  public void RestartSimulation() {

  }

  /// <summary>
  /// Restarts the simulation using a specific preset to choose the
  /// initial conditions.
  /// </summary>
  public void RestartSimulation(EcosystemPreset preset) {
    RestartSimulation(getPresetDescription(preset));
  }

  /// <summary>
  /// Restarts the simulation using a random description.
  /// </summary>
  public void RandomizeSimulation(bool forcePositionReset = true) {
    RestartSimulation(getRandomEcosystemDescription(), forcePositionReset);
  }

  /// <summary>
  /// Restarts the simulation using a random description calculated using
  /// the seed value.  The same seed value should always result in the same
  /// simulation description.
  /// </summary>
  public void RandomizeSimulation(string seed, bool forcePositionReset = true) {
    RestartSimulation(getRandomEcosystemDescription(seed), forcePositionReset);
  }

  /// <summary>
  /// Randomizes the simulation colors of the current simulation.
  /// </summary>
  public void RandomizeSimulationColors() {
  }

  /// <summary>
  /// Restar the simulation using the given description to describe the
  /// initial state of the simulation.
  /// </summary>
  public void RestartSimulation(SimulationDescription simulationDescription, bool forcePositionReset = true) {
    bool isLayoutDifferent = true;

    if (_currentDescription != null && !forcePositionReset) {
      List<int> originalSpeciesMap = new List<int>();
      _currentDescription.toSpawn.Query().Select(t => t.species).FillList(originalSpeciesMap);
      originalSpeciesMap.Sort();

      List<int> newSpeciesMap = new List<int>();
      simulationDescription.toSpawn.Query().Select(t => t.species).FillList(newSpeciesMap);
      newSpeciesMap.Sort();

      if (originalSpeciesMap.Count == newSpeciesMap.Count) {
        isLayoutDifferent = false;
        for (int i = 0; i < originalSpeciesMap.Count; i++) {
          if (originalSpeciesMap[i] != newSpeciesMap[i]) {
            isLayoutDifferent = true;
            break;
          }
        }
      }
    }

    List<SpeciesRect> layout = new List<SpeciesRect>();

    bool isUsingOptimizedLayout = tryCalculateOptimizedLayout(simulationDescription.toSpawn, layout);

    if (!isUsingOptimizedLayout) {
      calculateLayoutGeneral(simulationDescription.toSpawn, layout);
    }

    if (isLayoutDifferent) {
      resetParticleTextures(layout, simulationDescription.toSpawn);

      resetRenderMeshes(simulationDescription, layout);
    }

    resetBlitMeshes(layout, simulationDescription.speciesData, simulationDescription.socialData, isUsingOptimizedLayout);

    //TODO: more shader constants here
    _currScaledTime = 0;
    _currSimulationTime = 0;
    _prevSimulationTime = 0;

    //TODO: reset command buffer state correctly
    _commandIndex = 0;
  }

  private bool tryCalculateOptimizedLayout(List<ParticleSpawn> toSpawn, List<SpeciesRect> layout) {
    //TODO: implement optimize layout
    return false;
  }

  private void calculateLayoutGeneral(List<ParticleSpawn> toSpawn, List<SpeciesRect> layout) {
    toSpawn.Sort((a, b) => a.species.CompareTo(b.species));

    int existing = toSpawn.Count;
    int speciesCount = toSpawn.Query().CountUnique(t => t.species);
    int width = Mathf.RoundToInt(Mathf.Sqrt(speciesCount));

    int[,] speciesMap = new int[_textureDimension, _textureDimension].Fill(-1);
    layout.Clear();

    int index = 0;
    for (int c = 0; c < width; c++) {
      int dx0 = _textureDimension * c / width;
      int dx1 = _textureDimension * (c + 1) / width;

      for (int dx = dx0; dx < dx1; dx++) {
        for (int dy = 0; dy < _textureDimension; dy++) {
          speciesMap[dx, dy] = toSpawn[index].species;

          layout.Add(new SpeciesRect() {
            x = dx,
            y = dy,
            width = 1,
            height = 1,
            species = speciesMap[dx, dy]
          });

          index++;
          if (index == toSpawn.Count) {
            goto speciesMapFull;
          }
        }
      }
    }
    speciesMapFull:

    bool didCollapse;
    do {
      didCollapse = false;

      for (int i = 0; i < layout.Count - 1; i++) {
        SpeciesRect rect0 = layout[i];
        SpeciesRect rect1 = layout[i];
        if (rect0.species != rect1.species) {
          continue;
        }

        if (rect0.y == rect1.y &&
            rect0.x + rect0.width == rect1.x &&
            rect0.height == rect1.height) {
          rect0.width += rect1.width;
          layout[i] = rect0;
          layout.RemoveAt(i + 1);
          i--;
          continue;
        }

        if (rect0.x == rect1.x &&
           rect0.y + rect0.height == rect1.y &&
           rect0.width == rect1.width) {
          rect0.height += rect1.height;
          layout[i] = rect0;
          layout.RemoveAt(i + 1);
          i--;
          continue;
        }
      }
    } while (didCollapse);

    return;
  }

  private void resetParticleTextures(List<SpeciesRect> layout, List<ParticleSpawn> toSpawn) {
    TextureFormat format;
    switch (_textureFormat) {
      case RenderTextureFormat.ARGBFloat:
        format = TextureFormat.RGBAFloat;
        break;
      case RenderTextureFormat.ARGBHalf:
        format = TextureFormat.RGBAHalf;
        break;
      default:
        throw new System.Exception("Only ARGBFLoat or ARGBHalf are supported currently!");
    }

    GL.LoadPixelMatrix(0, _textureDimension, _textureDimension, 0);

    Color[] positionColors = new Color[_textureDimension * _textureDimension];
    Color[] velocityColors = new Color[_textureDimension * _textureDimension];
    var speciesMap = new IEnumerator<ParticleSpawn>[MAX_SPECIES];
    for (int i = 0; i < MAX_SPECIES; i++) {
      speciesMap[i] = toSpawn.Query().Where(t => t.species == i).GetEnumerator();
    }

    foreach (var rect in layout) {
      var enumerator = speciesMap[rect.species];
      for (int dx = rect.x; dx < rect.x + rect.width; dx++) {
        for (int dy = rect.y; dy < rect.y + rect.height; dy++) {
          enumerator.MoveNext();
          positionColors[dx + dy * _textureDimension] = (Vector4)enumerator.Current.position;
          velocityColors[dx + dy * _textureDimension] = (Vector4)enumerator.Current.velocity;
        }
      }
    }

    Texture2D tex;
    tex = new Texture2D(_textureDimension, _textureDimension, format, mipmap: false, linear: true);
    tex.SetPixels(positionColors);
    tex.Apply();

    blitCopy(tex, _positionSrc);
    DestroyImmediate(tex);

    tex = new Texture2D(_textureDimension, _textureDimension, format, mipmap: false, linear: true);
    tex.SetPixels(velocityColors);
    tex.Apply();

    blitCopy(tex, _velocitySrc);
    DestroyImmediate(tex);
  }

  private void uploadSpeciesColors(Color[] colors) {
    _displayBlock.SetVectorArray("_SpeciesColors", colors);
  }

  private void resetRenderMeshes(SimulationDescription desc, List<SpeciesRect> layout) {
    _renderMeshes.Clear();

    var sourceVerts = _particleMesh.vertices;
    var sourceTris = _particleMesh.triangles;

    List<Vector3> bakedVerts = new List<Vector3>();
    List<int> bakedTris = new List<int>();
    List<Vector4> bakedUvs = new List<Vector4>();

    var speciesMap = new IEnumerator<ParticleSpawn>[MAX_SPECIES];
    for (int i = 0; i < MAX_SPECIES; i++) {
      speciesMap[i] = desc.toSpawn.Query().Where(t => t.species == i).GetEnumerator();
    }

    Mesh bakedMesh = null;
    foreach (var rect in layout) {
      var enumerator = speciesMap[rect.species];
      for (int dx = rect.x; dx < rect.x + rect.width; dx++) {
        for (int dy = rect.y; dy < rect.y + rect.height; dy++) {

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
            _renderMeshes.Add(bakedMesh);
          }

          bakedVerts.AddRange(sourceVerts);
          bakedTris.AddRange(sourceTris);

          for (int k = 0; k < sourceVerts.Length; k++) {
            bakedUvs.Add(new Vector4((dx + 0.5f) / _textureDimension, 
                                     (dy + 0.5f) / _textureDimension,
                                     0,
                                     rect.species));
          }

          for (int k = 0; k < sourceTris.Length; k++) {
            sourceTris[k] += sourceVerts.Length;
          }
        }
      }
    }

    bakedMesh.hideFlags = HideFlags.HideAndDontSave;
    bakedMesh.SetVertices(bakedVerts);
    bakedMesh.SetTriangles(bakedTris, 0);
    bakedMesh.SetUVs(0, bakedUvs);
    bakedMesh.RecalculateNormals();
    bakedMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 10000);
  }

  private void resetBlitMeshes(List<SpeciesRect> layout, SpeciesData[] speciesData, SocialData[,] socialData, bool includeRectUv) {
    List<Vector3> verts = new List<Vector3>();
    List<int> tris = new List<int>();
    List<Vector4> uv0 = new List<Vector4>();
    List<Vector4> uv1 = new List<Vector4>();
    List<Vector4> uv2 = new List<Vector4>();

    foreach (var rectO in layout) {
      foreach (var rectM in layout) {
        int speciesM = rectM.species;
        int speciesO = rectO.species;

        tris.Add(verts.Count + 0);
        tris.Add(verts.Count + 1);
        tris.Add(verts.Count + 2);

        tris.Add(verts.Count + 0);
        tris.Add(verts.Count + 2);
        tris.Add(verts.Count + 3);

        verts.Add(new Vector3(rectM.x, rectM.y));
        verts.Add(new Vector3(rectM.x + rectM.width, rectM.y));
        verts.Add(new Vector3(rectM.x + rectM.width, rectM.y + rectM.height));
        verts.Add(new Vector3(rectM.x, rectM.y + rectM.height));

        float uvMx0 = rectM.x / 64.0f;
        float uvMx1 = (rectM.x + rectM.width) / 64.0f;

        float uvMy0 = rectM.y / 64.0f;
        float uvMy1 = (rectM.y + rectM.height) / 64.0f;

        float uvOx0 = rectO.x / 64.0f;
        float uvOx1 = (rectO.x + rectO.width) / 64.0f;

        float uvOy0 = rectO.y / 64.0f;
        float uvOy1 = (rectO.y + rectO.height) / 64.0f;

        uv0.Add(new Vector4(uvMx0, uvMy0, uvOx0, uvOy0));
        uv0.Add(new Vector4(uvMx1, uvMy0, uvOx0, uvOy0));
        uv0.Add(new Vector4(uvMx1, uvMy1, uvOx0, uvOy0));
        uv0.Add(new Vector4(uvMx0, uvMy1, uvOx0, uvOy0));

        Vector4 social;
        social.x = socialData[speciesM, speciesO].socialForce;
        social.y = socialData[speciesM, speciesO].socialRange;
        social.z = speciesData[speciesM].collisionForce * speciesData[speciesO].collisionForce;
        social.w = 0;

        uv1.Add(social);
        uv1.Add(social);
        uv1.Add(social);
        uv1.Add(social);

        uv2.Add(new Vector4(uvOx0, uvOy0, uvOx1, uvOy1));
        uv2.Add(new Vector4(uvOx0, uvOy0, uvOx1, uvOy1));
        uv2.Add(new Vector4(uvOx0, uvOy0, uvOx1, uvOy1));
        uv2.Add(new Vector4(uvOx0, uvOy0, uvOx1, uvOy1));
      }
    }

    if (_blitMeshInteraction == null) {
      _blitMeshInteraction = new Mesh();
      _blitMeshInteraction.name = "BlitMesh Interaction";
    }
    _blitMeshInteraction.Clear();
    _blitMeshInteraction.SetVertices(verts);
    _blitMeshInteraction.SetTriangles(tris, 0, calculateBounds: true);
    _blitMeshInteraction.SetUVs(0, uv0);
    _blitMeshInteraction.SetUVs(1, uv1);
    if (includeRectUv) {
      _blitMeshInteraction.SetUVs(2, uv2);
    }
    _blitMeshInteraction.UploadMeshData(markNoLogerReadable: true);

    verts.Clear();
    tris.Clear();
    uv0.Clear();
    uv1.Clear();
    uv2.Clear();

    foreach (var rect in layout) {
      tris.Add(verts.Count + 0);
      tris.Add(verts.Count + 1);
      tris.Add(verts.Count + 2);

      tris.Add(verts.Count + 0);
      tris.Add(verts.Count + 2);
      tris.Add(verts.Count + 3);

      verts.Add(new Vector3(rect.x, rect.y));
      verts.Add(new Vector3(rect.x + rect.width, rect.y));
      verts.Add(new Vector3(rect.x + rect.width, rect.y + rect.height));
      verts.Add(new Vector3(rect.x, rect.y + rect.height));

      float socialSteps = speciesData[rect.species].forceSteps;
      float dragMult = 1.0f - speciesData[rect.species].drag;    //use 1-drag so we can just multiply by it in shader

      float uvx0 = rect.x / 64.0f;
      float uvy0 = rect.y / 64.0f;
      float uvx1 = (rect.x + rect.width) / 64.0f;
      float uvy1 = (rect.y + rect.height) / 64.0f;

      uv0.Add(new Vector4(uvx0, uvy0, socialSteps, dragMult));
      uv0.Add(new Vector4(uvx1, uvy0, socialSteps, dragMult));
      uv0.Add(new Vector4(uvx1, uvy1, socialSteps, dragMult));
      uv0.Add(new Vector4(uvx0, uvy1, socialSteps, dragMult));
    }

    if (_blitMeshParticle == null) {
      _blitMeshParticle = new Mesh();
      _blitMeshParticle.name = "BlitMesh Particle";
    }
    _blitMeshParticle.Clear();
    _blitMeshParticle.SetVertices(verts);
    _blitMeshParticle.SetTriangles(tris, 0, calculateBounds: true);
    _blitMeshParticle.SetUVs(0, uv0);
    _blitMeshParticle.UploadMeshData(markNoLogerReadable: true);

    verts.Clear();
    tris.Clear();
    uv0.Clear();
    uv1.Clear();

    tris.Add(0);
    tris.Add(1);
    tris.Add(2);

    tris.Add(0);
    tris.Add(2);
    tris.Add(3);

    verts.Add(new Vector3(0, 0, 0));
    verts.Add(new Vector3(_textureDimension, 0, 0));
    verts.Add(new Vector3(_textureDimension, _textureDimension, 0));
    verts.Add(new Vector3(0, _textureDimension, 0));

    uv0.Add(new Vector4(0, 0, 0, 0));
    uv0.Add(new Vector4(1, 0, 0, 0));
    uv0.Add(new Vector4(1, 1, 0, 0));
    uv0.Add(new Vector4(0, 1, 0, 0));

    if (_blitMeshQuad == null) {
      _blitMeshQuad = new Mesh();
      _blitMeshQuad.name = "BitMesh Quad";
    }
    _blitMeshQuad.Clear();
    _blitMeshQuad.SetVertices(verts);
    _blitMeshQuad.SetTriangles(tris, 0, calculateBounds: true);
    _blitMeshQuad.SetUVs(0, uv0);
    _blitMeshQuad.UploadMeshData(markNoLogerReadable: true);
  }

  #endregion

  #region HAND INTERACTION

  private void doHandInfluenceStateUpdate(float framePercent) {
    if (!handInfluenceEnabled) {
      _simulationMat.SetInt("_SphereCount", 0);
      return;
    }

    _handActors[0].UpdateState(Hands.Left, framePercent);
    _handActors[1].UpdateState(Hands.Right, framePercent);

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

    //Transform into local space
    for (int i = 0; i < sphereCount; i++) {
      Vector4 sphere = _spheres[i];
      float w = sphere.w;

      sphere = transform.InverseTransformPoint(sphere);
      sphere.w = w / transform.lossyScale.x;
      _spheres[i] = sphere;

      Vector4 velocity = _sphereVels[i];
      velocity = transform.InverseTransformVector(velocity);
      velocity.w = _sphereVels[i].w;
      _sphereVels[i] = velocity;
    }

    _simulationMat.SetInt("_SphereCount", sphereCount);
    _simulationMat.SetVectorArray("_Spheres", _spheres);
    _simulationMat.SetVectorArray("_SphereVelocities", _sphereVels);
    _simulationMat.SetFloat("_SphereForce", influenceForce);
  }

  private void doHandCollision() {
    if (!_handCollisionEnabled) {
      return;
    }

    int capsuleCount = 0;

    generateCapsulesForHand(Hands.Left, _handActors[0], ref _prevLeft, ref capsuleCount);
    generateCapsulesForHand(Hands.Right, _handActors[1], ref _prevRight, ref capsuleCount);

    RuntimeGizmoDrawer drawer;
    if (_handCollisionEnabled && _drawHandColliders && RuntimeGizmoManager.TryGetGizmoDrawer(out drawer)) {
      drawer.color = _handColliderColor;
      for (int i = 0; i < capsuleCount; i++) {
        drawer.DrawWireCapsule(_capsuleA[i], _capsuleB[i], _capsuleA[i].w);
      }
    }

    _simulationMat.SetFloat("_HandCollisionInverseThickness", 1.0f / _handCollisionThickness);
    _simulationMat.SetFloat("_HandCollisionExtraForce", _extraHandCollisionForce);
    _simulationMat.SetInt("_SocialHandSpecies", _socialHandSpecies);
    _simulationMat.SetFloat("_SocialHandForceFactor", _socialHandEnabled ? _socialHandForceFactor : 0);

    //Transform capsules into local space
    for (int i = 0; i < capsuleCount; i++) {
      Vector4 v = _capsuleA[i];
      Vector4 tv = transform.InverseTransformPoint(v);
      tv.w = v.w / transform.lossyScale.x;

      _capsuleA[i] = tv;
      _capsuleB[i] = transform.InverseTransformPoint(_capsuleB[i]);
    }

    _simulationMat.SetInt("_CapsuleCount", capsuleCount);
    _simulationMat.SetVectorArray("_CapsuleA", _capsuleA);
    _simulationMat.SetVectorArray("_CapsuleB", _capsuleB);
  }

  private void generateCapsulesForHand(Hand source, HandActor actor, ref Hand prev, ref int count) {
    if (source != null) {
      if (prev == null) {
        prev = new Hand().CopyFrom(source);
      }
      if (!actor.active || !_disableCollisionWhenGrasping) {
        for (int i = 0; i < 5; i++) {
          Finger finger = source.Fingers[i];
          Finger prevFinger = prev.Fingers[i];
          for (int j = 0; j < 4; j++) {
            Bone bone = finger.bones[j];
            Bone prevBone = prevFinger.bones[j];

            Vector3 joint0 = bone.NextJoint.ToVector3();
            Vector3 joint1 = bone.PrevJoint.ToVector3();
            Vector3 prevJoint0 = prevBone.NextJoint.ToVector3();
            Vector3 prevJoint1 = prevBone.PrevJoint.ToVector3();

            int spheres = j == 0 ? _spheresPerMetacarpal : _spheresPerBone;
            for (int k = 0; k < spheres; k++) {
              float percent = k / (float)spheresPerBone;

              Vector4 a = Vector3.Lerp(joint0, joint1, percent);
              Vector4 b = Vector3.Lerp(prevJoint0, prevJoint1, percent);

              float speed = (a - b).magnitude;
              float speedPercent = speed / _maxHandCollisionSpeed;
              float radius = Mathf.Lerp(_handCollisionRadius.x, _handCollisionRadius.y, _handVelocityToCollisionRadius.Evaluate(speedPercent));
              a.w = radius;

              _capsuleA[count] = a;
              _capsuleB[count] = a + (b - a) * _handCollisionVelocityScale;

              count++;
            }
          }
        }
      }
      prev.CopyFrom(source);
    } else {
      prev = null;
    }
  }

  private class HandActor {
    public Vector3 position, prevPosition;
    public bool active;

    private SmoothedFloat _smoothedGrab = new SmoothedFloat();
    private TextureSimulator _sim;
    private float _influence;
    private float _radiusMultiplier;
    private float _alpha;
    private float _startingAlpha;
    private MaterialPropertyBlock _block;

    private Vector3 _prevTrackedPosition;
    private Vector3 _currTrackedPosition;

    public HandActor(TextureSimulator sim) {
      _sim = sim;
      _block = new MaterialPropertyBlock();
      _startingAlpha = sim._influenceMat.GetFloat("_Glossiness");
      _smoothedGrab.delay = sim.influenceGrabSmoothing;
    }

    public Vector4 sphere {
      get {
        Vector4 s = prevPosition;
        s.w = _sim.maxInfluenceRadius * _radiusMultiplier;
        return s;
      }
    }

    public Vector4 velocity {
      get {
        Vector4 vel = position - prevPosition;
        vel.w = _influence;
        return vel;
      }
    }

    private Vector3 getPositionFromHand(Hand hand) {
      return hand.PalmPosition.ToVector3() + hand.PalmarAxis() * _sim._influenceNormalOffset + hand.DistalAxis() * _sim._influenceForwardOffset;
    }

    public void UpdateHand(Hand hand) {
      _prevTrackedPosition = _currTrackedPosition;

      if (hand != null) {
        _currTrackedPosition = getPositionFromHand(hand);
      }

      if (active && _sim._showHandInfluenceBubble) {
        var meshMat = Matrix4x4.TRS(_currTrackedPosition, Quaternion.identity, Vector3.one * _sim.maxInfluenceRadius * _radiusMultiplier);
        _block.SetFloat("_Glossiness", _alpha * _startingAlpha);
        Graphics.DrawMesh(_sim._influenceMesh, meshMat, _sim._influenceMat, 0, null, 0, _block);
      }
    }

    public void UpdateState(Hand hand, float framePercent) {
      prevPosition = position;
      position = Vector3.Lerp(_prevTrackedPosition, _currTrackedPosition, framePercent);

      float rawGrab;
      if (hand != null) {
        rawGrab = 1;
        foreach (var finger in hand.Fingers) {
          if (finger.Type == Finger.FingerType.TYPE_THUMB) continue;
          float angle = Vector3.Angle(finger.bones[(int)Bone.BoneType.TYPE_DISTAL].Direction.ToVector3(), hand.DistalAxis());
          rawGrab = Mathf.Min(angle / 180, rawGrab);
        }
      } else {
        rawGrab = 0;
      }

      _smoothedGrab.delay = _sim.influenceGrabSmoothing;
      _smoothedGrab.Update(rawGrab, Time.deltaTime);
      float grab = _smoothedGrab.value;

      switch (_sim.handInfluenceMode) {
        case HandInfluenceMode.Binary:
          if (active) {
            active = hand != null && rawGrab > _sim.influenceBinarySettings.endGrabStrength;
          } else {
            active = hand != null && rawGrab > _sim.influenceBinarySettings.startGrabStrength;
          }
          _influence = 1;
          _radiusMultiplier = 1;
          _alpha = 1;
          break;
        case HandInfluenceMode.Radius:
          _radiusMultiplier = _sim._influenceRadiusSettings.grabStrengthToRadius.Evaluate(grab);
          _influence = _sim._influenceRadiusSettings.grabStrengthToInfluence.Evaluate(grab);

          _alpha = 1;
          active = _radiusMultiplier > 0;
          break;
        case HandInfluenceMode.Fade:
          _alpha = _sim._influenceFadeSettings.grabStrengthToAlpha.Evaluate(grab);
          _influence = _sim._influenceFadeSettings.grabStrengthToInfluence.Evaluate(grab);

          _radiusMultiplier = 1;
          active = _alpha > 0.05f;
          break;
      }
    }
  }

  #endregion

  #region PRIVATE IMPLEMENTATION
  private void updateShaderDebug() {
    if (_shaderDebugMode == ShaderDebugMode.None) {
      return;
    }

    if (_shaderDebugTexture == null) {
      _shaderDebugTexture = new RenderTexture(512, 512, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
      _shaderDebugTexture.filterMode = FilterMode.Point;
      _shaderDataDebug.material.mainTexture = _shaderDebugTexture;
    }

    Vector4 data = Vector4.zero;
    switch (_shaderDebugMode) {
      case ShaderDebugMode.Raw:
      case ShaderDebugMode.SocialData:
        data.x = _shaderDebugData0;
        data.y = _shaderDebugData1;
        break;
      default:
        data.x = _shaderDebugData0 / (float)MAX_PARTICLES;
        data.y = _shaderDebugData1 / (float)MAX_PARTICLES;
        break;
    }

    _simulationMat.SetInt("_DebugMode", (int)_shaderDebugMode);
    _simulationMat.SetVector("_DebugData", data);
    Graphics.Blit(null, _shaderDebugTexture, _simulationMat, PASS_SHADER_DEBUG);
  }

  private void updateShaderData() {
    _simulationMat.SetVector("_FieldCenter", _fieldCenter.localPosition);
    _simulationMat.SetFloat("_FieldRadius", _fieldRadius);
    _simulationMat.SetFloat("_FieldForce", _fieldForce);

    if (_provider != null) {
      _simulationMat.SetVector("_HeadPos", transform.InverseTransformPoint(_provider.transform.position));
      _simulationMat.SetFloat("_HeadRadius", _headRadius);
    }

    _simulationMat.SetFloat("_SpawnRadius", _spawnRadius);
    _simulationMat.SetInt("_SpeciesCount", _currentSimulationSpeciesCount);
    _simulationMat.SetInt("_ParticleCount", _particlesToSimulate);
    _displayBlock.SetFloat("_ParticleCount", _particlesToSimulate / (float)MAX_PARTICLES);

    _simulationMat.SetInt("_StochasticCount", Mathf.RoundToInt(Mathf.Lerp(0, 256, _stochasticPercent)));
    _simulationMat.SetFloat("_StochasticOffset", (Time.frameCount % _stochasticCycleCount) / (float)_stochasticCycleCount);
  }

  private void handleUserInput() {
    if (Input.GetKeyDown(_loadPresetEcosystemKey)) {
      LoadPresetEcosystem(_presetEcosystemSettings.ecosystemPreset);
    }

    if (Input.GetKeyDown(_loadEcosystemSeedKey)) {
      LoadRandomEcosystem(_ecosystemSeed);
      ResetPositions();
    }

    if (Input.GetKeyDown(_randomizeEcosystemKey)) {
      LoadRandomEcosystem();
    }

    if (Input.GetKeyDown(_resetParticlePositionsKey)) {
      ResetPositions();
    }
  }

  private RenderTexture createParticleTexture() {
    RenderTexture tex = new RenderTexture(_textureDimension, _textureDimension, 0, _textureFormat, RenderTextureReadWrite.Linear);
    tex.wrapMode = TextureWrapMode.Clamp;
    tex.filterMode = FilterMode.Point;

    RenderTexture.active = tex;
    GL.Clear(clearDepth: false, clearColor: true, backgroundColor: Color.green);
    RenderTexture.active = null;
    return tex;
  }

  private RenderTexture createQueueTexture(int steps) {
    RenderTexture tex = new RenderTexture(_textureDimension, _textureDimension * steps, 0, _textureFormat, RenderTextureReadWrite.Linear);
    tex.wrapMode = TextureWrapMode.Clamp;
    tex.filterMode = FilterMode.Point;

    RenderTexture.active = tex;
    GL.Clear(clearDepth: false, clearColor: true, backgroundColor: Color.black);
    RenderTexture.active = null;
    return tex;
  }

  private void stepSimulation(float framePercent) {
    if (_provider != null) {
      doHandCollision();

      doHandInfluenceStateUpdate(framePercent);
    }

    Graphics.ExecuteCommandBuffer(_simulationCommands[_commandIndex]);
    _commandIndex = (_commandIndex + 1) % _simulationCommands.Count;
  }

  private void updateKeywords() {
    _particleMat.DisableKeyword(BY_SPECIES);
    _particleMat.DisableKeyword(BY_SPECIES_WITH_VELOCITY);
    _particleMat.DisableKeyword(BY_VELOCITY);

    switch (_colorMode) {
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

    if (_dynamicTimestepEnabled) {
      _particleMat.EnableKeyword(INTERPOLATION_KEYWORD);
    } else {
      _particleMat.DisableKeyword(INTERPOLATION_KEYWORD);
    }

    _simulationMat.DisableKeyword(INFLUENCE_FORCE_KEYWORD);
    _simulationMat.DisableKeyword(INFLUENCE_STASIS_KEYWORD);

    switch (_handInfluenceType) {
      case HandInfluenceType.Force:
        _simulationMat.EnableKeyword(INFLUENCE_FORCE_KEYWORD);
        break;
      case HandInfluenceType.Stasis:
        _simulationMat.EnableKeyword(INFLUENCE_STASIS_KEYWORD);
        break;
      default:
        throw new System.Exception();
    }

    _particleMat.DisableKeyword(TAIL_FISH_KEYWORD);
    _particleMat.DisableKeyword(TAIL_SQUASH_KEYWORD);

    switch (_trailMode) {
      case TrailMode.Fish:
        _particleMat.EnableKeyword(TAIL_FISH_KEYWORD);
        break;
      case TrailMode.Squash:
        _particleMat.EnableKeyword(TAIL_SQUASH_KEYWORD);
        break;
    }

    if (_stochasticSamplingEnabled) {
      _simulationMat.EnableKeyword(STOCHASTIC_KEYWORD);

      Texture2D coordinates = new Texture2D(256, _stochasticCycleCount, TextureFormat.ARGB32, mipmap: false, linear: true);
      coordinates.filterMode = FilterMode.Point;
      coordinates.wrapMode = TextureWrapMode.Clamp;

      List<Color> colors = new List<Color>();
      for (int i = 0; i < _stochasticCycleCount; i++) {

        List<Color> block = new List<Color>();
        for (int dx = 0; dx < 16; dx++) {
          for (int dy = 0; dy < 16; dy++) {
            block.Add(new Color(dx / 64.0f, dy / 64.0f, 0, 0));
          }
        }
        block.Shuffle();

        colors.AddRange(block);
      }

      coordinates.SetPixels(colors.ToArray());
      coordinates.Apply();
      _simulationMat.SetTexture(STOCHASTIC_PROPERTY, coordinates);
    } else {
      _simulationMat.DisableKeyword(STOCHASTIC_KEYWORD);
    }
  }

  private void displaySimulation() {
    foreach (var mesh in _renderMeshes) {
      Graphics.DrawMesh(mesh, transform.localToWorldMatrix, _particleMat, 0, null, 0, _displayBlock);
    }
  }

  private void blitCopy(Texture src, RenderTexture dst) {
    GL.LoadPixelMatrix(0, 1, 0, 1);

    RenderTexture.active = dst;
    GL.Clear(clearDepth: false, clearColor: true, backgroundColor: new Color(0, 1, 0, 0));

    _simulationMat.SetTexture("_CopySource", src);
    _simulationMat.SetPass(PASS_COPY);

    GL.Begin(GL.QUADS);

    GL.TexCoord2(0, 0);
    GL.Vertex3(0, 0, 0);

    GL.TexCoord2(1, 0);
    GL.Vertex3(1, 0, 0);

    GL.TexCoord2(1, 1);
    GL.Vertex3(1, 1, 0);

    GL.TexCoord2(0, 1);
    GL.Vertex3(0, 1, 0);

    GL.End();

    RenderTexture.active = null;
  }

  private void buildSimulationCommands() {
    RenderTexture startPos = _positionSrc;
    RenderTexture startVel = _velocitySrc;
    RenderTexture startSoc = _socialQueueSrc;

    //We are going to keep building new command buffers
    //until the state of the textures equals the original
    //state.  If there is no fancy conditional logic involved 
    //then this will always wind up being 2 buffers.

    int bufferIndex = 0;
    do {
      CommandBuffer buffer = new CommandBuffer();
      buffer.name = "Particle Simulation " + bufferIndex;
      bufferIndex++;
      _simulationCommands.Add(buffer);

      buildSimulationCommands(bufferIndex, buffer);
    } while (_positionSrc != startPos ||
            _velocitySrc != startVel ||
            _socialQueueSrc != startSoc);

    Debug.Log("Built " + bufferIndex + " Simulation Command Buffers");
  }

  private void buildSimulationCommands(int bufferIndex, CommandBuffer buffer) {
    buffer.SetViewMatrix(Matrix4x4.identity);
    buffer.SetProjectionMatrix(Matrix4x4.Ortho(0, _textureDimension, 0, _textureDimension, -1, 1));

    blitCommands(buffer, _blitMeshParticle, VELOCITY_GLOBAL, ref _velocitySrc, ref _velocityDst, PASS_GLOBAL_FORCES);

    particleCommands(buffer);

    blitCommands(buffer, _blitMeshQuad, SOCIAL_FORCE_GLOBAL, ref _socialQueueSrc, ref _socialQueueDst, PASS_STEP_SOCIAL_QUEUE);

    blitCommands(buffer, _blitMeshParticle, VELOCITY_GLOBAL, ref _velocitySrc, ref _velocityDst, PASS_DAMP_VELOCITIES_APPLY_SOCIAL_FORCES);

    blitCommands(buffer, _blitMeshParticle, POSITION_GLOBAL, ref _positionSrc, ref _positionDst, PASS_INTEGRATE_VELOCITIES);
  }

  private void blitCommands(CommandBuffer buffer, Mesh mesh, string propertyName, ref RenderTexture src, ref RenderTexture dst, int pass) {
    //Blit onto the destination texture
    buffer.SetRenderTarget(dst);

    //Clear it before we do (would like to discard but we don't have that)
    buffer.ClearRenderTarget(clearDepth: false, clearColor: true, backgroundColor: new Color(0, 0, 0, 0));

    //Draw the mesh onto the destination
    buffer.DrawMesh(mesh, Matrix4x4.identity, _simulationMat, 0, pass);

    //Set up the destination texture as the new source
    buffer.SetGlobalTexture(propertyName, dst);

    //Swap the textures to that the next time we come around it goes the opposite way
    Utils.Swap(ref src, ref dst);
  }

  private void particleCommands(CommandBuffer buffer) {
    RenderTargetIdentifier[] _colorBuffers = new RenderTargetIdentifier[2];
    _colorBuffers[0] = _velocityDst;
    _colorBuffers[1] = _socialTemp;

    //Blit onto both the velocity texture as well as the social force temp texture
    buffer.SetRenderTarget(_colorBuffers, _velocitySrc);

    //Clear both of them to 0,0,0,0
    buffer.ClearRenderTarget(clearDepth: false, clearColor: true, backgroundColor: new Color(0, 0, 0, 0));

    //Draw the special interaction mesh
    buffer.DrawMesh(_blitMeshInteraction, Matrix4x4.identity, _simulationMat, 0, PASS_UPDATE_COLLISIONS);

    //Set the new velocity texture as the new global
    buffer.SetGlobalTexture(VELOCITY_GLOBAL, _velocityDst);

    Utils.Swap(ref _velocitySrc, ref _velocityDst);
  }
  #endregion

}