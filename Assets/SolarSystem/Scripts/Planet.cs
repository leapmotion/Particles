using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Planet : MonoBehaviour {

  [SerializeField]
  private Transform _orbitAnchor;

  [SerializeField]
  private Renderer _renderer;

  [SerializeField]
  private SolarSystemSimulator _simulator;

  private float _revolutionSpeed;

  public void Init(float revolutionSpeed, float mass, float axisTilt, Color color) {
    _revolutionSpeed = revolutionSpeed;
    _orbitAnchor.eulerAngles = new Vector3(axisTilt, Random.Range(0, 360), 0);

    _renderer.material.color = color;
    _renderer.transform.localScale = Vector3.one * mass;
  }

  private void LateUpdate() {
    _renderer.transform.localEulerAngles = new Vector3(0, 0, _revolutionSpeed * _simulator.simulationTime);
  }
}

