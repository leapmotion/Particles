using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Leap;
using Leap.Unity;
using Leap.Unity.Gestures;
using Leap.Unity.RuntimeGizmos;

public class PalmTapGesture : TwoHandedGesture, IRuntimeGizmoComponent {

  public TappedHand tappedHand = TappedHand.Left;
  public TargetSide targetSide = TargetSide.Palm;

  [Range(0, 0.15f)]
  public float areaRadius = 0.05f;

  [Range(0, 0.15f)]
  public float areaHeight = 0.03f;

  [Range(0, 180)]
  public float palmNormalTolerance = 45;

  [Range(0, 180)]
  public float radialAngleTolerance = 45;

  [Range(0, 0.15f)]
  public float hysteresis = 0.01f;

  protected override bool ShouldGestureActivate(Hand leftHand, Hand rightHand) {
    var handThatIsTapped = tappedHand == TappedHand.Left ? leftHand : rightHand;
    var handThatTaps = tappedHand == TappedHand.Left ? rightHand : leftHand;

    if (targetSide == TargetSide.Palm) {
      return doesSatisfy(handThatTaps, handThatIsTapped, facingSameDirection: false, facingRadialAxis: true);
    } else {
      return doesSatisfy(handThatTaps, handThatIsTapped, facingSameDirection: true, facingRadialAxis: false);
    }
  }

  protected override bool ShouldGestureDeactivate(Hand leftHand, Hand rightHand, out DeactivationReason? deactivationReason) {
    var handThatIsTapped = tappedHand == TappedHand.Left ? leftHand : rightHand;
    var handThatTaps = tappedHand == TappedHand.Left ? rightHand : leftHand;

    deactivationReason = DeactivationReason.FinishedGesture;
    if (targetSide == TargetSide.Palm) {
      return !doesSatisfy(handThatTaps, handThatIsTapped, facingSameDirection: false, facingRadialAxis: true);
    } else {
      return !doesSatisfy(handThatTaps, handThatIsTapped, facingSameDirection: true, facingRadialAxis: false);
    }
  }


  private bool doesSatisfy(Hand tappingHand, Hand targetHand, bool facingSameDirection, bool facingRadialAxis) {
    Vector3 tappingNormal = tappingHand.PalmNormal.ToVector3();
    Vector3 targetNormal = targetHand.PalmNormal.ToVector3();

    Vector3 tappingRadial = tappingHand.DistalAxis();
    Vector3 targetRadial = targetHand.RadialAxis();

    float normalAngle;
    if (facingSameDirection) {
      normalAngle = Vector3.Angle(tappingNormal, targetNormal);
    } else {
      normalAngle = Vector3.Angle(tappingNormal, -targetNormal);
    }

    if (normalAngle > palmNormalTolerance) {
      return false;
    }

    float radialAngle;
    if (facingRadialAxis) {
      radialAngle = Mathf.Abs(Vector3.SignedAngle(tappingRadial, targetRadial, targetNormal));
    } else {
      radialAngle = Mathf.Abs(Vector3.SignedAngle(tappingRadial, -targetRadial, targetNormal));
    }

    if (radialAngle > radialAngleTolerance) {
      return false;
    }

    Vector3 targetPalm = targetHand.PalmPosition.ToVector3();
    foreach (var finger in tappingHand.Fingers) {
      Vector3 tip = finger.Bone(Bone.BoneType.TYPE_DISTAL).NextJoint.ToVector3();
      Vector3 tipDelta = tip - targetPalm;
      Vector3 projectedTip = Vector3.Project(tipDelta, targetNormal);

      float heightAxis = projectedTip.magnitude;
      float radialAxis = (tipDelta - projectedTip).magnitude;

      if (heightAxis < areaHeight && radialAxis < areaRadius) {
        return true;
      }
    }

    return false;
  }


  public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
    //var leftHand = Hands.Left;
    //var rightHand = Hands.Right;

    //var handThatIsTapped = tappedHand == TappedHand.Left ? leftHand : rightHand;
    //var handThatTaps = tappedHand == TappedHand.Left ? rightHand : leftHand;

    //if (handThatIsTapped == null) {
    //  return;
    //}

    //if (handThatIsTapped != null && handThatTaps != null) {
    //  if (doesSatisfy(rightHand, leftHand, false, true)) {
    //    drawer.color = Color.green;
    //  }
    //}


    //drawer.DrawSphere(handThatIsTapped.PalmPosition.ToVector3(), 0.04f);
  }

  public enum TargetSide {
    Palm = 1,
    Back = 2
  }

  public enum TappedHand {
    Left,
    Right
  }
}
