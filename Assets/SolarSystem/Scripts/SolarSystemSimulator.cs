﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.DevGui;
using Leap.Unity.Attributes;

public class SolarSystemSimulator : MonoBehaviour {

  [SerializeField]
  private Planet _planetPrefab;

  [Header("Simulation")]
  [SerializeField]
  private bool _simulate;
  public bool simulate {
    get { return _simulate; }
    set { _simulate = value; }
  }

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

  [MinMax(0, 1)]
  [SerializeField]
  private Vector2 _revolutionRange = new Vector2(0.1f, 0.2f);

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
  }

  public class SolarSystemState {
    public const float TIMESTEP = 1 / 60.0f;

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
      sunMass = other.sunMass;
      gravivationalConstant = other.gravivationalConstant;
      simTime = other.simTime;
      planets = new List<PlanetState>(other.planets);
      comets = new List<CometState>(other.comets);
    }

    public void Step() {
      simTime += TIMESTEP;

      for (int i = 0; i < planets.Count; i++) {
        PlanetState planet = planets[i];

        planet.angle += planet.angularSpeed * TIMESTEP;
        planet.position = new Vector3(Mathf.Cos(planet.angle), 0, Mathf.Sin(planet.angle));

        planets[i] = planet;
      }

      for (int i = 0; i < comets.Count; i++) {
        CometState comet = comets[i];

        for (int j = 0; j < planets.Count; j++) {
          PlanetState planet = planets[j];

          Vector3 toPlanet = planet.position - comet.position;
          float distToPlanet = toPlanet.magnitude;

          Vector3 accelOnComet = planet.mass * gravivationalConstant * toPlanet / (distToPlanet * distToPlanet * distToPlanet);
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

  private SolarSystemState _currState;
  private SolarSystemState _prevState;
  private float _simTime;

  private List<Planet> _spawnedPlanets = new List<Planet>();

  #region PUBLIC API

  public float simulationTime {
    get {
      return _simTime;
    }
  }

  #endregion

  #region UNITY MESSAGES

  private void Start() {

  }

  private void Update() {
    if (_simulate) {
      stepSimulation();
      updatePlanetPositions();
    }
  }

  #endregion

  #region SIMULATION

  private void createSimulation() {
    if (_currState != null || _prevState != null) {
      destroySimulation();
    }

    _currState = new SolarSystemState();
    for (int i = 0; i < _planetCount; i++) {
      var planet = Instantiate(_planetPrefab);
      var planetState = new PlanetState();

      float orbitRadius = Random.Range(_orbitRadiusRange.x, _orbitRadiusRange.y);
      float mass = Random.Range(_massRange.x, _massRange.y);
      float revolutionSpeed = Random.Range(_revolutionRange.x, _revolutionRange.y);
      float axisTilt = _axisTiltDistribution.Evaluate(Random.value);

      float orbitalVelocity = Mathf.Sqrt(_gravitationalConstant * _sunMass / orbitRadius);
      float orbitLength = Mathf.PI * 2 * orbitRadius;
      float orbitPeriod = orbitalVelocity * orbitLength;
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

      planet.Init(revolutionSpeed, mass, axisTilt, Color.HSVToRGB(hue, saturation, value));

      _currState.planets.Add(planetState);
      _spawnedPlanets.Add(planet);
    }

    _prevState = _currState.Clone();
  }

  private void destroySimulation() {
    _currState = null;
    _prevState = null;

    foreach (var planet in _spawnedPlanets) {
      DestroyImmediate(planet.gameObject);
    }
    _spawnedPlanets.Clear();
  }

  private void stepSimulation() {
    _simTime += Time.deltaTime;

    while (_simTime > _currState.simTime) {
      _prevState.CopyFrom(_currState);
      _currState.Step();
    }
  }

  private void updatePlanetPositions() {
    float interpFactor = Mathf.InverseLerp(_prevState.simTime, _currState.simTime, _simTime);
    for (int i = 0; i < _currState.planets.Count; i++) {
      _spawnedPlanets[i].transform.position = Vector3.Lerp(_prevState.planets[i].position, _currState.planets[i].position, interpFactor);
    }
  }

  #endregion
}
