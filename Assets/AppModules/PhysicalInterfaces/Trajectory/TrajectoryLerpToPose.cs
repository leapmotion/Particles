using Leap.Unity.Attributes;
using Leap.Unity.PhysicalInterfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Animation {

  public class TrajectoryLerpToPose : MonoBehaviour, IMoveToPose {

    #region Inspector

    [Header("Target")]

    public Pose targetPose;

    [Header("Trajectory")]

    public TrajectorySimulator simulator;

    [Header("Animation")]

    //[MinValue(0.001f)]
    //public float lerpDuration = 1f;

    [UnitCurve]
    public AnimationCurve lerpToPoseCurve = DefaultCurve.SigmoidUp;

    #endregion

    #region Tween

    private Tween _tween;

    private Tween CreateAnimationTween(float duration) {
      _tween = Tween.Single().Value(0f, 1f, onTweenValue)
                             .OverTime(duration);
      return _tween;
    }

    private void onTweenValue(float f) {
      updateLerp(lerpToPoseCurve.Evaluate(f));
    }

    #endregion

    private void updateLerp(float t) {
      this.transform.SetWorldPose(Pose.Interpolate(simulator.GetSimulatedPose(),
                                                   targetPose,
                                                   t));

      // Reset the absolute rotation of the object being simulated;
      // this adds a lot of rotational "drag" but prevents flips due to the complex
      // nature of quaternions :\
      simulator.SetSimulatedRotation(this.transform.rotation);

      bool isFinished = t == 1f;
      if (isFinished) {
        simulator.StopSimulating();

        OnReachTarget();
      }

      OnMovementUpdate();
    }

    #region IMoveToPose

    Pose IMoveToPose.targetPose {
      get { return targetPose; }
      set { targetPose = value; }
    }

    public event Action OnReachTarget;
    public event Action OnMovementUpdate;

    public void Cancel() {
      if (_tween.isValid && _tween.isRunning) {
        _tween.Stop();
        simulator.StopSimulating();
      }
    }

    public void MoveToTarget(Pose? targetPose = null,
                             float? movementDuration = null) {
      if (targetPose.HasValue) {
        this.targetPose = targetPose.Value;
      }

      float duration;
      if (movementDuration.HasValue) {
        duration = movementDuration.Value;
      }
      else {
        duration = getMovementDurationBasedOnVelocity();
      }

      simulator.StartSimulating();

      CreateAnimationTween(duration).Play();
    }

    #endregion

    private float getMovementDurationBasedOnVelocity() {
      if (simulator == null) return 0f;

      return simulator.velocity.magnitude.Map(0.02f, 10f, 0f, 2.0f);
    }

  }

}
