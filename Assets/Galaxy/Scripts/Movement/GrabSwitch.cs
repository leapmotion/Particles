using Leap.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Pose = Leap.Unity.Pose;

public class GrabSwitch : MonoBehaviour {
  public bool grasped;
  public Vector3 Position;
  public Quaternion Rotation;

  public Pose pose {
    get {
      return new Pose(Position, Rotation);
    }
  }

  public bool autoUpdateTransform;
  void Update() {
    if (autoUpdateTransform) {
      Position = transform.position;
      Rotation = transform.rotation;
    }
  }
}
