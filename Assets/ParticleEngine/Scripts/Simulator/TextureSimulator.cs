using System.IO;
using System.Collections;
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
  public const int MAX_PARTICLES = SimulationManager.MAX_PARTICLES;
  public const int MAX_FORCE_STEPS = SimulationManager.MAX_FORCE_STEPS;
  public const int MAX_SPECIES = SimulationManager.MAX_SPECIES;

  public const int TAIL_RAMP_RESOLUTION = 128;

  //#### Simulation Keywords ####
  public const string KEYWORD_INFLUENCE_STASIS = "SPHERE_MODE_STASIS";
  public const string KEYWORD_INFLUENCE_FORCE = "SPHERE_MODE_FORCE";

  public const string KEYWORD_SAMPLING_GENERAL = "GENERAL_SAMPLING";
  public const string KEYWORD_SAMPLING_UNIFORM = "UNIFORM_SAMPLE_RECT";

  public const string KEYWORD_STOCHASTIC = "ENABLE_STOCHASTIC_SAMPLING";
  public const string PROP_STOCHASTIC = "_StochasticCoordinates";

  public const string PROP_VELOCITY_GLOBAL = "_ParticleVelocities";
  public const string PROP_POSITION_GLOBAL = "_ParticlePositions";
  public const string PROP_POSITION_PREV_GLOBAL = "_ParticlePrevPositions";
  public const string PROP_SOCIAL_FORCE_GLOBAL = "_ParticleSocialForces";

  public const string PROP_SIMULATION_FRACTION = "_SampleFraction";

  //#### Display Keywords ####
  public const string KEYWORD_BY_SPECIES = "COLOR_SPECIES";
  public const string KEYWORD_BY_SPEED = "COLOR_SPECIES_MAGNITUDE";
  public const string KEYWORD_BY_VELOCITY = "COLOR_VELOCITY";

  public const string KEYWORD_ENABLE_INTERPOLATION = "ENABLE_INTERPOLATION";

  public const string KEYWORD_TAIL_FISH = "FISH_TAIL";
  public const string KEYWORD_TAIL_SQUASH = "SQUASH_TAIL";

  public const string PROP_SPECIES_COLOR = "_SpeciesColors";

  //#### Pass Constants ####
  public const int PASS_INTEGRATE_VELOCITIES = 0;
  public const int PASS_UPDATE_COLLISIONS = 1;
  public const int PASS_GLOBAL_FORCES = 2;
  public const int PASS_DAMP_VELOCITIES_APPLY_SOCIAL_FORCES = 3;
  public const int PASS_STEP_SOCIAL_QUEUE = 4;
  public const int PASS_SHADER_DEBUG = 5;
  public const int PASS_COPY = 6;
  public const int PASS_RANDOM_INIT = 7;

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

  [SerializeField]
  private HandCollisionBoneScales _boneCollisionScalars;

  [System.Serializable]
  public struct HandCollisionBoneScales {
    public float distalScalar;
    public float intermediateScalar;
    public float proximalScalar;
    public float metacarpalScalar;

    public float GetScalar(Bone.BoneType type) {
      switch (type) {
        case Bone.BoneType.TYPE_DISTAL:
          return distalScalar;
        case Bone.BoneType.TYPE_INTERMEDIATE:
          return intermediateScalar;
        case Bone.BoneType.TYPE_PROXIMAL:
          return proximalScalar;
        case Bone.BoneType.TYPE_METACARPAL:
          return metacarpalScalar;
        default:
          throw new System.Exception();
      }
    }
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

  [MinValue(0)]
  [SerializeField]
  private float _pointingFactor = 2;
  public float pointingFactor {
    get { return _pointingFactor; }
    set { _pointingFactor = value; }
  }

  [Range(0, 1)]
  [SerializeField]
  private float _minPointingDelta = 0.05f;
  public float minPointingDelta {
    get { return _minPointingDelta; }
    set { _minPointingDelta = value; }
  }

  [SerializeField]
  [OnEditorChange("handInfluenceType")]
  private HandInfluenceType _handInfluenceType = HandInfluenceType.Force;
  public HandInfluenceType handInfluenceType {
    get { return _handInfluenceType; }
    set {
      _handInfluenceType = value;
      UpdateShaderKeywords();
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

  //#######################//
  ///      Simulation      //
  //#######################//
  [Header("Simulation")]
  [SerializeField]
  [OnEditorChange("dynamicTimestepEnabled")]
  private bool _dynamicTimestepEnabled = true;
  public bool dynamicTimestepEnabled {
    get { return _dynamicTimestepEnabled; }
    set {
      _dynamicTimestepEnabled = value;
      UpdateShaderKeywords();
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

  [SerializeField]
  private RenderTextureFormat _textureFormat = RenderTextureFormat.ARGBFloat;

  [SerializeField]
  private Material _simulationMat;
  public Material simulationMat {
    get { return _simulationMat; }
  }

  //################################//
  ///      Stochastic Sampling      //
  //################################//
  [Header("Stochastic Sampling")]
  [Disable]
  [OnEditorChange("stochasticSamplingEnabled")]
  [SerializeField]
  private bool _stochasticSamplingEnabled = false;
  public bool stochasticSamplingEnabled {
    get { return _stochasticSamplingEnabled; }
    set {
      _stochasticSamplingEnabled = value;
      UpdateShaderKeywords();
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
      UpdateShaderKeywords();
    }
  }

  //###########################//
  ///      Reset Behavior      //
  //###########################//
  [Header("Reset Behavior")]
  [MinValue(0)]
  [SerializeField]
  private float _resetTime = 0.5f;

  [MinValue(0)]
  [SerializeField]
  private float _resetForce = 1;

  [MinValue(0)]
  [SerializeField]
  private float _resetRange = 1;

  [MinValue(0)]
  [SerializeField]
  private float _resetHeadRange = 0.6f;

  [SerializeField]
  private AnimationCurve _resetSocialCurve;

  //####################//
  ///      Display      //
  //####################//
  [Header("Display")]
  [SerializeField]
  private Material _particleMat;
  public Material particleMat {
    get { return _particleMat; }
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
  private Renderer _layoutDebug;

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
  private bool _showIsPointing = false;

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

  //General
  private SimulationManager _manager;

  //Simulation
  private List<CommandBuffer> _simulationCommands = new List<CommandBuffer>();
  private int _commandIndex = 0;

  private int _textureDimension;

  private RenderTexture _positionSrc, _velocitySrc, _positionDst, _velocityDst;
  private RenderTexture _socialQueueSrc, _socialQueueDst;
  private RenderTexture _socialTemp;

  private Mesh _blitMeshQuad;
  private Mesh _blitMeshInteraction;
  private Mesh _blitMeshParticle;

  private float _currScaledTime = 0;
  private float _currSimulationTime = 0;
  private float _prevSimulationTime = 0;
  private float _headRadiusTransitionDelta = 0;

  //Display
  private int _particlesPerMesh;
  private List<Mesh> _displayMeshes = new List<Mesh>();
  private MaterialPropertyBlock _displayBlock;
  private Texture2D _displayColorA;
  private Texture2D _displayColorB;
  private Color[] _displayColorArray = new Color[4096];

  //Hand interaction
  private Vector4[] _capsuleA = new Vector4[128];
  private Vector4[] _capsuleB = new Vector4[128];

  private Vector4[] _spheres = new Vector4[2];
  private Matrix4x4[] _sphereDeltas = new Matrix4x4[2];

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

  public RenderTexture positionTexture0 {
    get {
      return _positionSrc;
    }
  }

  public RenderTexture positionTexture1 {
    get {
      return _positionDst;
    }
  }

  public RenderTexture velocityTexture0 {
    get {
      return _velocitySrc;
    }
  }

  public RenderTexture velocityTexture1 {
    get {
      return _velocityDst;
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

  public void UpdateShaderKeywords() {
    _particleMat.DisableKeyword(KEYWORD_BY_SPECIES);
    _particleMat.DisableKeyword(KEYWORD_BY_SPEED);
    _particleMat.DisableKeyword(KEYWORD_BY_VELOCITY);

    switch (_manager.colorMode) {
      case ColorMode.BySpecies:
        _particleMat.EnableKeyword(KEYWORD_BY_SPECIES);
        break;
      case ColorMode.BySpeciesWithMagnitude:
        _particleMat.EnableKeyword(KEYWORD_BY_SPEED);
        break;
      case ColorMode.ByVelocity:
        _particleMat.EnableKeyword(KEYWORD_BY_VELOCITY);
        break;
    }

    if (_dynamicTimestepEnabled) {
      _particleMat.EnableKeyword(KEYWORD_ENABLE_INTERPOLATION);
    } else {
      _particleMat.DisableKeyword(KEYWORD_ENABLE_INTERPOLATION);
    }

    _simulationMat.DisableKeyword(KEYWORD_INFLUENCE_FORCE);
    _simulationMat.DisableKeyword(KEYWORD_INFLUENCE_STASIS);

    switch (_handInfluenceType) {
      case HandInfluenceType.Force:
        _simulationMat.EnableKeyword(KEYWORD_INFLUENCE_FORCE);
        break;
      case HandInfluenceType.Stasis:
        _simulationMat.EnableKeyword(KEYWORD_INFLUENCE_STASIS);
        break;
      default:
        throw new System.Exception();
    }

    _particleMat.DisableKeyword(KEYWORD_TAIL_FISH);
    _particleMat.DisableKeyword(KEYWORD_TAIL_SQUASH);

    switch (_manager.trailMode) {
      case TrailMode.Fish:
        _particleMat.EnableKeyword(KEYWORD_TAIL_FISH);
        break;
      case TrailMode.Squash:
        _particleMat.EnableKeyword(KEYWORD_TAIL_SQUASH);
        break;
    }

    if (_stochasticSamplingEnabled) {
      _simulationMat.EnableKeyword(KEYWORD_STOCHASTIC);

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
      _simulationMat.SetTexture(PROP_STOCHASTIC, coordinates);
    } else {
      _simulationMat.DisableKeyword(KEYWORD_STOCHASTIC);
    }
  }


  public void RebuildTrailTexture() {
    if (!Application.isPlaying) {
      return;
    }

    Texture2D ramp = new Texture2D(TAIL_RAMP_RESOLUTION, 1, TextureFormat.Alpha8, mipmap: false, linear: true);
    for (int i = 0; i < TAIL_RAMP_RESOLUTION; i++) {
      float speed = i / (float)TAIL_RAMP_RESOLUTION;
      float length = _manager.speedToTrailLength.Evaluate(speed);
      ramp.SetPixel(i, 0, new Color(length, length, length, length));
    }
    ramp.Apply(updateMipmaps: false, makeNoLongerReadable: true);

    _particleMat.SetTexture("_TailRamp", ramp);
  }

  #endregion

  #region UNITY MESSAGES
  private void Awake() {
    _manager = GetComponentInParent<SimulationManager>();

    initBlitMeshes();
    buildDisplayMeshes();

    _displayBlock = new MaterialPropertyBlock();
    _displayColorA = new Texture2D(64, 64, TextureFormat.ARGB32, mipmap: false, linear: true);
    _displayColorA.filterMode = FilterMode.Point;
    _displayColorB = new Texture2D(64, 64, TextureFormat.ARGB32, mipmap: false, linear: true);
    _displayColorB.filterMode = FilterMode.Point;
    _handActors.Fill(() => new HandActor(this));
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

    buildSimulationCommands();

    updateShaderData();

    UpdateShaderKeywords();

    RebuildTrailTexture();
  }

  void Update() {
    if (_enableSpeciesDebugColors) {
      throw new System.NotImplementedException();
    }

    updateShaderData();

    updateShaderDebug();

    if (_provider != null && handInfluenceEnabled) {
      _handActors[0].UpdateHand(Hands.Left);
      _handActors[1].UpdateHand(Hands.Right);
    }

    if (_manager.simulationEnabled) {
      _currScaledTime += Time.deltaTime * _manager.simulationTimescale;
      if (_dynamicTimestepEnabled) {
        while (_currSimulationTime < _currScaledTime) {
          _prevSimulationTime = _currSimulationTime;
          _currSimulationTime += 1.0f / _simulationFPS;

          stepSimulation(Mathf.InverseLerp(_currSimulationTime, _prevSimulationTime, _currScaledTime));

          if (_limitStepsPerFrame) {
            break;
          }
        }

        float lerpValue = Mathf.InverseLerp(_currSimulationTime, _prevSimulationTime, _currScaledTime);
        _displayBlock.SetFloat("_Lerp", lerpValue);
      } else {
        _currSimulationTime = _prevSimulationTime = _currScaledTime;
        stepSimulation(1);
      }
    }

    if (_manager.displayParticles) {
      displaySimulation();
    }
  }
  #endregion

  #region RESET LOGIC

  [System.Serializable]
  public struct SpeciesRect {
    public int x, y, width, height;
    public int species;
  }

  /// <summary>
  /// Restar the simulation using the given description to describe the
  /// initial state of the simulation.
  /// </summary>
  public void RestartSimulation(EcosystemDescription ecosystemDescription, ResetBehavior resetBehavior) {
    if (_resetCoroutine != null) {
      StopCoroutine(_resetCoroutine);
    }
    _resetCoroutine = StartCoroutine(restartCoroutine(ecosystemDescription, resetBehavior));
  }

  private Coroutine _resetCoroutine;
  private IEnumerator restartCoroutine(EcosystemDescription ecosystemDescription, ResetBehavior resetBehavior) {
    _manager.isPerformingTransition = true;

    if (_manager.OnEcosystemBeginTransition != null) {
      _manager.OnEcosystemBeginTransition();
    }

    if (_manager.currentDescription == null) {
      resetBehavior = ResetBehavior.ResetPositions;
    }

    List<SpeciesRect> layout = new List<SpeciesRect>();

    bool isUsingOptimizedLayout = tryCalculateOptimizedLayout(ecosystemDescription.particles, layout);

    if (isUsingOptimizedLayout) {
      _simulationMat.DisableKeyword(KEYWORD_SAMPLING_GENERAL);
      _simulationMat.EnableKeyword(KEYWORD_SAMPLING_UNIFORM);

      _simulationMat.SetInt("_SampleWidth", layout[0].width);
      _simulationMat.SetInt("_SampleHeight", layout[0].height);
    } else {
      _simulationMat.DisableKeyword(KEYWORD_SAMPLING_UNIFORM);
      _simulationMat.EnableKeyword(KEYWORD_SAMPLING_GENERAL);

      calculateLayoutGeneral(ecosystemDescription.particles, layout);
    }

    var colorArray = ecosystemDescription.speciesData.Query().Select(s => (Vector4)s.color).ToArray();

    switch (resetBehavior) {
      case ResetBehavior.None:
        refillColorArrays(layout, colorArray, forceTrueAlpha: true);
        uploadColorTexture(_displayColorA);
        uploadColorTexture(_displayColorB);

        resetBlitMeshes(layout, ecosystemDescription.speciesData, ecosystemDescription.socialData, !isUsingOptimizedLayout);
        if (_manager.OnEcosystemMidTransition != null) {
          _manager.OnEcosystemMidTransition();
        }
        break;
      case ResetBehavior.ResetPositions:
        refillColorArrays(layout, colorArray, forceTrueAlpha: true);
        resetParticleTextures(layout, ecosystemDescription.particles);

        uploadColorTexture(_displayColorA);
        uploadColorTexture(_displayColorB);

        resetBlitMeshes(layout, ecosystemDescription.speciesData, ecosystemDescription.socialData, !isUsingOptimizedLayout);

        _commandIndex = 0;
        Shader.SetGlobalTexture(PROP_POSITION_GLOBAL, _positionSrc);
        Shader.SetGlobalTexture(PROP_VELOCITY_GLOBAL, _velocitySrc);
        Shader.SetGlobalTexture(PROP_SOCIAL_FORCE_GLOBAL, _socialQueueSrc);
        if (_manager.OnEcosystemMidTransition != null) {
          _manager.OnEcosystemMidTransition();
        }
        break;
      case ResetBehavior.FadeInOut:
      case ResetBehavior.SmoothTransition:
        _simulationMat.SetFloat("_ResetRange", _resetRange);
        _simulationMat.SetFloat("_ResetForce", _resetForce * -100);

        bool isIncreasingParticleCount = ecosystemDescription.particles.Count >
                                         _manager.currentDescription.particles.Count;
        if (resetBehavior == ResetBehavior.FadeInOut) {
          _displayColorArray.Fill(new Color(0, 0, 0, 0));
        } else {
          if (isIncreasingParticleCount) {
            refillColorArrays(layout, colorArray, forceTrueAlpha: false);
          } else {
            refillColorArrays(layout, colorArray, forceTrueAlpha: true);
          }
        }

        //Don't upload color to A yet because we need to lerp
        uploadColorTexture(_displayColorB);
        _particleMat.EnableKeyword("COLOR_LERP");

        float startTime = Time.time;
        float endTime = Time.time + _resetTime;
        bool hasUploadedNewSocialMesh = false;
        while (Time.time < endTime) {
          float percent = Mathf.InverseLerp(startTime, endTime, Time.time);
          float resetPercent = _resetSocialCurve.Evaluate(percent);

          if (resetBehavior != ResetBehavior.FadeInOut) {
            _simulationMat.SetFloat("_ResetPercent", resetPercent);
          }

          if (!hasUploadedNewSocialMesh || isIncreasingParticleCount || resetBehavior == ResetBehavior.FadeInOut) {
            _displayBlock.SetFloat("_ColorLerp", resetPercent);
          }

          if (isIncreasingParticleCount && resetBehavior != ResetBehavior.FadeInOut) {
            _headRadiusTransitionDelta = Mathf.Lerp(0, _resetHeadRange - _manager.headRadius, resetPercent);
          }

          float socialPercent = _resetSocialCurve.Evaluate(percent);
          if (socialPercent > 0.99f && !hasUploadedNewSocialMesh) {
            hasUploadedNewSocialMesh = true;
            resetBlitMeshes(layout, ecosystemDescription.speciesData, ecosystemDescription.socialData, !isUsingOptimizedLayout);
            _simulationMat.SetFloat(PROP_SIMULATION_FRACTION, 1.0f / layout.Count);

            if (resetBehavior == ResetBehavior.FadeInOut) {
              refillColorArrays(layout, colorArray, forceTrueAlpha: true);
              uploadColorTexture(_displayColorA);
              resetParticleTextures(layout, ecosystemDescription.particles);
              _commandIndex = 0;
              Shader.SetGlobalTexture(PROP_POSITION_GLOBAL, _positionSrc);
              Shader.SetGlobalTexture(PROP_VELOCITY_GLOBAL, _velocitySrc);
              Shader.SetGlobalTexture(PROP_SOCIAL_FORCE_GLOBAL, _socialQueueSrc);
            } else {
              if (isIncreasingParticleCount) {
                refillColorArrays(layout, colorArray, forceTrueAlpha: true);
                uploadColorTexture(_displayColorA);
              }

              Texture2D randomTexture = new Texture2D(64, 64, TextureFormat.RGBAFloat, mipmap: false, linear: true);
              randomTexture.filterMode = FilterMode.Point;
              randomTexture.SetPixels(new Color[4096].Fill(() => (Vector4)Random.insideUnitSphere * _manager.fieldRadius));
              randomTexture.Apply();

              blitCopy(randomTexture, _socialTemp, PASS_RANDOM_INIT);
              Graphics.CopyTexture(_socialTemp, _positionSrc);
              Graphics.CopyTexture(_socialTemp, _positionDst);

              DestroyImmediate(randomTexture);
            }

            if (_manager.OnEcosystemMidTransition != null) {
              _manager.OnEcosystemMidTransition();
            }
          }

          yield return null;
        }

        //Finish by uploading colors to channel A
        //both channels now have the new color
        uploadColorTexture(_displayColorA);
        _particleMat.DisableKeyword("COLOR_LERP");
        break;
    }

    _simulationMat.SetFloat(PROP_SIMULATION_FRACTION, 1.0f / layout.Count);
    _simulationMat.SetFloat("_ResetPercent", 0);
    _currScaledTime = 0;
    _currSimulationTime = 0;
    _prevSimulationTime = 0;

    _manager.currentDescription = ecosystemDescription;
    _resetCoroutine = null;

    if (_manager.OnEcosystemEndedTransition != null) {
      _manager.OnEcosystemEndedTransition();
    }

    _manager.isPerformingTransition = false;
  }

  private bool tryCalculateOptimizedLayout(List<ParticleDescription> toSpawn, List<SpeciesRect> layout) {
    //TODO: implement optimize layout
    return false;
  }

  private void calculateLayoutGeneral(List<ParticleDescription> toSpawn, List<SpeciesRect> layout) {
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

      for (int dy = 0; dy < _textureDimension; dy++) {
        for (int dx = dx0; dx < dx1; dx++) {
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
        SpeciesRect rect1 = layout[i + 1];
        if (rect0.species != rect1.species) {
          continue;
        }

        if (rect0.y == rect1.y &&
            rect0.x + rect0.width == rect1.x &&
            rect0.height == rect1.height) {
          didCollapse = true;
          rect0.width += rect1.width;
          layout[i] = rect0;
          layout.RemoveAt(i + 1);
          i--;
          continue;
        }

        if (rect0.x == rect1.x &&
           rect0.y + rect0.height == rect1.y &&
           rect0.width == rect1.width) {
          didCollapse = true;
          rect0.height += rect1.height;
          layout[i] = rect0;
          layout.RemoveAt(i + 1);
          i--;
          continue;
        }
      }
    } while (didCollapse);

    if (layout.Count > 100) {
      Debug.Log("Layout had more than 100 rects! (" + layout.Count + ")");
      throw new System.Exception();
    }

    return;
  }

  private void resetParticleTextures(List<SpeciesRect> layout, List<ParticleDescription> toSpawn) {
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
    var speciesMap = new IEnumerator<ParticleDescription>[MAX_SPECIES];
    for (int i = 0; i < MAX_SPECIES; i++) {
      int j = i;
      speciesMap[i] = toSpawn.Query().Where(t => t.species == j).GetEnumerator();
    }

    Texture2D layoutTex = null;
    if (_layoutDebug != null && _layoutDebug.gameObject.activeInHierarchy) {
      layoutTex = new Texture2D(_textureDimension, _textureDimension, TextureFormat.ARGB32, mipmap: false);
      layoutTex.filterMode = FilterMode.Point;
      layoutTex.wrapMode = TextureWrapMode.Clamp;
      _layoutDebug.material.mainTexture = layoutTex;
    }

    int minSpecies = layout.Query().Min(t => t.species);
    int maxSpecies = layout.Query().Max(t => t.species);

    foreach (var rect in layout) {
      var enumerator = speciesMap[rect.species];

      Color speciesDebugCenter = Color.HSVToRGB(Mathf.InverseLerp(minSpecies, maxSpecies + 1, rect.species), 1, 1);
      Color speciesDebugEdge = Color.HSVToRGB(Mathf.InverseLerp(minSpecies, maxSpecies + 1, rect.species), 1, 0.86f);

      for (int dx = rect.x; dx < rect.x + rect.width; dx++) {
        for (int dy = rect.y; dy < rect.y + rect.height; dy++) {
          if (!enumerator.MoveNext()) {
            throw new System.Exception("Enumerator finished too early!  Layout did not match actual particles to spawn!");
          }
          positionColors[dx + dy * _textureDimension] = (Vector4)enumerator.Current.position;
          velocityColors[dx + dy * _textureDimension] = (Vector4)enumerator.Current.velocity;

          if (layoutTex != null) {
            if (dx == rect.x || dx == rect.x + rect.width - 1 || dy == rect.y || dy == rect.y + rect.height - 1) {
              layoutTex.SetPixel(dx, dy, speciesDebugEdge);
            } else {
              layoutTex.SetPixel(dx, dy, speciesDebugCenter);
            }
          }
        }
      }
    }

    if (layoutTex != null) {
      layoutTex.Apply();
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

  private void refillColorArrays(List<SpeciesRect> layout, Vector4[] colors, bool forceTrueAlpha) {
    if (forceTrueAlpha) {
      _displayColorArray.Fill(new Color(0, 0, 0, 0));
    }

    foreach (var rect in layout) {
      Color color = colors[rect.species];
      for (int dx = 0; dx < rect.width; dx++) {
        for (int dy = 0; dy < rect.height; dy++) {
          int x = dx + rect.x;
          int y = dy + rect.y;

          int index = y * 64 + x;

          Color toAssign = color;
          if (!forceTrueAlpha) {
            Color existing = _displayColorArray[index];
            toAssign *= existing.a;
          }

          _displayColorArray[index] = toAssign;
        }
      }
    }
  }

  private void uploadColorTexture(Texture2D texture) {
    texture.SetPixels(_displayColorArray);
    texture.Apply();
    _displayBlock.SetTexture(texture == _displayColorA ? "_ColorA" : "_ColorB", texture);
  }

  private void resetBlitMeshes(List<SpeciesRect> layout,
                               SpeciesDescription[] speciesData,
                               SocialDescription[,] socialData,
                               bool includeRectUv) {
    List<Vector3> verts = new List<Vector3>();
    List<int> tris = new List<int>();
    List<Vector4> uv0 = new List<Vector4>();
    List<Vector4> uv1 = new List<Vector4>();
    List<Vector4> uv2 = new List<Vector4>();

    float velocityScalar = 100;
    float socialScalar = 1000;

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
        social.x = socialData[speciesM, speciesO].socialForce * socialScalar;
        social.y = socialData[speciesM, speciesO].socialRange;
        social.z = (speciesData[speciesM].collisionForce + speciesData[speciesO].collisionForce) * 0.5f * velocityScalar;
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

    _blitMeshInteraction.Clear();
    _blitMeshInteraction.SetVertices(verts);
    _blitMeshInteraction.SetTriangles(tris, 0, calculateBounds: true);
    _blitMeshInteraction.SetUVs(0, uv0);
    _blitMeshInteraction.SetUVs(1, uv1);
    if (includeRectUv) {
      _blitMeshInteraction.SetUVs(2, uv2);
    }
    _blitMeshInteraction.UploadMeshData(markNoLogerReadable: false);

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

    _blitMeshParticle.Clear();
    _blitMeshParticle.SetVertices(verts);
    _blitMeshParticle.SetTriangles(tris, 0, calculateBounds: true);
    _blitMeshParticle.SetUVs(0, uv0);
    _blitMeshParticle.UploadMeshData(markNoLogerReadable: false);

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

    _blitMeshQuad.Clear();
    _blitMeshQuad.SetVertices(verts);
    _blitMeshQuad.SetTriangles(tris, 0, calculateBounds: true);
    _blitMeshQuad.SetUVs(0, uv0);
    _blitMeshQuad.UploadMeshData(markNoLogerReadable: false);
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
      _sphereDeltas[sphereCount] = _handActors[0].deltaMatrix;
      sphereCount++;
    }

    if (_handActors[1].active) {
      _spheres[sphereCount] = _handActors[1].sphere;
      _sphereDeltas[sphereCount] = _handActors[1].deltaMatrix;
      sphereCount++;
    }

    //Transform into local space
    for (int i = 0; i < sphereCount; i++) {
      Vector4 sphere = _spheres[i];
      float w = sphere.w;

      sphere = transform.InverseTransformPoint(sphere);
      sphere.w = w / transform.lossyScale.x;
      _spheres[i] = sphere;

      _sphereDeltas[i] = transform.worldToLocalMatrix * _sphereDeltas[i] * transform.localToWorldMatrix;
    }

    _simulationMat.SetInt("_SphereCount", sphereCount);
    _simulationMat.SetVectorArray("_Spheres", _spheres);
    _simulationMat.SetMatrixArray("_SphereDeltas", _sphereDeltas);
    _simulationMat.SetFloat("_SphereForce", influenceForce);
  }

  private void doHandCollision() {
    if (!_handCollisionEnabled) {
      return;
    }

    int capsuleCount = 0;

    generateCapsulesForHand(Hands.Left, _handActors[0], ref _prevLeft, ref capsuleCount);
    generateCapsulesForHand(Hands.Right, _handActors[1], ref _prevRight, ref capsuleCount);

    //Draw the capsules in world space first
    RuntimeGizmoDrawer drawer;
    if (_handCollisionEnabled && _drawHandColliders && RuntimeGizmoManager.TryGetGizmoDrawer(out drawer)) {
      drawer.color = _handColliderColor;
      for (int i = 0; i < capsuleCount; i++) {
        drawer.DrawWireCapsule(_capsuleA[i], _capsuleB[i], _capsuleA[i].w);
      }
    }

    //Then transform capsules into local space
    for (int i = 0; i < capsuleCount; i++) {
      Vector4 v = _capsuleA[i];
      Vector4 tv = transform.InverseTransformPoint(v);
      tv.w = v.w / transform.lossyScale.x;

      _capsuleA[i] = tv;
      _capsuleB[i] = transform.InverseTransformPoint(_capsuleB[i]);
    }

    _simulationMat.SetFloat("_HandCollisionInverseThickness", 1.0f / (_handCollisionThickness / transform.lossyScale.x));
    _simulationMat.SetFloat("_HandCollisionExtraForce", _extraHandCollisionForce);
    _simulationMat.SetInt("_SocialHandSpecies", _socialHandSpecies);
    _simulationMat.SetFloat("_SocialHandForceFactor", _socialHandEnabled ? _socialHandForceFactor : 0);

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
            float boneScale = _boneCollisionScalars.GetScalar((Bone.BoneType)j);
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
              a.w = radius * boneScale;

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
    public Quaternion rotation, prevRotation;
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
    private Quaternion _prevTrackedRotation;
    private Quaternion _currTrackedRotation;

    private float[] _curls = new float[4];

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

    public Matrix4x4 deltaMatrix {
      get {
        //return Matrix4x4.TRS(prevPosition, prevRotation, Vector3.one).inverse * Matrix4x4.TRS(position, rotation, Vector3.zero);
        return Matrix4x4.TRS(position, rotation, Vector3.one) * Matrix4x4.TRS(prevPosition, prevRotation, Vector3.one).inverse;
      }
    }

    private Vector3 getPositionFromHand(Hand hand) {
      return hand.PalmPosition.ToVector3() + hand.PalmarAxis() * _sim._influenceNormalOffset + hand.DistalAxis() * _sim._influenceForwardOffset;
    }

    public void UpdateHand(Hand hand) {
      _prevTrackedPosition = _currTrackedPosition;
      _prevTrackedRotation = _currTrackedRotation;

      if (hand != null) {
        _currTrackedPosition = getPositionFromHand(hand);
        _currTrackedRotation = hand.Rotation.ToQuaternion();
      }

      if (active && _sim._showHandInfluenceBubble) {
        var meshMat = Matrix4x4.TRS(_currTrackedPosition, Quaternion.identity, Vector3.one * _sim.maxInfluenceRadius * _radiusMultiplier);
        _block.SetFloat("_Glossiness", _alpha * _startingAlpha);
        Graphics.DrawMesh(_sim._influenceMesh, meshMat, _sim._influenceMat, 0, null, 0, _block);
      }
    }

    public void UpdateState(Hand hand, float framePercent) {
      prevPosition = position;
      prevRotation = rotation;
      position = Vector3.Lerp(_prevTrackedPosition, _currTrackedPosition, framePercent);
      rotation = Quaternion.Slerp(_prevTrackedRotation, _currTrackedRotation, framePercent);

      float rawGrab;
      if (hand != null) {
        //First update curls array
        {
          int index = 0;
          foreach (var finger in hand.Fingers) {
            if (finger.Type == Finger.FingerType.TYPE_THUMB) continue;

            _curls[index++] = Vector3.Angle(finger.bones[(int)Bone.BoneType.TYPE_DISTAL].Direction.ToVector3(), hand.DistalAxis()) / 180.0f;
          }
        }

        //Then calculate raw grab
        {
          rawGrab = 1;
          for (int i = 0; i < _curls.Length; i++) {
            rawGrab = Mathf.Min(_curls[i], rawGrab);
          }
        }

        //Then calculate whether or not we are pointing
        {
          //Pointing if any finger is significantly more uncurled than all of the other fingers
          for (int i = 0; i < 4; i++) {

            float minOtherCurl = float.MaxValue;
            float maxOtherCurl = float.MinValue;
            for (int j = 0; j < 4; j++) {
              if (i == j) continue;

              float curl = _curls[j];
              minOtherCurl = Mathf.Min(minOtherCurl, curl);
              maxOtherCurl = Mathf.Max(maxOtherCurl, curl);
            }

            float pointingDelta = minOtherCurl - _curls[i];

            //If the distance between our curl and the next highest curl is
            //greater by X than the distance between the min and the max curl, 
            //we assume this finger is pointing!

            //We also ensure that the delta is greater than a certain minimum,
            //or else we get a singularity when the fingers all have almost the
            //same curl values
            if (pointingDelta > _sim._minPointingDelta &&
                pointingDelta > (maxOtherCurl - minOtherCurl) * _sim._pointingFactor) {
              rawGrab = 0;

              RuntimeGizmoDrawer drawer;
              if (_sim._showIsPointing && RuntimeGizmoManager.TryGetGizmoDrawer(out drawer)) {
                drawer.color = Color.green;
                drawer.DrawSphere(hand.PalmPosition.ToVector3(), 0.03f);
              }

              break;
            }
          }
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
    _simulationMat.SetVector("_FieldCenter", _manager.fieldCenter);
    _simulationMat.SetFloat("_FieldRadius", _manager.fieldRadius);
    _simulationMat.SetFloat("_FieldForce", _manager.fieldForce);

    if (_provider != null) {
      _simulationMat.SetVector("_HeadPos", transform.InverseTransformPoint(_provider.transform.position));
      _simulationMat.SetFloat("_HeadRadius", (_manager.headRadius + _headRadiusTransitionDelta) / transform.lossyScale.x);
    }

    _simulationMat.SetInt("_StochasticCount", Mathf.RoundToInt(Mathf.Lerp(0, 256, _stochasticPercent)));
    _simulationMat.SetFloat("_StochasticOffset", (Time.frameCount % _stochasticCycleCount) / (float)_stochasticCycleCount);
  }

  private void buildDisplayMeshes() {
    _displayMeshes.Clear();
    var particleVerts = _manager.particleMesh.vertices;
    var particleTris = _manager.particleMesh.triangles;
    var particleNormals = _manager.particleMesh.normals;

    List<Vector3> verts = new List<Vector3>();
    List<Vector3> normals = new List<Vector3>();
    List<Vector4> uvs = new List<Vector4>();
    List<int> tris = new List<int>();

    Mesh currMesh = null;
    for (int i = 0; i < MAX_PARTICLES; i++) {
      float x = (i % 64) / 64.0f;
      float y = (i / 64) / 64.0f;

      if (currMesh != null && verts.Count + _manager.particleMesh.vertexCount >= 65536) {
        if (_particlesPerMesh == 0) {
          _particlesPerMesh = i;
        }

        currMesh.SetVertices(verts);
        currMesh.SetNormals(normals);
        currMesh.SetUVs(0, uvs);
        currMesh.SetTriangles(tris, 0, calculateBounds: true);
        currMesh.UploadMeshData(markNoLogerReadable: true);
        currMesh = null;
      }

      if (currMesh == null) {
        currMesh = new Mesh();
        currMesh.name = "Particle Mesh";
        currMesh.hideFlags = HideFlags.HideAndDontSave;
        _displayMeshes.Add(currMesh);

        verts.Clear();
        normals.Clear();
        tris.Clear();
        uvs.Clear();
      }

      tris.AddRange(particleTris.Query().Select(t => t + verts.Count).ToArray());
      verts.AddRange(particleVerts);
      normals.AddRange(particleNormals);
      uvs.Append(particleVerts.Length, new Vector4(x, y, 0, 0));
    }

    currMesh.SetVertices(verts);
    currMesh.SetNormals(normals);
    currMesh.SetUVs(0, uvs);
    currMesh.SetTriangles(tris, 0, calculateBounds: true);
    currMesh.UploadMeshData(markNoLogerReadable: true);
  }

  private void initBlitMeshes() {
    _blitMeshInteraction = new Mesh();
    _blitMeshInteraction.name = "BlitMesh Interaction";

    _blitMeshQuad = new Mesh();
    _blitMeshQuad.name = "BitMesh Quad";

    _blitMeshParticle = new Mesh();
    _blitMeshParticle.name = "BlitMesh Particle";
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

    for (int i = 0; i < _manager.stepsPerTick; i++) {
      Graphics.ExecuteCommandBuffer(_simulationCommands[_commandIndex]);
      _commandIndex++;
      if (_commandIndex == _simulationCommands.Count) {
        _commandIndex = 0;
      }
    }
  }

  private void displaySimulation() {
    foreach (var mesh in _displayMeshes) {
      Graphics.DrawMesh(mesh, transform.localToWorldMatrix, _particleMat, 0, null, 0, _displayBlock);
    }
  }

  private void blitCopy(Texture src, RenderTexture dst, int pass = PASS_COPY) {
    GL.LoadPixelMatrix(0, 1, 0, 1);

    RenderTexture.active = dst;
    GL.Clear(clearDepth: false, clearColor: true, backgroundColor: new Color(0, 0, 0, 0));

    _simulationMat.SetTexture("_CopySource", src);
    _simulationMat.SetPass(pass);

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
    //then this will always wind up being 1 or 2 buffers.

    int bufferIndex = 0;
    do {
      CommandBuffer buffer = new CommandBuffer();
      buffer.name = "Particle Simulation " + bufferIndex;
      buildSimulationCommands(bufferIndex, buffer);

      bufferIndex++;
      _simulationCommands.Add(buffer);
    } while (_positionSrc != startPos ||
            _velocitySrc != startVel ||
            _socialQueueSrc != startSoc);

    Debug.Log("Built " + bufferIndex + " Simulation Command Buffers");
  }

  private void buildSimulationCommands(int bufferIndex, CommandBuffer buffer) {
    buffer.SetViewMatrix(Matrix4x4.identity);
    buffer.SetProjectionMatrix(Matrix4x4.Ortho(0, _textureDimension, 0, _textureDimension, -1, 1));

    blitCommands(buffer, _blitMeshParticle, PROP_VELOCITY_GLOBAL, ref _velocitySrc, ref _velocityDst, PASS_GLOBAL_FORCES);

    particleCommands(buffer);

    blitCommands(buffer, _blitMeshQuad, PROP_SOCIAL_FORCE_GLOBAL, ref _socialQueueSrc, ref _socialQueueDst, PASS_STEP_SOCIAL_QUEUE);

    blitCommands(buffer, _blitMeshParticle, PROP_VELOCITY_GLOBAL, ref _velocitySrc, ref _velocityDst, PASS_DAMP_VELOCITIES_APPLY_SOCIAL_FORCES);

    blitCommands(buffer, _blitMeshParticle, PROP_POSITION_GLOBAL, ref _positionSrc, ref _positionDst, PASS_INTEGRATE_VELOCITIES);

    buffer.SetGlobalTexture(PROP_POSITION_PREV_GLOBAL, _positionDst);
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
    buffer.SetGlobalTexture(PROP_VELOCITY_GLOBAL, _velocityDst);

    Utils.Swap(ref _velocitySrc, ref _velocityDst);
  }
  #endregion

}