using Leap.Unity;
using Leap.Unity.Attributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Animation {

  public class FollowingController : MonoBehaviour {

    #region Inspector

    [Tooltip("This transform will attempt to match the world position (and optionally "
         + "rotation) of this target when it is non-null.")]
    public Transform followTarget;

    [Header("Acceleration")]

    [Tooltip("This coefficient controls linear acceleration; higher valuers result in "
         + "larger accelerations.")]
    public float lerpCoeffPerSec = 20F;

    [Tooltip("Second-order slerp coefficient; larger values cause larger accelerations. "
         + "This coefficient controls angular acceleration.")]
    [DisableIf("followRotation", isEqualTo: false)]
    public float slerpCoeffPerSec = 10F;

    [Header("Rotation Settings")]

    [Tooltip("Disable this to prevent the follower from matching the target's rotation.")]
    public bool followRotation = true;

    public enum RotationType { ProjectForwardToHorizon, Free }
    [Tooltip("The type of rotation the following controller is allowed to perform.")]
    public RotationType rotationType = RotationType.ProjectForwardToHorizon;

    [Header("Stiffening")]

    /// <summary>
    /// Smoothly slows down the following controller. The stiffenedMultiplier property
    /// determines the strength of this effect.
    /// </summary>
    [Tooltip("Smoothly slows down the following controller. The stiffenedMultiplier "
           + "property determines the strength of this effect.")]
    public bool stiffened = false;

    /// <summary>
    /// The multiplier for this follower's lerp and slerp coefficients when the controller
    /// is "stiffened." Capped between 0f and 1f.
    /// </summary>
    [Tooltip("The multiplier for this follower's lerp and slerp coefficients when the "
           + "controller is \"stiffened.\" Capped between 0f and 1f.")]
    [MinValue(0f)]
    [MaxValue(1f)]
    public float stiffenedMultiplier = 0.1F;

    [Tooltip("When locked, the target linear and angular velocities for this controller "
           + "are set to 0.")]
    public bool locked = false;

    #endregion

    #region Unity Events

    private float _effLerpCoeffPerSec;
    private float _effSlerpCoeffPerSec;

    void Start() {
      _effLerpCoeffPerSec = lerpCoeffPerSec;
      _effSlerpCoeffPerSec = slerpCoeffPerSec;
    }

    void Update() {
      //if (locked) {
      //  _effLerpCoeffPerSec = 0f;
      //  _effSlerpCoeffPerSec = 0f;
      //}

      if (followTarget != null) {
        float targetLerpCoeffPerSec = lerpCoeffPerSec * (stiffened ?
                                                           stiffenedMultiplier : 1F)
                                                      * (locked ? 0f : 1f);
        _effLerpCoeffPerSec = Mathf.Lerp(_effLerpCoeffPerSec,
                                         targetLerpCoeffPerSec,
                                         5F * Time.deltaTime);

        float targetSlerpCoeffPerSec = slerpCoeffPerSec * (stiffened ?
                                                             stiffenedMultiplier : 1F)
                                                        * (locked ? 0f : 1f)
                                                        * (followRotation ? 1f : 0f);
        _effSlerpCoeffPerSec = Mathf.Lerp(_effSlerpCoeffPerSec,
                                          targetSlerpCoeffPerSec,
                                          5F * Time.deltaTime);


        // Update position and rotation gradually towards the targets.
        Vector3 targetPosition = followTarget.position;
        this.transform.position = Vector3.Lerp(this.transform.position,
                                               targetPosition,
                                               _effLerpCoeffPerSec * Time.deltaTime);

        if (followRotation) {
          Quaternion targetRotation;
          switch (rotationType) {
            case RotationType.ProjectForwardToHorizon:
              targetRotation =
                Quaternion.LookRotation(followTarget.forward
                                                    .ProjectOnPlane(Vector3.up));
              break;
            default:
              targetRotation = followTarget.rotation;
              break;
          }
          this.transform.rotation = Quaternion.Slerp(this.transform.rotation,
                                                     targetRotation,
                                                     _effSlerpCoeffPerSec * Time.deltaTime);
        }
      }
    }

    #endregion

  }

  #region Extensions

  public static class ToolbeltFollowControllerVector3Extensions {

    public static Vector3 ProjectOnPlane(this Vector3 v, Vector3 n) {
      return Vector3.ProjectOnPlane(v, n);
    }

  }

  #endregion

}