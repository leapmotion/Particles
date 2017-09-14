﻿using Leap.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolbeltMovementController : MonoBehaviour {
  
  public Transform followTarget;

  public float lerpCoeffPerSec = 20F;
  public float slerpCoeffPerSec = 10F;

  public bool stiffened = false;
  public float stiffenedMultiplier = 0.1F;

  public bool locked = false;

  private float _effLerpCoeffPerSec;
  private float _effSlerpCoeffPerSec;

  void Start() {
    _effLerpCoeffPerSec = lerpCoeffPerSec;
    _effSlerpCoeffPerSec = slerpCoeffPerSec;
  }

  void Update() {
    if (locked) {
      _effLerpCoeffPerSec = 0f;
      _effSlerpCoeffPerSec = 0f;
    }

    if (followTarget != null && !locked) {
      float targetLerpCoeffPerSec = lerpCoeffPerSec * (stiffened ? stiffenedMultiplier : 1F);
      _effLerpCoeffPerSec = Mathf.Lerp(_effLerpCoeffPerSec, targetLerpCoeffPerSec, 20F * Time.deltaTime);

      float targetSlerpCoeffPerSec = slerpCoeffPerSec * (stiffened ? stiffenedMultiplier : 1F);
      _effSlerpCoeffPerSec = Mathf.Lerp(_effSlerpCoeffPerSec, targetSlerpCoeffPerSec, 20F * Time.deltaTime);

      // Update position and rotation every frame so we don't see jittering
      if (_hasLastFixedUpdatePose) {
        var timeSinceLastFixedUpdate = Time.time - Time.fixedTime;

        // For example:
        // 0.2s since last fixed update (at this current update)
        // was traveling at 1 m/s
        // so at this current update, we should be at:
        // 1m/s * 0.2s = 0.2m further than the last fixed update pose

        var updatePose = Pose.TimedExtrapolate(_lastFixedUpdatePose, 0f,
                                               _fixedUpdatePose,     Time.fixedDeltaTime,
                                               Time.fixedDeltaTime + timeSinceLastFixedUpdate);

        this.transform.position = updatePose.position;
        this.transform.rotation = updatePose.rotation;
      }
    }
  }

  private Pose _fixedUpdatePose;
  private Pose _lastFixedUpdatePose;
  private bool _hasFixedUpdatePose = false;
  private bool _hasLastFixedUpdatePose = false;

  void FixedUpdate() {
    if (_hasFixedUpdatePose) {
      _lastFixedUpdatePose = _fixedUpdatePose;
      _hasLastFixedUpdatePose = true;

      Vector3 delta = followTarget.transform.position - this.transform.position;
      this.transform.position = Vector3.Lerp(_fixedUpdatePose.position, followTarget.transform.position + (delta * 1F), _effLerpCoeffPerSec * Time.fixedDeltaTime);

      Quaternion targetRotation = Quaternion.LookRotation(followTarget.transform.forward.ProjectOnPlane(Vector3.up));
      this.transform.rotation = Quaternion.Slerp(_fixedUpdatePose.rotation, targetRotation, _effSlerpCoeffPerSec * Time.fixedDeltaTime);
    }

    _fixedUpdatePose = new Pose(this.transform.position, this.transform.rotation);
    _hasFixedUpdatePose = true;
  }

  public void StiffenPosition() {
    stiffened = true;
  }

  public void RelaxPosition() {
    stiffened = false;
  }

}

public static class ToolbeltFollowControllerVector3Extensions {

  public static Vector3 ProjectOnPlane(this Vector3 v, Vector3 n) {
    return Vector3.ProjectOnPlane(v, n);
  }

}