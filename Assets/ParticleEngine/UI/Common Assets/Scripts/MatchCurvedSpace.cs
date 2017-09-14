using Leap.Unity.Space;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class MatchCurvedSpace : MonoBehaviour {

  public LeapSpace leapSpace;

  private void Reset() {
    if (leapSpace == null) {
      leapSpace = FindObjectOfType<LeapSpace>();
    }
  }

  public Vector3 localRectangularPosition = Vector3.zero;

  private void Update() {
    if (leapSpace != null) {
      if (leapSpace.transformer != null) {
        this.transform.position =
          leapSpace.transform.TransformPoint(
            leapSpace.transformer.TransformPoint(
              leapSpace.transform.InverseTransformPoint(
                this.transform.parent.TransformPoint(
                  localRectangularPosition))));
      }
    }
  }

}
