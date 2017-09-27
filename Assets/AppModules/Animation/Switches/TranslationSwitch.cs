using Leap.Unity.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Animation {

  public class TranslationSwitch : TweenSwitch {

    #region Inspector

    [Header("Translation")]

    [QuickButton("Use Current",
                 "setOnLocalPosition",
                 "Sets this property with the object's current local position.")]
    public Vector3 onLocalPosition;

    [QuickButton("Use Current",
                 "setOffLocalPosition",
                 "Sets this property with the object's current local position.")]
    public Vector3 offLocalPosition;

    [Header("Animation Curves")]

    [UnitCurve]
    public AnimationCurve movementCurve = DefaultCurve.SigmoidUp;

    #endregion

    #region Unity Events

    protected virtual void Reset() {
      onLocalPosition  = this.transform.localPosition;
      offLocalPosition = this.transform.localPosition + Vector3.back * 0.20f;
    }

    #endregion

    #region Switch Implementation

    protected override void updateSwitch(float time, bool immediately = false) {
      this.transform.localPosition = Vector3.Lerp(offLocalPosition,
                                                  onLocalPosition,
                                                  movementCurve.Evaluate(time));
    }

    #endregion

    #region Support

    /// <summary>
    /// Sets the "onLocalPosition" field with the transform's current local position.
    /// </summary>
    private void setOnLocalPosition() {
      this.onLocalPosition = this.transform.localPosition;
    }

    /// <summary>
    /// Sets the "setOffLocalPosition" field with the transform's current local position.
    /// </summary>
    private void setOffLocalPosition() {
      this.offLocalPosition = this.transform.localPosition;
    }

    #endregion

  }

}
