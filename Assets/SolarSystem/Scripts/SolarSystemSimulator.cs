
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;
using Leap.Unity.DevGui;
using Leap.Unity.Animation;
using Leap.Unity.Attributes;

public class SolarSystemSimulator : MonoBehaviour {

  public static List<IPropertyMultiplier> speedMultiplier = new List<IPropertyMultiplier>();
  public static System.Action OnDestroySystem;
  public static System.Action OnCreateSystem;
  public static System.Action OnUpdateSystem;

  [SerializeField]
  private Planet _planetPrefab;

  [SerializeField]
  private Transform _displayAnchor;

  [Header("Simulation"), DevCategory("Solar System Simulation")]
  [SerializeField, DevValue]
  private bool _simulate;
  public bool simulate {
    get { return _simulate; }
    set { _simulate = value; }
  }

  [Range(0, 10)]
  [SerializeField, DevValue]
  private float _simulationSpeed = 1;

  [SerializeField, DevValue]
  private bool _autoLoop = true;

  [SerializeField, DevValue]
  private float _loopTime = 10;

  [SerializeField, DevValue]
  private bool _autoReset = true;

  [SerializeField, DevValue]
  private float _autoResetDistance = 4;

  [Header("Solar System Generation"), DevCategory]
  [SerializeField, DevValue]
  private int _planetCount = 4;

  [MinMax(0, 1)]
  [SerializeField]
  private Vector2 _orbitRadiusRange = new Vector2(0.05f, 0.3f);

  [SerializeField, DevValue]
  private float _sunMass = 1;

  [SerializeField, DevValue]
  private float _gravitationalConstant = 0.001f;

  [Header("Planet Generation"), DevCategory]
  [MinMax(0, 1)]
  [SerializeField]
  private Vector2 _massRange = new Vector2(0.1f, 0.2f);

  [MinMax(0, 500)]
  [SerializeField]
  private Vector2 _revolutionRange = new Vector2(0.1f, 0.2f);

  [CurveBounds(1, 90)]
  [SerializeField]
  private AnimationCurve _axisTiltDistribution;

  [Range(0, 1)]
  [SerializeField]
  private float _chanceToRevolveBackwards = 0.1f;

  [MinMax(0, 1)]
  [SerializeField]
  private Vector2 _saturationRange;

  [MinMax(0, 1)]
  [SerializeField]
  private Vector2 _valueRange;

  [Header("Planet Orbit Visualization"), DevCategory]
  [SerializeField]
  private Material _planetOrbitMaterial;

  [SerializeField]
  private int _planetOrbitResolution = 32;

  [Header("Comets"), DevCategory]
  [Range(0, 10)]
  [SerializeField, DevValue]
  private int _cometCount = 1;

  [SerializeField, DevValue]
  private int _cometPathLength = 1000;

  [SerializeField, DevValue]
  private int _maxPathStepsPerFrame = 200;

  [SerializeField]
  private Material _cometPathMaterial;

  [SerializeField]
  private Material _cometMaterial;

  [SerializeField]
  private Mesh _cometMesh;

  [Range(0, 0.1f)]
  [SerializeField, DevValue]
  private float _cometScale;

  [UnitCurve]
  [SerializeField]
  private AnimationCurve _cometPathGradient;

  public struct PlanetState {
    public Vector3 position;
    public float mass;
    public float distanceFromCenter;
    public float angle;
    public float angularSpeed;
  }

  public struct CometState {
    public Vector3 position;
    public Vector3 velocity;

    public static CometState Lerp(CometState a, CometState b, float t) {
      CometState c;
      c.position = Vector3.Lerp(a.position, b.position, t);
      c.velocity = Vector3.Lerp(a.velocity, b.velocity, t);
      return c;
    }

    public void Generate2States(float prevTime, float currTime, float stateTime, out CometState prev, out CometState curr) {
      prev.position = position + velocity * (prevTime - stateTime);
      prev.velocity = velocity;

      curr.position = position + velocity * (currTime - stateTime);
      curr.velocity = velocity;
    }
  }

  public class SolarSystemState {
    public const float TIMESTEP = 1 / 60.0f;

    public int frames;
    public float sunMass;
    public float gravivationalConstant;
    public float simTime;
    public List<PlanetState> planets = new List<PlanetState>();
    public List<CometState> comets = new List<CometState>();

    public SolarSystemState Clone() {
      var clone = new SolarSystemState();
      clone.CopyFrom(this);
      return clone;
    }

    public void CopyFrom(SolarSystemState other) {
      frames = other.frames;
      sunMass = other.sunMass;
      gravivationalConstant = other.gravivationalConstant;
      simTime = other.simTime;
      planets = new List<PlanetState>(other.planets);
      comets = new List<CometState>(other.comets);
    }

    public void Step() {
      frames++;
      simTime += TIMESTEP;

      for (int i = 0; i < planets.Count; i++) {
        PlanetState planet = planets[i];

        planet.angle += planet.angularSpeed * TIMESTEP;
        planet.position = new Vector3(planet.distanceFromCenter * Mathf.Cos(planet.angle), 0, planet.distanceFromCenter * Mathf.Sin(planet.angle));

        planets[i] = planet;
      }

      for (int i = 0; i < comets.Count; i++) {
        CometState comet = comets[i];

        for (int j = 0; j < planets.Count; j++) {
          PlanetState planet = planets[j];

          Vector3 toPlanet = planet.position - comet.position;
          float distToPlanet = toPlanet.magnitude;

          Vector3 accelOnComet = 10 * planet.mass * gravivationalConstant * toPlanet / (distToPlanet * distToPlanet * distToPlanet);
          comet.velocity += accelOnComet * TIMESTEP;
        }

        Vector3 toSun = -comet.position;
        float distToSun = toSun.magnitude;

        Vector3 accelToSun = sunMass * gravivationalConstant * toSun / (distToSun * distToSun * distToSun);
        comet.velocity += accelToSun * TIMESTEP;

        comet.position += comet.velocity * TIMESTEP;

        comets[i] = comet;
      }
    }
  }

  //Temp vars
  private List<Vector3> _tempVerts = new List<Vector3>();
  private List<Color32> _tempColors = new List<Color32>();
  private List<int> _tempLines = new List<int>();

  //Simulation vars
  private SolarSystemState _currState;
  private SolarSystemState _prevState;
  private float _simTime;
  private int _simBlockers = 0;

  //Planet vars
  private List<Planet> _spawnedPlanets = new List<Planet>();
  private Mesh _planetOrbitMesh;

  //Comet path vars
  private SolarSystemState _cometPathState;
  private Queue<Vector3>[] _cometPaths;
  private Mesh _cometPathMesh;
  private Dictionary<int, int[]> _trailIndexCache = new Dictionary<int, int[]>();

  #region PUBLIC API

  public int simBlockers {
    get {
      return _simBlockers;
    }
    set {
      _simBlockers = value;
    }
  }

  public float simulationTime {
    get {
      return _simTime;
    }
  }

  public float simulationSpeed {
    get {
      float baseSpeed = _simulationSpeed;
      foreach (var multiplier in speedMultiplier) {
        baseSpeed *= multiplier.multiplier;
      }
      return baseSpeed;
    }
  }

  public SolarSystemState currState {
    get {
      return _currState;
    }
  }

  public SolarSystemState prevState {
    get {
      return _prevState;
    }
  }

  public void RestartPaths() {
    _cometPathState = _currState.Clone();

    _cometPaths = new Queue<Vector3>[_cometPathState.comets.Count];
    for (int i = 0; i < _cometPaths.Length; i++) {
      _cometPaths[i] = new Queue<Vector3>();
    }
  }

  #endregion

  #region UNITY MESSAGES

  private void Start() {
    createSimulation();

    _cometPathMesh = new Mesh();
    _cometPathMesh.MarkDynamic();
    _cometPathMesh.name = "Comet Paths Mesh";
  }

  private void LateUpdate() {
    if (Input.GetKeyDown(KeyCode.Space)) {
      createSimulation();
    }

    if (_autoLoop && _currState.simTime >= _loopTime) {
      createSimulation();
    }

    if (_autoReset) {
      foreach (var comet in _currState.comets) {
        if (comet.position.magnitude > _autoResetDistance) {
          createSimulation();
          break;
        }
      }
    }

    if (_simulate && _simBlockers == 0 && simulationSpeed > Mathf.Epsilon) {
      stepSimulation();
      updatePlanetPositions();
    }

    updateTrails();
    displayComets();

    Graphics.DrawMesh(_planetOrbitMesh, _displayAnchor.localToWorldMatrix, _planetOrbitMaterial, 0);
  }

  #endregion

  #region SIMULATION

  [DevButton("Restart Simulation"), DevCategory("Solar System Simulation")]
  private void createSimulation() {
    _simTime = 0;
    if (_currState != null || _prevState != null) {
      destroySimulation();
    }

    _currState = new SolarSystemState();
    _currState.sunMass = _sunMass;
    _currState.gravivationalConstant = _gravitationalConstant;

    //Generate the planets for the solar system
    for (int i = 0; i < _planetCount; i++) {
      var planet = Instantiate(_planetPrefab);
      planet.transform.parent = _displayAnchor;
      planet.transform.localPosition = Vector3.zero;
      planet.transform.localRotation = Quaternion.identity;
      planet.transform.localScale = Vector3.one;

      var planetState = new PlanetState();

      float orbitRadius = Random.Range(_orbitRadiusRange.x, _orbitRadiusRange.y);
      float mass = Random.Range(_massRange.x, _massRange.y);
      float revolutionSpeed = Random.Range(_revolutionRange.x, _revolutionRange.y);
      float axisTilt = _axisTiltDistribution.Evaluate(Random.value);

      float orbitalVelocity = Mathf.Sqrt(_gravitationalConstant * _sunMass / orbitRadius);
      float orbitLength = Mathf.PI * 2 * orbitRadius;
      float orbitPeriod = orbitLength / orbitalVelocity;
      float angularSpeed = Mathf.PI * 2 / orbitPeriod;

      if (Random.value > _chanceToRevolveBackwards) {
        revolutionSpeed = -revolutionSpeed;
      }

      float hue = Random.value;
      float saturation = Random.Range(_saturationRange.x, _saturationRange.y);
      float value = Random.Range(_valueRange.x, _valueRange.y);

      planetState.angle = Random.Range(0, Mathf.PI * 2);
      planetState.angularSpeed = angularSpeed;
      planetState.distanceFromCenter = orbitRadius;
      planetState.mass = mass;

      planet.gameObject.SetActive(true);
      planet.Init(revolutionSpeed, mass, axisTilt, Color.HSVToRGB(hue, saturation, value));

      _currState.planets.Add(planetState);
      _spawnedPlanets.Add(planet);
    }

    //Add starting commets
    for (int i = 0; i < _cometCount; i++) {
      var rot = Quaternion.Euler(0, Random.Range(0, 360), 0);
      _currState.comets.Add(new CometState() {
        position = rot * (Vector3.right * 0.3f + Vector3.up * 0.05f),
        velocity = rot * (Vector3.forward * 0.4f)
      });
    }

    _prevState = _currState.Clone();

    //Generate planet orbit mesh
    {
      foreach (var planet in _currState.planets) {
        for (int i = 0; i < _planetOrbitResolution; i++) {
          _tempLines.Add(i + _tempVerts.Count);
          _tempLines.Add((i + 1) % _planetOrbitResolution + _tempVerts.Count);
        }

        for (int i = 0; i < _planetOrbitResolution; i++) {
          float angle = Mathf.PI * 2 * i / (float)_planetOrbitResolution;
          float dx = Mathf.Cos(angle) * planet.distanceFromCenter;
          float dz = Mathf.Sin(angle) * planet.distanceFromCenter;
          _tempVerts.Add(new Vector3(dx, 0, dz));
        }
      }

      if (_planetOrbitMesh == null) {
        _planetOrbitMesh = new Mesh();
      }
      _planetOrbitMesh.Clear();
      _planetOrbitMesh.SetVertices(_tempVerts);
      _planetOrbitMesh.SetIndices(_tempLines.ToArray(), MeshTopology.Lines, 0);

      _tempVerts.Clear();
      _tempLines.Clear();
    }

    //Restart the comet paths
    RestartPaths();

    if (OnCreateSystem != null) {
      OnCreateSystem();
    }
  }

  private void destroySimulation() {
    if (OnDestroySystem != null) {
      OnDestroySystem();
    }

    _currState = null;
    _prevState = null;

    foreach (var planet in _spawnedPlanets) {
      DestroyImmediate(planet.gameObject);
    }
    _spawnedPlanets.Clear();
  }

  private void stepSimulation() {
    _simTime += Time.deltaTime * simulationSpeed;

    while (_simTime > _currState.simTime) {
      _prevState.CopyFrom(_currState);
      _currState.Step();
    }

    if (OnUpdateSystem != null) {
      OnUpdateSystem();
    }
  }

  private void updateTrails() {
    int stepsTaken = 0;
    while (_cometPathState.frames < (_currState.frames + _cometPathLength)) {
      _cometPathState.Step();
      stepsTaken++;

      for (int i = 0; i < _cometPaths.Length; i++) {
        _cometPaths[i].Enqueue(_cometPathState.comets[i].position);
        while (_cometPaths[i].Count > _cometPathLength) {
          _cometPaths[i].Dequeue();
        }
      }

      if (stepsTaken >= _maxPathStepsPerFrame) {
        break;
      }
    }

    if (_cometPathState.frames - _currState.frames >= _cometPathLength) {
      using (new ProfilerSample("Build Comet Paths")) {
        _tempVerts.Clear();
        _tempLines.Clear();

        using (new ProfilerSample("Build Vertex List")) {
          for (int i = 0; i < _cometPaths.Length; i++) {
            int index = 0;
            foreach (var point in _cometPaths[i]) {
              if (index != 0) {
                _tempLines.Add(_tempVerts.Count);
                _tempLines.Add(_tempVerts.Count - 1);
              }

              _tempVerts.Add(point);
              _tempColors.Add(_cometPathGradient.Evaluate(index / (_cometPaths[i].Count - 1.0f)) * Color.white);

              index++;
            }
          }
        }

        int[] indexArray;
        using (new ProfilerSample("Build Index Array")) {
          int goalLength = Mathf.NextPowerOfTwo(_tempLines.Count);

          if (!_trailIndexCache.TryGetValue(goalLength, out indexArray)) {
            indexArray = new int[goalLength];
            _trailIndexCache[goalLength] = indexArray;
          }

          for (int i = 0; i < _tempLines.Count; i++) {
            indexArray[i] = _tempLines[i];
          }

          for (int i = _tempLines.Count; i < goalLength; i++) {
            indexArray[i] = 0;
          }
        }

        using (new ProfilerSample("Upload Mesh")) {
          _cometPathMesh.Clear();
          _cometPathMesh.SetVertices(_tempVerts);
          _cometPathMesh.SetColors(_tempColors);
          _cometPathMesh.SetIndices(indexArray, MeshTopology.Lines, 0);

          _tempVerts.Clear();
          _tempLines.Clear();
          _tempColors.Clear();
        }
      }
    }

    Graphics.DrawMesh(_cometPathMesh, _displayAnchor.localToWorldMatrix, _cometPathMaterial, 0);
  }

  private void updatePlanetPositions() {
    float interpFactor = Mathf.InverseLerp(_prevState.simTime, _currState.simTime, _simTime);
    for (int i = 0; i < _currState.planets.Count; i++) {
      _spawnedPlanets[i].transform.localPosition = Vector3.Lerp(_prevState.planets[i].position, _currState.planets[i].position, interpFactor);
    }
  }

  private void displayComets() {
    float interpFactor = Mathf.InverseLerp(_prevState.simTime, _currState.simTime, _simTime);
    for (int i = 0; i < _currState.comets.Count; i++) {
      Vector3 cometPos;

      if (i >= _prevState.comets.Count) {
        cometPos = _currState.comets[i].position;
      } else {
        cometPos = Vector3.Lerp(_prevState.comets[i].position, _currState.comets[i].position, interpFactor);
      }

      Matrix4x4 transform = _displayAnchor.localToWorldMatrix *
                            Matrix4x4.TRS(cometPos, Quaternion.identity, Vector3.one * _cometScale);

      Graphics.DrawMesh(_cometMesh, transform, _cometMaterial, 0);
    }
  }

  #endregion
}
