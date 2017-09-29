using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity {

  [System.Serializable]
  public struct Pose {

    public Vector3    position;
    public Quaternion rotation;

    public Pose(Vector3 position, Quaternion rotation) {
      this.position = position;
      this.rotation = rotation;
    }

    public static Pose zero {
      get { return new Pose(); }
    }

    /// <summary>
    /// Returns a pose interpolated (Lerp for position, Slerp for rotation)
    /// between a and b by t from 0 to 1. This method clamps t between 0 and 1; if
    /// extrapolation is desired, see Extrapolate.
    /// </summary>
    public static Pose Interpolate(Pose a, Pose b, float t) {
      if (t >= 1f) return b;
      if (t <= 0f) return a;
      return new Pose(Vector3.Lerp(a.position, b.position, t),
                      Quaternion.Slerp(a.rotation, b.rotation, t));
    }

    /// <summary>
    /// As Interpolate, but doesn't clamp t between 0 and 1. Values above one extrapolate
    /// forwards beyond b, while values less than zero extrapolate backwards past a.
    /// </summary>
    public static Pose Extrapolate(Pose a, Pose b, float t) {
      return new Pose(Vector3.LerpUnclamped(a.position, b.position, t),
                      Quaternion.SlerpUnclamped(a.rotation, b.rotation, t));
    }

    /// <summary>
    /// As Extrapolate, but extrapolates using time values for a and b, and a target time
    /// at which to determine the extrapolated pose.
    /// </summary>
    public static Pose TimedExtrapolate(Pose a, float aTime, Pose b, float bTime,
                                        float extrapolateTime) {
      return Extrapolate(a, b, extrapolateTime.MapUnclamped(aTime, bTime, 0f, 1f));
    }

    // TODO: Determine if addition and subtraction operations are useful.

    /// <summary>
    /// Returns a new pose with the sum of the argument pose positions, and the
    /// rotation product: b.rotation * a.rotation. Note that this "summation" does not
    /// take either rotation into account when summing positions; positions are
    /// interpreted in a non-rotated space.
    /// </summary>
    public static Pose operator +(Pose a, Pose b) {
      return new Pose(b.position + a.position, b.rotation * a.rotation);
    }

    // Pose X, Pose Y
    // (X + (Y - X)) = Y
    // (X.rotation * (Quaternion.Inverse(X.rotation) * Y.rotation))

    ///// <summary>
    ///// Returns the delta pose D such that b + D = a, using the Pose addition operator.
    ///// This operation is equivalent to
    ///// new Pose(a.position - b.position, Quaternion.Inverse(b.rotation) * a.rotation)
    ///// </summary>
    //public static Pose operator -(Pose a, Pose b) {
    //  return new Pose(a.position - b.position, Quaternion.Inverse(b.rotation) * a.rotation);
    //}

  }

  public static class PoseExtensions {

    /// <summary>
    /// Creates a Pose using the transform's localPosition and localRotation.
    /// </summary>
    public static Pose ToLocalPose(this Transform t) {
      return new Pose(t.localPosition, t.localRotation);
    }

    /// <summary>
    /// Creates a Pose using the transform's position and rotation.
    /// </summary>
    public static Pose ToWorldPose(this Transform t) {
      return new Pose(t.position, t.rotation);
    }

    /// <summary>
    /// Sets the localPosition and localRotation of this transform to the argument pose's
    /// position and rotation.
    /// </summary>
    public static void SetLocalPose(this Transform t, Pose localPose) {
      t.localPosition = localPose.position;
      t.localRotation = localPose.rotation;
    }

    /// <summary>
    /// Sets the position and rotation of this transform to the argument pose's
    /// position and rotation.
    /// </summary>
    public static void SetWorldPose(this Transform t, Pose localPose) {
      t.position = localPose.position;
      t.rotation = localPose.rotation;
    }

  }

}
