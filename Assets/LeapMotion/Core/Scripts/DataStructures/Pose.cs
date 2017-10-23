using UnityEngine;

#if UNITY_2017_2_OR_NEWER

using UnityPose = UnityEngine.Pose;

#endif

namespace Leap.Unity {

  /// <summary>
  /// A position and rotation. You can multiply two poses; this acts like Matrix4x4
  /// multiplication, but Poses always have unit scale.
  /// </summary>
  [System.Serializable]
  public struct Pose {

    public Vector3    position;
    public Quaternion rotation;

    public Pose(Vector3 position, Quaternion rotation) {
      this.position = position;
      this.rotation = rotation;
    }

    public static readonly Pose identity = new Pose(Vector3.zero, Quaternion.identity);

    public Pose inverse {
      get {
        var invQ = Quaternion.Inverse(this.rotation);
        return new Pose(-(invQ * this.position), invQ);
      }
    }

    public bool ApproxEquals(Pose other) {
      return position.ApproxEquals(other.position) && rotation.ApproxEquals(other.rotation);
    }

    /// <summary>
    /// Returns Pose B transformed by Pose A, like a transform hierarchy with A as the
    /// parent of B.
    /// </summary>
    public static Pose operator *(Pose A, Pose B) {
      return new Pose(A.position + (A.rotation * B.position),
                      A.rotation * B.rotation);
    }

    /// <summary>
    /// Returns a pose interpolated (Lerp for position, Slerp, NOT Lerp for rotation)
    /// between a and b by t from 0 to 1. This method clamps t between 0 and 1; if
    /// extrapolation is desired, see Extrapolate.
    /// </summary>
    public static Pose Lerp(Pose a, Pose b, float t) {
      if (t >= 1f) return b;
      if (t <= 0f) return a;
      return new Pose(Vector3.Lerp(a.position, b.position, t),
                      Quaternion.Slerp(a.rotation, b.rotation, t));
    }

    /// <summary>
    /// As Lerp, but doesn't clamp t between 0 and 1. Values above one extrapolate
    /// forwards beyond b, while values less than zero extrapolate backwards past a.
    /// </summary>
    public static Pose LerpUnclamped(Pose a, Pose b, float t) {
      return new Pose(Vector3.LerpUnclamped(a.position, b.position, t),
                      Quaternion.SlerpUnclamped(a.rotation, b.rotation, t));
    }

    /// <summary>
    /// As LerpUnclamped, but extrapolates using time values for a and b, and a target
    /// time at which to determine the extrapolated pose.
    /// </summary>
    public static Pose LerpUnclampedTimed(Pose a, float aTime,
                                          Pose b, float bTime,
                                          float extrapolateTime) {
      return LerpUnclamped(a, b, extrapolateTime.MapUnclamped(aTime, bTime, 0f, 1f));
    }

    public override string ToString() {
      return "[Pose | Position: " + this.position.ToString()
           + ", Rotation: " + this.rotation.ToString() + "]";
    }

    public string ToString(string format) {
      return "[Pose | Position: " + this.position.ToString(format)
           + ", Rotation: " + this.rotation.ToString(format) + "]";
    }

    public override bool Equals(object obj) {
      if (!(obj is Pose)) return false;
      else return this.Equals((Pose)obj);
    }
    public bool Equals(Pose other) {
      return other.position == this.position && other.rotation == this.rotation;
    }

    public override int GetHashCode() {
      return new Hash() {
        position,
        rotation
      };
    }

    public static bool operator ==(Pose a, Pose b) {
      return a.Equals(b);
    }

    public static bool operator !=(Pose a, Pose b) {
      return !(a.Equals(b));
    }
    
#if UNITY_2017_2_OR_NEWER

    public static implicit operator UnityPose(Pose leapPose) {
      return new UnityPose(leapPose.position, leapPose.rotation);
    }

    public static implicit operator Pose(UnityPose unityPose) {
      return new Pose(unityPose.position, unityPose.rotation);
    }

#endif

  }

  public static class PoseExtensions {

    public static Vector3 GetVector3(this Matrix4x4 m) { return m.GetColumn(3); }

    public static Quaternion GetQuaternion(this Matrix4x4 m) {
      if (m.GetColumn(2) == m.GetColumn(1)) { return Quaternion.identity; }
      return Quaternion.LookRotation(m.GetColumn(2), m.GetColumn(1));
    }

    public const float EPSILON = 0.0001f;

    public static bool ApproxEquals(this Vector3 v0, Vector3 v1) {
      return (v0 - v1).magnitude < EPSILON;
    }

    public static bool ApproxEquals(this Quaternion q0, Quaternion q1) {
      return (q0.ToAngleAxisVector() - q1.ToAngleAxisVector()).magnitude < EPSILON;
    }

  }

}