using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap;
using Leap.Unity;
using Leap.Unity.Gestures;
using Leap.Unity.Attributes;


public class PalmTapGesture : TwoHandedGesture {

  public TappedHand tappedHand = TappedHand.Left;
  public TargetSide targetSide = TargetSide.Palm;

  [Range(0, 0.15f)]
  public float areaRadius = 0.05f;

  [Range(0, 0.15f)]
  public float areaHeight = 0.03f;

  [Range(0, 0.15f)]
  public float hysteresis = 0.01f;

  protected override bool ShouldGestureActivate(Hand leftHand, Hand rightHand) {
    var handThatIsTapped = tappedHand == TappedHand.Left ? leftHand : rightHand;
    var handThatTaps = tappedHand == TappedHand.Left ? rightHand : leftHand;

    return isHandInsideCylinder(handThatTaps, handThatIsTapped, 2, 2) &&
          !isHandInsideCylinder(handThatTaps, handThatIsTapped, areaRadius, areaHeight - hysteresis * 0.5f);
  }

  protected override bool ShouldGestureDeactivate(Hand leftHand, Hand rightHand, out DeactivationReason? deactivationReason) {
    var handThatIsTapped = tappedHand == TappedHand.Left ? leftHand : rightHand;
    var handThatTaps = tappedHand == TappedHand.Left ? rightHand : leftHand;

    if (!isHandInsideCylinder(handThatTaps, handThatIsTapped, areaRadius, areaHeight + hysteresis * 0.5f)) {
      deactivationReason = DeactivationReason.FinishedGesture;
      return true;
    } else {
      deactivationReason = null;
      return false;
    }
  }

  private bool isHandInsideCylinder(Hand hand, Hand target, float radius, float height) {
    Vector3 palmPos = target.PalmPosition.ToVector3();
    Vector3 normalVector = target.PalmNormal.ToVector3();

    Vector3 tipPos = hand.GetIndex().Bone(Bone.BoneType.TYPE_DISTAL).NextJoint.ToVector3();
    Vector3 deltaTip = tipPos - palmPos;

    var tipSide = Vector3.Dot(deltaTip, normalVector) > 0 ? TargetSide.Palm : TargetSide.Back;
    if (tipSide != targetSide) {
      return false;
    }

    Vector3 projected = Vector3.Project(deltaTip, normalVector);

    float heightAxis = projected.magnitude;
    float radialAxis = (deltaTip - projected).magnitude;

    return heightAxis < height && radialAxis < radius;
  }

  public enum TargetSide {
    Palm,
    Back
  }

  public enum TappedHand {
    Left,
    Right
  }
}
