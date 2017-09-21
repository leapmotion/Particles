using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotator : MonoBehaviour {

  public float rate = 0;

  void Update() {
    transform.Rotate(0, 0, Time.deltaTime * rate);
  }
}
