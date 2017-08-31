using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;
using Leap.Unity.Attributes;

public enum ColorMode {
  BySpecies,
  BySpeciesWithMagnitude,
  ByVelocity,
}

public enum TrailMode {
  Fish,
  Squash
}

public enum ResetBehavior {
  None,
  SmoothTransition,
  ResetPositions,
  FadeInOut,
}

public class SimulationManager : MonoBehaviour {
  public const int MAX_PARTICLES = 4096;
  public const int MAX_SPECIES = 31;
  public const int MAX_FORCE_STEPS = 64;

  #region INSPECTOR
  //##################//
  ///      Field      //
  //##################//
  [Header("Field")]
  [Tooltip("Must be a child of this component.")]
  [SerializeField]
  private Transform _fieldCenter;
  public Vector3 fieldCenter {
    get { return _fieldCenter.localPosition; }
  }

  [Range(0, 2)]
  [SerializeField]
  private float _fieldRadius = 1;
  public float fieldRadius {
    get { return _fieldRadius; }
    set { _fieldRadius = value; }
  }

  [Range(0, 0.1f)]
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

  [Range(0, 2)]
  [SerializeField]
  private float _simulationTimescale = 1;
  public float simulationTimescale {
    get { return _simulationTimescale; }
    set { _simulationTimescale = value; }
  }

  [MinValue(1)]
  [SerializeField]
  private int _stepsPerTick = 1;
  public int stepsPerTick {
    get { return _stepsPerTick; }
    set { _stepsPerTick = value; }
  }

  [SerializeField]
  private KeyCode _resetParticlePositionsKey = KeyCode.P;

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
  public Mesh particleMesh {
    get { return _particleMesh; }
    set {
      _particleMesh = value;
      //buildDisplayMeshes();
    }
  }

  [OnEditorChange("colorMode")]
  [SerializeField]
  private ColorMode _colorMode = ColorMode.BySpecies;
  public ColorMode colorMode {
    get { return _colorMode; }
    set {
      _colorMode = value;
      //updateKeywords();
    }
  }

  [OnEditorChange("trailMode")]
  [SerializeField]
  private TrailMode _trailMode = TrailMode.Fish;
  public TrailMode trailMode {
    get { return _trailMode; }
    set {
      _trailMode = value;
      //updateKeywords();
    }
  }

  [OnEditorChange("RebuildTrailTexture")]
  [SerializeField]
  private AnimationCurve _speedToTrailLength;
  public AnimationCurve speedToTrailLength {
    get { return _speedToTrailLength; }
  }

  [SerializeField]
  private KeyCode _ranzomizeColorsKey = KeyCode.C;

  //#######################//
  ///      Ecosystems      //
  //#######################//
  [Header("Ecosystems")]
  [SerializeField]
  private bool _loadEcosystemOnStart = true;

  [SerializeField]
  private KeyCode _saveEcosystemKey = KeyCode.F5;

  [SerializeField]
  private KeyCode _loadEcosystemKey = KeyCode.F6;

  [SerializeField]
  private StreamingFolder _loadingFolder;

  #endregion

  #region PUBLIC API


  public System.Action OnPresetLoaded;
  public System.Action OnEcosystemBeginTransition;
  public System.Action OnEcosystemMidTransition;
  public System.Action OnEcosystemEndedTransition;

  public bool isPerformingTransition { get; set; }

  public EcosystemDescription currentDescription {
    get {
      throw new System.NotImplementedException();
    }
    set {
      throw new System.NotImplementedException();
    }
  }

  public float simulationAge {
    get {
      return _currSimulationTime;
    }
  }

  public Color GetSpeciesColor(int speciesIdx) {
    //var colors = Pool<List<Color>>.Spawn();
    //try {
    //  _particleMat.GetColorArray("_Colors", colors);
    //  if (speciesIdx > colors.Count - 1) {
    //    return Color.black;
    //  } else {
    //    return colors[speciesIdx];
    //  }
    //} finally {
    //  colors.Clear();
    //  Pool<List<Color>>.Recycle(colors);
    //}
    throw new System.NotImplementedException();
  }

  /// <summary>
  /// Restarts the simulation to whatever state it was in when it was
  /// most recently restarted.
  /// </summary>
  public void RestartSimulation() {
    //If we had generated a random simulation, re-generate it so that new settings
    //can take effect.  We assume the name of the description is it's seed!
    if (_currentSimDescription.isRandomDescription) {
      RandomizeSimulation(_currentSimDescription.name, ResetBehavior.SmoothTransition);
    } else {
      RestartSimulation(_currentSimDescription, ResetBehavior.SmoothTransition);
    }
  }

  /// <summary>
  /// Restarts the simulation using a specific preset to choose the
  /// initial conditions.
  /// </summary>
  public void RestartSimulation(EcosystemPreset preset, ResetBehavior resetBehavior = ResetBehavior.FadeInOut) {
    var presetDesc = getPresetDescription(preset);
    copyDescriptionToSlidersIfLinked(presetDesc);

    RestartSimulation(presetDesc, resetBehavior);

    if (OnPresetLoaded != null) {
      OnPresetLoaded();
    }
  }

  /// <summary>
  /// Restarts the simulation using a random description.
  /// </summary>
  public void RandomizeSimulation(ResetBehavior resetBehavior) {
    RestartSimulation(getRandomEcosystemDescription(), resetBehavior);
  }


  /// <summary>
  /// Restarts the simulation using a random description calculated using
  /// the seed value.  The same seed value should always result in the same
  /// simulation description.
  /// </summary>
  public void RandomizeSimulation(string seed, ResetBehavior resetBehavior) {
    RestartSimulation(getRandomEcosystemDescription(seed), resetBehavior);
  }

  /// <summary>
  /// Randomizes the simulation colors of the current simulation.
  /// </summary>
  public void RandomizeSimulationColors() {
    var newRandomColors = getRandomColors();
    for (int i = 0; i < _currentSimDescription.speciesData.Length; i++) {
      _currentSimDescription.speciesData[i].color = newRandomColors[i];
    }

    RestartSimulation(_currentSimDescription, ResetBehavior.None);
  }


  public void ApplySliderValues() {
    ResetBehavior resetBehavior = ResetBehavior.None;
    if (_randomEcosystemSettings.particleCount != _currentSimDescription.toSpawn.Count) {
      resetBehavior = ResetBehavior.SmoothTransition;
    }
    if (_randomEcosystemSettings.speciesCount != _currentSimDescription.toSpawn.Query().CountUnique(t => t.species)) {
      resetBehavior = ResetBehavior.SmoothTransition;
    }

    if (_currentSimDescription.isRandomDescription) {
      RandomizeSimulation(_currentSimDescription.name, resetBehavior);
    } else {
      var preset = (EcosystemPreset)System.Enum.Parse(typeof(EcosystemPreset), _currentSimDescription.name);
      var presetDesc = getPresetDescription(preset);

      float maxForce = float.Epsilon;
      float maxRange = float.Epsilon;
      int maxSteps = 0;
      float maxDrag = float.Epsilon;
      for (int i = 0; i < presetDesc.speciesData.Length; i++) {
        for (int j = 0; j < presetDesc.speciesData.Length; j++) {
          maxForce = Mathf.Max(maxForce, presetDesc.socialData[i, j].socialForce);
          maxRange = Mathf.Max(maxRange, presetDesc.socialData[i, j].socialRange);
        }
        maxSteps = Mathf.Max(maxSteps, presetDesc.speciesData[i].forceSteps);
        maxDrag = Mathf.Max(maxDrag, presetDesc.speciesData[i].drag);
      }

      float forceFactor = _randomEcosystemSettings.maxSocialForce / maxForce;
      float rangeFactor = _randomEcosystemSettings.maxSocialRange / maxRange;
      float dragFactor = _randomEcosystemSettings.dragCenter / maxDrag;

      for (int i = 0; i < presetDesc.speciesData.Length; i++) {
        for (int j = 0; j < presetDesc.speciesData.Length; j++) {
          presetDesc.socialData[i, j].socialForce *= forceFactor;
          presetDesc.socialData[i, j].socialRange *= rangeFactor;
        }

        float percent;
        if (maxSteps == 0) {
          percent = 1;
        } else {
          percent = Mathf.InverseLerp(0, maxSteps, presetDesc.speciesData[i].forceSteps);
        }

        int result = Mathf.FloorToInt(Mathf.Lerp(0, _randomEcosystemSettings.maxForceSteps - 1, percent));
        presetDesc.speciesData[i].forceSteps = result;
        presetDesc.speciesData[i].drag *= dragFactor;
      }

      RestartSimulation(presetDesc, resetBehavior);
    }
  }

  private void copyDescriptionToSlidersIfLinked(SimulationDescription desc) {
    if (!_linkToPresets) return;

    float maxForce = 0;
    float maxRange = 0;
    float maxSteps = 1;
    float maxDrag = 0;
    for (int i = 0; i < desc.speciesData.Length; i++) {
      for (int j = 0; j < desc.speciesData.Length; j++) {
        maxForce = Mathf.Max(maxForce, desc.socialData[i, j].socialForce);
        maxRange = Mathf.Max(maxRange, desc.socialData[i, j].socialRange);
      }
      maxSteps = Mathf.Max(maxSteps, desc.speciesData[i].forceSteps + 1);
      maxDrag = Mathf.Max(maxDrag, desc.speciesData[i].drag);
    }

    _randomEcosystemSettings.maxSocialForce = maxForce;
    _randomEcosystemSettings.maxSocialRange = maxRange;
    _randomEcosystemSettings.maxForceSteps = Mathf.RoundToInt(maxSteps);
    _randomEcosystemSettings.dragCenter = maxDrag;
    _randomEcosystemSettings.particleCount = desc.toSpawn.Count;
    _randomEcosystemSettings.speciesCount = desc.toSpawn.Query().CountUnique(t => t.species);
  }

  #endregion

  #region UNITY MESSAGES

  #endregion

  #region PRIVATE IMPLEMENTATION


  private void handleUserInput() {
    if (Input.GetKeyDown(_loadPresetEcosystemKey)) {
      RestartSimulation(_presetEcosystemSettings.ecosystemPreset, ResetBehavior.ResetPositions);
    }

    if (Input.GetKeyDown(_randomizeEcosystemKey)) {
      RandomizeSimulation(ResetBehavior.SmoothTransition);
    }

    if (Input.GetKeyDown(_resetParticlePositionsKey)) {
      RestartSimulation();
    }

    if (Input.GetKeyDown(_applyLinkedSliders)) {
      ApplySliderValues();
    }

    if (Input.GetKeyDown(_ranzomizeColorsKey)) {
      RandomizeSimulationColors();
    }

    if (Input.GetKeyDown(_saveEcosystemKey)) {
      File.WriteAllText(_currentSimDescription.name + ".json", JsonUtility.ToJson(_currentSimDescription, prettyPrint: false));
    }

    if (Input.GetKeyDown(_loadEcosystemKey)) {
      var file = Directory.GetFiles(_loadingFolder.Path).Query().FirstOrDefault(t => t.EndsWith(".json"));
      var description = JsonUtility.FromJson<SimulationDescription>(File.ReadAllText(file));
      RestartSimulation(description, ResetBehavior.ResetPositions);
    }
  }

  #endregion


}
