using Leap.Unity.Attributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScaleAppearVanishController : MonoBehaviour {

  public Transform localScaleTarget;

  [MinValue(0f)]
  public float animTime = 1f;

  [UnitCurve]
  public AnimationCurve scaleCurve;

  void Reset() {
    if (localScaleTarget == null) localScaleTarget = this.transform;
  }



}
