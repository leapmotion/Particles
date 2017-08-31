using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeneratorManager : MonoBehaviour {

  [Range(0, 2)]
  [SerializeField]
  private float _spawnRadius;
  public float spawnRadius {
    get { return _spawnRadius; }
    set { _spawnRadius = value; }
  }




}
