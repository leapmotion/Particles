using UnityEngine;

namespace Leap.Unity.PhysicalInterfaces {

  public static class PoseUtil {

    /// <summary>
    /// Returns the rotation thatm akes a transform at objectPosition point its forward
    /// vector at targetPosition and keep its rightward vector parallel with the horizon
    /// defined by a normal of Vector3.up.
    /// 
    /// For example, this will point an interface panel at a user camera while
    /// maintaining the alignment of text and other elements with the horizon line.
    /// </summary>
    /// <returns></returns>
    public static Quaternion FaceTargetWithoutTwist(Vector3 fromPosition, Vector3 targetPosition, bool flip180 = false) {
      return FaceTargetWithoutTwist(fromPosition, targetPosition, Vector3.up, flip180);
    }

    /// <summary>
    /// Returns the rotation that makes a transform at objectPosition point its forward
    /// vector at targetPosition and keep its rightward vector parallel with the horizon
    /// defined by the upwardDirection normal.
    /// 
    /// For example, this will point an interface panel at a user camera while
    /// maintaining the alignment of text and other elements with the horizon line.
    /// </summary>
    public static Quaternion FaceTargetWithoutTwist(Vector3 objectPosition, Vector3 targetPosition, Vector3 upwardDirection, bool flip180 = false) {
      Vector3 objToTarget = -1f * (Camera.main.transform.position - objectPosition);
      Vector3 horizonRight = Vector3.Cross(upwardDirection, objToTarget);
      Vector3 objUp = Vector3.Cross(objToTarget.normalized, horizonRight.normalized);
      return Quaternion.LookRotation((flip180 ? -1 : 1) * objToTarget, objUp);
    }

  }

}