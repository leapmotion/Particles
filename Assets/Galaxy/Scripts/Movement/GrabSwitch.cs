using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrabSwitch : MonoBehaviour {
  public bool grasped;
  public Vector3 Position;
  public Quaternion Rotation;

  public bool autoUpdateTransform;
  void Update() {
    if (autoUpdateTransform) {
      Position = transform.position;
      Rotation = transform.rotation;
    }
  }
}
