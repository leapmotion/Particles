using Leap.Unity.Animation;
using Leap.Unity.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Interaction.UI {

  public class ScaleAppearVanishController : TweenAppearVanishController, IAppearVanishController {

    #region Inspector

    [Header("Scale Control")]

    /// <summary> The target to animate. </summary>
    public Transform localScaleTarget;

    /// <summary>
    /// Any animation curve values multiply this value to set the localScale
    /// of the target transform.
    /// </summary>
    [Disable, SerializeField]
    private Vector3 _baseLocalScale = Vector3.one;

    [Header("Animation Curves")]
    [UnitCurve]
    [DisableIf("nonUniformScale", isEqualTo: true)]
    public AnimationCurve scaleCurve = DefaultCurve.SigmoidUp;

    public bool nonUniformScale = false;

    [UnitCurve]
    [DisableIf("nonUniformScale", isEqualTo: false)]
    public AnimationCurve xScaleCurve = DefaultCurve.SigmoidUp;
    [UnitCurve]
    [DisableIf("nonUniformScale", isEqualTo: false)]
    public AnimationCurve yScaleCurve = DefaultCurve.SigmoidUp;
    [UnitCurve]
    [DisableIf("nonUniformScale", isEqualTo: false)]
    public AnimationCurve zScaleCurve = DefaultCurve.SigmoidUp;

    #endregion

    #region Unity Events

    void Reset() {
      if (localScaleTarget == null) localScaleTarget = this.transform;
    }

    void OnValidate() {
      _baseLocalScale = this.transform.localScale;
    }

    #endregion

    #region Scale Appear/Vanish

    protected override void updateAppearVanish(float time, bool immediately = false) {
      Vector3 targetScale = getTargetScale(time);

      this.transform.localScale = targetScale;
    }

    private Vector3 getTargetScale(float time) {
      if (!nonUniformScale) {
        return _baseLocalScale * scaleCurve.Evaluate(time);
      }
      else {
        return new Vector3(_baseLocalScale.x * xScaleCurve.Evaluate(time),
                           _baseLocalScale.y * yScaleCurve.Evaluate(time),
                           _baseLocalScale.z * zScaleCurve.Evaluate(time));
      }
    }

    #endregion

  }

}