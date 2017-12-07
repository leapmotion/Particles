using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Attributes;

public class SolarSystemSimulator : MonoBehaviour {

  [SerializeField]
  private GameObject _planetPrefab;

  [MinValue(0)]
  [SerializeField]
  private int _planetCount = 4;

  [MinValue(0)]
  [SerializeField]
  private float _maxOrbitRadius;


  public struct PlanetState {
    public Vector3 position;
    public float mass;
    public float distanceFromCenter;
    public float angle;
    public float angularSpeed;
  }

  public struct CometState {
    public float mass;
    public Vector3 position;
    public Vector3 velocity;
  }

  public class SolarySystemState {
    public const float TIMESTEP = 1 / 60.0f;

    public List<PlanetState> planets = new List<PlanetState>();
    public List<CometState> comets = new List<CometState>();

    public void Step() {

    }
  }





}
