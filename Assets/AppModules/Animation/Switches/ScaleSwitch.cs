using Leap.Unity.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Animation {

  public class ScaleSwitch : TweenSwitch, IPropertySwitch {

    public const float NEAR_ZERO = 0.0001f;

    #region Inspector

    [Header("Scale Control")]

    /// <summary> The target to animate. </summary>
    public Transform localScaleTarget;

    /// <summary>
    /// Any animation curve values multiply this value to set the localScale of the
    /// target transform.
    /// </summary>
    [SerializeField]
    public Vector3 baseLocalScale = Vector3.one;

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

    protected virtual void Reset() {
      if (localScaleTarget == null) localScaleTarget = this.transform;
    }

    #endregion

    #region Switch Implementation

    protected override void updateSwitch(float time, bool immediately = false) {
#if UNITY_EDITOR
      if (!Application.isPlaying) {
        UnityEditor.Undo.RecordObject(localScaleTarget.transform, "Update Scale Switch");
      }
#endif

      Vector3 targetScale = getTargetScale(time);

      if (enforceNonzeroScale) {
        targetScale = Vector3.Max(targetScale, Vector3.one * NEAR_ZERO);
      }

      localScaleTarget.localScale = targetScale;

      if (deactivateSelfWhenZero) {
        this.gameObject.SetActive(!(targetScale.CompMin() <= NEAR_ZERO));
      }
    }

    private Vector3 getTargetScale(float time) {
      if (!nonUniformScale) {
        return baseLocalScale * scaleCurve.Evaluate(time);
      }
      else {
        return new Vector3(baseLocalScale.x * xScaleCurve.Evaluate(time),
                           baseLocalScale.y * yScaleCurve.Evaluate(time),
                           baseLocalScale.z * zScaleCurve.Evaluate(time));
      }
    }

    #endregion

  }

}