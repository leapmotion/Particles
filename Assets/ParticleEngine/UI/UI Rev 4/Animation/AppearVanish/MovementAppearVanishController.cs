using Leap.Unity.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Animation {

  public class MovementAppearVanishController : TweenAppearVanishController {

    [Header("Movement")]

    [QuickButton("Use Current",
                 "setApparentLocalPosition",
                 "Sets this property with the object's current local position.")]
    public Vector3 apparentLocalPosition;

    [QuickButton("Use Current",
                 "setVanishedLocalPosition",
                 "Sets this property with the object's current local position.")]
    public Vector3 vanishedLocalPosition;

    [Header("Animation Curves")]

    [UnitCurve]
    public AnimationCurve movementCurve = DefaultCurve.SigmoidUp;

    protected virtual void Reset() {
      apparentLocalPosition = this.transform.localPosition;
      vanishedLocalPosition = this.transform.localPosition + Vector3.back * 0.20f;
    }

    protected override void updateAppearVanish(float time, bool immediately = false) {
      this.transform.localPosition = Vector3.Lerp(vanishedLocalPosition,
                                                  apparentLocalPosition,
                                                  movementCurve.Evaluate(time));
    }

    /// <summary>
    /// Sets the "apparentLocalPosition" field with the transform's current local
    /// position.
    /// </summary>
    private void setApparentLocalPosition() {
      this.apparentLocalPosition = this.transform.localPosition;
    }

    /// <summary>
    /// Sets the "vanishedLocalPosition" field with the transform's current local
    /// position.
    /// </summary>
    private void setVanishedLocalPosition() {
      this.vanishedLocalPosition = this.transform.localPosition;
    }

  }

}
