using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
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
  public const int CLUSTER_COUNT = 64;

  public const string BY_SPECIES = "COLOR_SPECIES";
  public const string BY_SPECIES_WITH_VELOCITY = "COLOR_SPECIES_MAGNITUDE";
  public const string BY_VELOCITY = "COLOR_VELOCITY";
  public const string BY_CLUSTER = "COLOR_CLUSTER";

  public const string INTERPOLATION_KEYWORD = "ENABLE_INTERPOLATION";

  public const string INFLUENCE_STASIS_KEYWORD = "SPHERE_MODE_STASIS";
  public const string INFLUENCE_FORCE_KEYWORD = "SPHERE_MODE_FORCE";

  public const string TAIL_FISH_KEYWORD = "FISH_TAIL";
  public const string TAIL_SQUASH_KEYWORD = "SQUASH_TAIL";

  public const int PASS_INTEGRATE_VELOCITIES = 0;
  public const int PASS_UPDATE_COLLISIONS = 1;
  public const int PASS_GLOBAL_FORCES = 2;
  public const int PASS_DAMP_VELOCITIES_APPLY_SOCIAL_FORCES = 3;
  public const int PASS_RANDOMIZE_PARTICLES = 4;
  public const int PASS_STEP_SOCIAL_QUEUE = 5;
  public const int PASS_SHADER_DEBUG = 6;
  public const int PASS_COPY = 7;

  public const string CLUSTER_KEYWORD = "USE_CLUSTERS";
  public const string CLUSTER_KERNEL_ASSIGN = "CalculateClusterAssignments";
  public const string CLUSTER_KERNEL_INTEGRATE = "IntegrateCounts";
  public const string CLUSTER_KERNEL_SORT = "SortParticlesIntoClusters";
  public const string CLUSTER_KERNEL_UPDATE = "UpdateClusterCenters";

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

  [Range(1, MAX_PARTICLES)]
  [SerializeField]
  private int _particlesToSimulate = MAX_PARTICLES;
  public int particlesToSimulate {
    get { return _particlesToSimulate; }
    set {
      _particlesToSimulate = Mathf.Clamp(value, 1, MAX_PARTICLES);
    }
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

  //#######################//
  ///      Clustering      //
  //#######################//
  [Header("Clustering")]
  [OnEditorChange("clusteringEnabled")]
  [SerializeField]
  private bool _clusteringEnabled = false;
  public bool clusteringEnabled {
    get { return _clusteringEnabled; }
    set {
      _clusteringEnabled = value;
      updateKeywords();
    }
  }

  [SerializeField]
  private ComputeShader _clusterShader;

  //####################//
  ///      Display      //
  //####################//
  [Header("Display")]
  [SerializeField]
  private bool _displayParticles = true;
  public bool displayParticles {
    get { return _displayParticles; }
    set { _displayParticles = value; }
  }

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

    [SerializeField]
    private SpawnPreset _spawnMode = SpawnPreset.Spherical;
    public SpawnPreset spawnMode {
      get { return _spawnMode; }
      set { _spawnMode = value; }
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

  [SerializeField]
  private bool _displayClusteringDebug = false;

  [OnEditorChange("gpuStatsEnabled")]
  [SerializeField]
  private bool _gpuStatsEnabled = false;
  public bool gpuStatsEnabled {
    get { return _gpuStatsEnabled; }
    set {
      _gpuStatsEnabled = value;
      updateKeywords();
    }
  }
  #endregion

  //Simulation
  private int _currentSimulationSpeciesCount = MAX_SPECIES;
  private SpawnPreset _currentSpawnPreset = SpawnPreset.Spherical;
  private RenderTexture _frontPos, _frontVel, _backPos, _backVel;
  private RenderTexture _frontSocial, _backSocial;
  private RenderTexture _socialTemp;

  private float _currScaledTime = 0;
  private float _currSimulationTime = 0;
  private float _prevSimulationTime = 0;

  private ComputeBuffer _gpuStatsBuffer;
  private int[] _gpuStatsData = new int[2];

  //Display
  private List<Mesh> _meshes = new List<Mesh>();
  private MaterialPropertyBlock _displayBlock;

  //Hand interaction
  private Vector4[] _capsuleA = new Vector4[128];
  private Vector4[] _capsuleB = new Vector4[128];

  private Vector4[] _spheres = new Vector4[2];
  private Vector4[] _sphereVels = new Vector4[2];

  private HandActor[] _handActors = new HandActor[2];
  private Hand _prevLeft, _prevRight;

  //Clustering
  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  private struct Cluster {
    public Vector3 center;
    public float radius;
    public uint count;
    public uint start;
    public uint end;
  }

  private ComputeBuffer _clusters;
  private ComputeBuffer _clusterAssignments;
  private ComputeBuffer _clusterDebug;
  private RenderTexture _clusteredParticles;
  private int _clusterKernelAssign;
  private int _clusterKernelIntegrate;
  private int _clusterKernelSort;
  private int _clusterKernelUpdate;
  private int _clusterKernelParticleNaN;
  private int _clusterKernelClusteredNaN;

  //Shader debug
  private RenderTexture _shaderDebugTexture;

  #region PUBLIC API

  public enum ColorMode {
    BySpecies,
    BySpeciesWithMagnitude,
    ByVelocity,
    ByCluster
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

  private string _currentSpecies = "";
  public string currentSpecies {
    get {
      return _currentSpecies;
    }
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

  private string _lastSeed = "";
  public void ReloadRandomEcosystem() {
    LoadRandomEcosystem(_lastSeed);
    ResetPositions();
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

  #endregion

  #region UNITY MESSAGES
  private void Awake() {
    _displayBlock = new MaterialPropertyBlock();
  }

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

    RecalculateMeshesForParticles();

    LoadPresetEcosystem(_presetEcosystemSettings.ecosystemPreset);

    updateShaderData();

    _handActors.Fill(() => new HandActor(this));

    updateKeywords();

    ResetPositions();
  }

  private void OnDisable() {
    cleanupClusters();

    if (_gpuStatsBuffer != null) {
      _gpuStatsBuffer.Release();
      _gpuStatsBuffer = null;
    }
  }

  void Update() {
    //FindObjectOfType<TextMesh>().text = "";

    if (_enableSpeciesDebugColors) {
      Color[] colors = new Color[MAX_SPECIES];
      colors.Fill(Color.black);
      colors[_debugSpeciesNumber] = Color.white;
      _particleMat.SetColorArray("_Colors", colors);
    }

    updateShaderData();

    handleUserInput();

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

  #region ECOSYSTEMS
  public const int SPECIES_CAP_FOR_PRESETS = 10;

  public enum EcosystemPreset {
    RedMenace,
    Chase,
    Mitosis,
    BodyMind,
    Planets,
    Globules,
    Test,
    Fluidy
  }

  public enum SpawnPreset {
    Spherical
  }

  public void LoadPresetEcosystem(EcosystemPreset preset) {
    _particlesToSimulate = MAX_PARTICLES;

    var setting = _presetEcosystemSettings;
    _currentSpawnPreset = SpawnPreset.Spherical;
    _currentSimulationSpeciesCount = SPECIES_CAP_FOR_PRESETS;
    Color[] colors = new Color[MAX_SPECIES];
    Vector4[,] socialData = new Vector4[MAX_SPECIES, MAX_SPECIES];
    Vector4[] speciesData = new Vector4[MAX_SPECIES];

    Vector3[] particlePositions = new Vector3[MAX_PARTICLES].Fill(() => Random.insideUnitSphere);
    Vector3[] particleVelocities = new Vector3[MAX_PARTICLES];
    int[] particleSpecies = new int[MAX_PARTICLES].Fill(() => Random.Range(0, int.MaxValue));

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

        ResetPositions();
        break;
      case EcosystemPreset.Chase:
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

        ResetPositions();
        break;
      case EcosystemPreset.Mitosis:
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

        ResetPositions();
        break;
      case EcosystemPreset.Planets:

        _currentSimulationSpeciesCount = 9;

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

        ResetPositions();
        break;

      case EcosystemPreset.Test:
        _currentSimulationSpeciesCount = 8;

        float drag = 0.0f;
        float collision = 1.0f;
        int steps = 6;

        float selfLove = 0.3f;
        float selfRange = 0.3f;

        float fearForce = -0.4f;
        float fearRange_ = 0.2f;

        float loveForce = 0.3f;
        float loveRange_ = 0.2f;

        float d = Mathf.Lerp(setting.minDrag, setting.maxDrag, drag);
        float c = Mathf.Lerp(setting.minCollision, setting.maxCollision, collision);

        int numParticlesPerSpecies = MAX_PARTICLES / _currentSimulationSpeciesCount;

        int pp = 0;
        for (int species = 0; species < _currentSimulationSpeciesCount; species++) {
          float g = (float)species / (float)_currentSimulationSpeciesCount;
          colors[species] = new Color(g, g, g);
          speciesData[species] = new Vector3(d, steps, c);

          // clear it all
          for (int other = 0; other < _currentSimulationSpeciesCount; other++) {
            socialData[species, other] = new Vector2(setting.maxSocialForce * 0.0f, setting.maxSocialRange * 0.0f);
          }

          // species reaction to own kind
          socialData[species, species] = new Vector2(setting.maxSocialForce * selfLove, setting.maxSocialRange * selfRange);

          // species reaction to before species and after species
          int before = species - 1;
          int after = species + 1;

          if (before == -1) { before = _currentSimulationSpeciesCount - before; }
          if (after == _currentSimulationSpeciesCount) { after = _currentSimulationSpeciesCount - after; }

          socialData[species, before] = new Vector2(setting.maxSocialForce * fearForce, setting.maxSocialRange * fearRange_);
          socialData[species, after] = new Vector2(setting.maxSocialForce * loveForce, setting.maxSocialRange * loveRange_);

          for (int p = 0; p < numParticlesPerSpecies; p++) {
            float ff = (float)p / (float)numParticlesPerSpecies;
            float rr = 0.3f;
            float xx = -rr * 0.5f + ff * rr;
            float yy = -rr * 0.5f + g * rr;
            float zz = -rr * 0.5f + Random.value * rr;

            particleSpecies[pp] = species;
            particlePositions[pp] = new Vector3(xx, yy, zz);
            pp++;
          }
        }

        ResetPositions(particlePositions, particleVelocities, particleSpecies);

        //default
        //ResetPositions();

        break;

      case EcosystemPreset.BodyMind:
        _currentSimulationSpeciesCount = 3;

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

        ResetPositions();
        break;
      case EcosystemPreset.Globules:
        _currentSimulationSpeciesCount = 3;

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

        ResetPositions();
        break;
      case EcosystemPreset.Fluidy:
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

        //Alex added this
        for (int i = 0; i < MAX_PARTICLES; i++) {
          float percent = Mathf.InverseLerp(0, MAX_PARTICLES, i);
          float percent2 = percent * 12.123123f + Random.value;
          particlePositions[i] = new Vector3(Mathf.Lerp(-1, 1, percent2 - (int)percent2), Mathf.Lerp(-1, 1, percent), Random.Range(-0.01f, 0.01f));
          particleSpecies[i] = Mathf.FloorToInt(percent * _currentSimulationSpeciesCount);
        }
        ResetPositions(particlePositions, particleVelocities, particleSpecies);
        break;
    }

    //Invert drag before we upload to the GPU
    for (int i = 0; i < MAX_SPECIES; i++) {
      Vector4 species = speciesData[i];
      species.x = 1 - species.x;
      speciesData[i] = species;
    }

    //Calculate the max social range for each species
    for (int i = 0; i < MAX_SPECIES; i++) {
      Vector4 data = speciesData[i];
      for (int j = 0; j < MAX_SPECIES; j++) {
        data.w = Mathf.Max(data.w, socialData[i, j].y);
      }
      speciesData[i] = data;
    }

    var packedSocialData = new Vector4[MAX_SPECIES * MAX_SPECIES];
    for (int i = 0; i < MAX_SPECIES; i++) {
      for (int j = 0; j < MAX_SPECIES; j++) {
        packedSocialData[i * MAX_SPECIES + j] = socialData[i, j];
      }
    }

    _currentSpecies = preset.ToString();

    _simulationMat.SetVectorArray("_SocialData", packedSocialData);
    _simulationMat.SetVectorArray("_SpeciesData", speciesData);
    _particleMat.SetColorArray("_Colors", colors);
  }
  
  public void ResetPositions() {
    ResetPositions(_currentSpawnPreset);
  }

  public void ResetPositions(SpawnPreset preset) {
    _currentSpawnPreset = preset;
    Vector3[] positions = new Vector3[MAX_PARTICLES].Fill(() => Random.insideUnitSphere);
    Vector3[] velocities = new Vector3[MAX_PARTICLES];
    int[] species = new int[MAX_PARTICLES].Fill(() => Random.Range(0, int.MaxValue));

    switch (preset) {
      case SpawnPreset.Spherical:
        positions.Fill(() => Random.insideUnitSphere * _spawnRadius);
        break;
    }

    ResetPositions(positions, velocities, species);
  }

  public void ResetPositions(Vector3[] positions, Vector3[] velocities, int[] species) {
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

    GL.LoadPixelMatrix(0, 1, 1, 0);

    Texture2D tex;
    tex = new Texture2D(MAX_PARTICLES, 1, format, mipmap: false, linear: true);
    _simulationMat.SetTexture("_CopySource", tex);

    tex.SetPixels(positions.Query().Zip(species.Query(), (p, s) => {
      Color c = (Vector4)p;
      c.a = (s % _currentSimulationSpeciesCount);
      return c;
    }).ToArray());
    tex.Apply();

    blitPos(PASS_COPY);
    DestroyImmediate(tex);

    tex = new Texture2D(MAX_PARTICLES, 1, format, mipmap: false, linear: true);
    _simulationMat.SetTexture("_CopySource", tex);

    tex.SetPixels(velocities.Query().Select(p => (Color)(Vector4)p).ToArray());
    tex.Apply();

    blitVel(PASS_COPY);
    DestroyImmediate(tex);

    _simulationMat.SetInt("_SpeciesCount", _currentSimulationSpeciesCount);
    _currScaledTime = 0;
    _currSimulationTime = 0;
    _prevSimulationTime = 0;
  }

  private void setPlanetValues(int i, int j, float f, float r) {

    /*
    type[i].force [i] = f;    type[i].radius[i] = r;

    type[i].force [j] = f;    type[i].radius[j] = r;

    type[j].force [i] = f;    type[j].radius[i] = r;

    type[j].force [j] = f;    type[j].radius[j] = r;
  */

  }



  public void LoadRandomEcosystem() {
    Random.InitState(Time.realtimeSinceStartup.GetHashCode());

    var gen = GetComponent<NameGenerator>();
    string name;
    if (gen == null) {
      name = Random.Range(0, 1000).ToString();
    } else {
      name = gen.GenerateName();
    }
    _currentSpecies = name;
    Debug.Log(name);

    LoadRandomEcosystem(name);

    ResetPositions();
  }

  public void LoadRandomEcosystem(string seed) {
    var setting = _randomEcosystemSettings;
    _currentSpawnPreset = setting.spawnMode;

    _currentSimulationSpeciesCount = setting.speciesCount;
    Random.InitState(seed.GetHashCode());
    _lastSeed = seed;

    Vector4[] _socialData = new Vector4[MAX_SPECIES * MAX_SPECIES];

    for (int s = 0; s < MAX_SPECIES; s++) {
      for (int o = 0; o < MAX_SPECIES; o++) {
        _socialData[s * MAX_SPECIES + o] = new Vector2(Random.Range(-setting.maxSocialForce, setting.maxSocialForce),
                                                       Random.value * setting.maxSocialRange);
      }
    }

    Vector4[] speciesData = new Vector4[MAX_SPECIES];
    for (int i = 0; i < MAX_SPECIES; i++) {
      Vector4 data = new Vector4();
      data.x = Random.Range(setting.minDrag, setting.maxDrag);
      data.y = Random.Range(0, setting.maxForceSteps);
      data.z = Random.Range(setting.minCollision, setting.maxCollision);
      speciesData[i] = data;
    }

    // Perform color randomization last so that it has no effect on particle interaction.
    RandomizeEcosystemColors();

    //Invert drag before we upload to the GPU
    for (int i = 0; i < MAX_SPECIES; i++) {
      Vector4 species = speciesData[i];
      species.x = 1 - species.x;
      speciesData[i] = species;
    }

    _currentSpecies = seed;

    _simulationMat.SetVectorArray("_SpeciesData", speciesData);
    _simulationMat.SetVectorArray("_SocialData", _socialData);

    ResetPositions();
  }

  public void RandomizeEcosystemColors() {
    List<Color> colors;
    colors = Pool<List<Color>>.Spawn();
    try {
      GetRandomizedEcosystemColors(colors);
      _particleMat.SetColorArray("_Colors", colors.ToArray());
    } finally {
      colors.Clear();
      Pool<List<Color>>.Recycle(colors);
    }
  }

  public void GetRandomizedEcosystemColors(List<Color> colors) {
    var setting = _randomEcosystemSettings;

    colors.Clear();
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
    }
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
      sphere.w = w / transform.lossyScale.magnitude;
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
      tv.w = v.w;
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

  [ContextMenu("Recalculate Meshes")]
  public void RecalculateMeshesForParticles() {
    _meshes.Clear();

    var sourceVerts = _particleMesh.vertices;
    var sourceTris = _particleMesh.triangles;

    List<Vector3> bakedVerts = new List<Vector3>();
    List<int> bakedTris = new List<int>();
    List<Vector2> bakedUvs = new List<Vector2>();

    Mesh bakedMesh = null;
    for (int i = 0; i < _particlesToSimulate; i++) {
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

  private RenderTexture createTexture(int height = 1, bool randomWrite = false) {
    RenderTexture tex = new RenderTexture(MAX_PARTICLES, height, 0, _textureFormat, RenderTextureReadWrite.Linear);
    tex.wrapMode = TextureWrapMode.Clamp;
    tex.filterMode = FilterMode.Point;
    tex.enableRandomWrite = randomWrite;

    RenderTexture.active = tex;
    GL.Clear(clearDepth: false, clearColor: true, backgroundColor: Color.blue);
    RenderTexture.active = null;
    return tex;
  }

  private void stepSimulation(float framePercent) {
    if (_gpuStatsEnabled) {
      if (_gpuStatsBuffer == null) {
        _gpuStatsBuffer = new ComputeBuffer(2, sizeof(int));
        _simulationMat.SetBuffer("_DebugBuffer", _gpuStatsBuffer);
      }

      _gpuStatsData[0] = 0;
      _gpuStatsData[1] = 0;
      _gpuStatsBuffer.SetData(_gpuStatsData);
      Graphics.SetRandomWriteTarget(2, _gpuStatsBuffer);
    }

    if (_provider != null) {
      doHandCollision();

      doHandInfluenceStateUpdate(framePercent);
    }

    //ensureClustersReady();

    checkForFrontBack("A0");

    if (_clusteringEnabled) {
      performClustering();
    }

    if (_clusterDebug != null) {
      //_clusterDebug.GetData(new uint[1]);
      _clusterShader.SetTexture(_clusterKernelParticleNaN, "_Particles", _clusteredParticles);
    }

    checkForFrontBack("A");

    GL.LoadPixelMatrix(0, 1, 1, 0);
    blitVel(PASS_GLOBAL_FORCES);

    checkForFrontBack("B");

    using (new ProfilerSample("Particle Interaction")) {
      doParticleInteraction();
    }

    checkForFrontBack("C");

    blit("_SocialForce", ref _frontSocial, ref _backSocial, PASS_STEP_SOCIAL_QUEUE, 1);

    checkForFrontBack("D");

    blitVel(PASS_DAMP_VELOCITIES_APPLY_SOCIAL_FORCES);

    checkForFrontBack("E");

    blitPos(PASS_INTEGRATE_VELOCITIES);

    checkForFrontBack("F");

    _displayBlock.SetTexture("_CurrPos", _frontPos);
    _displayBlock.SetTexture("_PrevPos", _backPos);

    _displayBlock.SetTexture("_CurrVel", _frontVel);
    _displayBlock.SetTexture("_PrevVel", _backVel);

    _positionDebug.material.mainTexture = _frontPos;
    _velocityDebug.material.mainTexture = _frontVel;
    _socialDebug.material.mainTexture = _backSocial;

    if (_gpuStatsEnabled) {
      _gpuStatsBuffer.GetData(_gpuStatsData);
      float particlaPercent = _gpuStatsData[0] / (float)(MAX_PARTICLES * MAX_PARTICLES);
      float clusterPercent = _gpuStatsData[1] / (float)(MAX_PARTICLES);
      Debug.Log(particlaPercent + " : " + clusterPercent);
      Graphics.ClearRandomWriteTargets();
    }
  }

  private void updateKeywords() {
    _particleMat.DisableKeyword(BY_SPECIES);
    _particleMat.DisableKeyword(BY_SPECIES_WITH_VELOCITY);
    _particleMat.DisableKeyword(BY_VELOCITY);
    _particleMat.DisableKeyword(BY_CLUSTER);

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
      case ColorMode.ByCluster:
        _particleMat.EnableKeyword(BY_CLUSTER);
        break;
    }

    if (_dynamicTimestepEnabled) {
      _particleMat.EnableKeyword(INTERPOLATION_KEYWORD);
    } else {
      _particleMat.DisableKeyword(INTERPOLATION_KEYWORD);
    }

    if (_gpuStatsEnabled) {
      _simulationMat.EnableKeyword("GPU_STATS");
    } else {
      _simulationMat.DisableKeyword("GPU_STATS");
    }

    _simulationMat.DisableKeyword(INFLUENCE_FORCE_KEYWORD);
    _simulationMat.DisableKeyword(INFLUENCE_STASIS_KEYWORD);

    if (_clusteringEnabled) {
      _simulationMat.EnableKeyword(CLUSTER_KEYWORD);
    } else {
      _simulationMat.DisableKeyword(CLUSTER_KEYWORD);
    }

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
  }

  bool logItOutFoo = true;
  private void ensureClustersReady() {
    if (_clusters == null || _clusterAssignments == null || _clusteredParticles == null || _clusterDebug == null) {
      cleanupClusters();

      _clusteredParticles = createTexture(height: 1, randomWrite: true);

      _clusters = new ComputeBuffer(CLUSTER_COUNT, Marshal.SizeOf(typeof(Cluster)));
      _clusterAssignments = new ComputeBuffer(MAX_PARTICLES, sizeof(uint));
      _clusterDebug = new ComputeBuffer(1, sizeof(uint));

      _clusterKernelAssign = _clusterShader.FindKernel(CLUSTER_KERNEL_ASSIGN);
      _clusterKernelIntegrate = _clusterShader.FindKernel(CLUSTER_KERNEL_INTEGRATE);
      _clusterKernelSort = _clusterShader.FindKernel(CLUSTER_KERNEL_SORT);
      _clusterKernelUpdate = _clusterShader.FindKernel(CLUSTER_KERNEL_UPDATE);
      _clusterKernelParticleNaN = _clusterShader.FindKernel("CheckParticlesForNaN");

      _clusters.SetData(new Cluster[CLUSTER_COUNT].Fill(() => new Cluster() {
        center = Random.insideUnitCircle
      }));

      foreach (var kernel in new int[] { _clusterKernelAssign,
                                         _clusterKernelIntegrate,
                                         _clusterKernelSort,
                                         _clusterKernelUpdate,
                                         _clusterKernelParticleNaN }) {
        _clusterShader.SetBuffer(kernel, "_Clusters", _clusters);
        _clusterShader.SetBuffer(kernel, "_ClusterAssignments", _clusterAssignments);
        _clusterShader.SetTexture(kernel, "_ClusteredParticles", _clusteredParticles);
        _clusterShader.SetBuffer(kernel, "_Debug", _clusterDebug);
      }

      _displayBlock.SetBuffer("_ClusterAssignments", _clusterAssignments);
      _simulationMat.SetTexture("_ClusteredParticles", _clusteredParticles);
      _simulationMat.SetBuffer("_Clusters", _clusters);

      logItOutFoo = true;
    }
  }

  private void performClustering() {
    ensureClustersReady();

    _clusterShader.SetTexture(_clusterKernelAssign, "_Particles", _backPos);
    _clusterShader.SetTexture(_clusterKernelSort, "_Particles", _backPos);

    checkForFrontBack("##A");

    using (new ProfilerSample("Assign Clusters")) {
      _clusterShader.Dispatch(_clusterKernelAssign, MAX_PARTICLES / 64, 1, 1);
      checkForFrontBack("##B");
    }

    using (new ProfilerSample("Integrate Clusters")) {
      _clusterShader.Dispatch(_clusterKernelIntegrate, 1, 1, 1);
      checkForFrontBack("##C");
    }

    using (new ProfilerSample("Sort Clusters")) {
      _clusterShader.Dispatch(_clusterKernelSort, MAX_PARTICLES / 64, 1, 1);
      checkForFrontBack("##D");
    }

    using (new ProfilerSample("Update Clusters")) {
      _clusterShader.Dispatch(_clusterKernelUpdate, CLUSTER_COUNT, 1, 1);
      checkForFrontBack("##E");
    }
  }

  private void checkForFrontBack(string message) {
    checkForNaN(_frontPos, "Front Pos " + message);
    checkForNaN(_backPos, "Back Pos " + message);

    checkForNaN(_frontVel, "Front Vel " + message);
    checkForNaN(_backVel, "Back Vel " + message);

    checkForNaN(_clusteredParticles, "Clustered " + message);
  }

  private void checkForNaN(Texture tex, string message) {
    if (_clusterDebug == null || true) return;

    uint[] data = new uint[1] { 0 };

    _clusterDebug.SetData(data);
    _clusterShader.SetTexture(_clusterKernelParticleNaN, "_Particles", tex);
    _clusterShader.Dispatch(_clusterKernelParticleNaN, MAX_PARTICLES / 64, 1, 1);
    _clusterDebug.GetData(data);

    if (data[0] != 0) {
      Debug.Log("Particles are NaN: " + message);
    }
  }

  public void cleanupClusters() {
    if (_clusters != null) {
      _clusters.Release();
      _clusters = null;
    }

    if (_clusterAssignments != null) {
      _clusterAssignments.Release();
      _clusterAssignments = null;
    }

    if (_clusteredParticles != null) {
      _clusteredParticles.Release();
      _clusteredParticles = null;
    }

    if (_clusterDebug != null) {
      _clusterDebug.Release();
      _clusterDebug = null;
    }
  }

  private void displaySimulation() {
    foreach (var mesh in _meshes) {
      Graphics.DrawMesh(mesh, transform.localToWorldMatrix, _particleMat, 0, null, 0, _displayBlock);
    }

    if (_displayClusteringDebug && _clusteringEnabled) {
      RuntimeGizmoDrawer drawer;
      if (RuntimeGizmoManager.TryGetGizmoDrawer(out drawer)) {
        Cluster[] clusters = new Cluster[CLUSTER_COUNT];
        _clusters.GetData(clusters);

        foreach (var cluster in clusters) {
          drawer.DrawWireSphere(cluster.center, cluster.radius);
        }
      }
    }
  }

  private void blit(string propertyName, ref RenderTexture front, ref RenderTexture back, int pass, float height) {
    RenderTexture.active = front;
    front.DiscardContents();

    _simulationMat.SetPass(pass);

    quad(height);

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
    float percent = _particlesToSimulate / (float)MAX_PARTICLES;

    GL.Begin(GL.QUADS);

    GL.TexCoord2(0, 1);
    GL.Vertex3(0, 0, 0);

    GL.TexCoord2(percent, 1);
    GL.Vertex3(percent, 0, 0);

    GL.TexCoord2(percent, 0);
    GL.Vertex3(percent, height, 0);

    GL.TexCoord2(0, 0);
    GL.Vertex3(0, height, 0);

    GL.End();
  }
  #endregion

}