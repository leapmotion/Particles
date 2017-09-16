using Leap.Unity.Attributes;
using Leap.Unity.Space;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Layout {

  [ExecuteInEditMode]
  public class MatchCurvedSpace : MonoBehaviour {

    public LeapSpace leapSpace;

    private void Reset() {
      if (leapSpace == null) {
        leapSpace = FindObjectOfType<LeapSpace>();
      }
    }

    [Header("Manual Specification")]
    public Vector3 localRectangularPosition = Vector3.zero;

    [Header("Or, ILocalPositionProvider (overrides manual spec)")]
    [ImplementsInterface(typeof(ILocalPositionProvider))]
    public MonoBehaviour localPositionProvider = null;

    private void LateUpdate() {
      if (leapSpace != null) {
        if (leapSpace.transformer != null) {
          this.transform.position =
            leapSpace.transform.TransformPoint(
              leapSpace.transformer.TransformPoint(
                leapSpace.transform.InverseTransformPoint(
                  this.transform.parent.TransformPoint(
                    localPositionProvider == null ? localRectangularPosition
                                                  : (localPositionProvider as ILocalPositionProvider)
                                                    .GetLocalPosition(this.transform)))));
        }
      }
    }

  }
  
}
