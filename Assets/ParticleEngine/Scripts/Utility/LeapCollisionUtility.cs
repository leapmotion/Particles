using System.Collections.Generic;
using UnityEngine;
using Leap;
using Leap.Unity;

public static class LeapCollisionUtility {
  public const float DEFAULT_CAPSULE_RADIUS = 0.015f;

  public struct Capsule {
    public Vector3 pointA, pointB;
    public float radius;
  }

  private static List<Capsule> _capsuleRepresentation = new List<Capsule>();

  /// <summary>
  /// Returns a list of capsules that represent a frame object.  This value
  /// is updated whenever you call UpdateCapsuleRepresentation.
  /// </summary>
  public static List<Capsule> capsuleRepresentation {
    get {
      return _capsuleRepresentation;
    }
  }

  /// <summary>
  /// Pass in a leap Frame object to calculate the list of capsules that 
  /// represents all hands that are inside that frame.  If the frame has
  /// no hands in it, the list of capsules will be empty.
  /// </summary>
  public static List<Capsule> UpdateCapsuleRepresentation(Frame frame) {
    _capsuleRepresentation.Clear();

    foreach (var hand in frame.Hands) {
      foreach (var finger in hand.Fingers) {
        foreach (var bone in finger.bones) {
          _capsuleRepresentation.Add(new Capsule() {
            pointA = bone.PrevJoint.ToVector3(),
            pointB = bone.NextJoint.ToVector3(),
            radius = DEFAULT_CAPSULE_RADIUS
          });
        }
      }
    }

    return _capsuleRepresentation;
  }
}
