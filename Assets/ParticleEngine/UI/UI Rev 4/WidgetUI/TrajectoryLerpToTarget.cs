using Leap.Unity.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Animation {

  public class TrajectoryLerpToTarget : TweenBehaviour,
                                        IMoveToTarget {

    #region Inspector

    [Header("Trajectory")]

    public TrajectorySimulator simulator;

    [Header("Animation")]

    public Vector3 targetPosition;

    [MinValue(0.001f)]
    public float   lerpDuration = 1f;

    [UnitCurve]
    public AnimationCurve lerpToPositionCurve = DefaultCurve.SigmoidUp;

    #endregion

    #region Tween

    private Tween _tween;

    private Tween CreateAnimationTween(float duration) {
      _tween = Tween.Single().Value(0f, 1f, onTweenValue)
                             .OverTime(lerpDuration);
      return _tween;
    }

    private void onTweenValue(float f) {
      updateLerp(lerpToPositionCurve.Evaluate(f));
    }

    #endregion

    private void updateLerp(float t) {
      bool finished = t == 1f;
      this.transform.position = Vector3.Lerp(simulator.GetSimulatedPosition(),
                                             targetPosition,
                                             t);
      if (finished) {
        OnReachTarget();
      }
    }

    #region IMoveToTarget

    Vector3 IMoveToTarget.targetPosition {
      get { return targetPosition; }
      set { targetPosition = value; }
    }

    public event Action OnReachTarget;

    public void Cancel() {
      if (_tween.isValid && _tween.isRunning) {
        _tween.Stop();
      }
    }

    public void MoveToTarget(Vector3? targetPosition = null,
                             float?   movementDuration = null) {
      if (targetPosition.HasValue) {
        this.targetPosition = targetPosition.Value;
      }
      if (movementDuration.HasValue) {
        lerpDuration = movementDuration.Value;
      }

      CreateAnimationTween(lerpDuration).Play();
    }

    #endregion

  }

}
