using Leap.Unity.Animation;
using Leap.Unity.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Interaction.UI {

  public class ScaleAppearVanishController : TweenAppearVanishController, IAppearVanishController {

    public const float NEAR_ZERO = 0.0001f;

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

    /// <summary>
    /// Enforces a minimum value of 0.0001f for each localScale axis.
    /// </summary>
    public bool enforceNonzeroScale = true;

    /// <summary>
    /// Deactivates this object when its target localScale is zero or very near zero.
    /// </summary>
    public bool deactivateSelfWhenZero = true;

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

      if (enforceNonzeroScale) {
        targetScale = Vector3.Max(targetScale, Vector3.one * NEAR_ZERO);
      }

      this.transform.localScale = targetScale;

      if (deactivateSelfWhenZero) {
        gameObject.SetActive(!(this.transform.localScale.CompMin() <= NEAR_ZERO));
      }
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