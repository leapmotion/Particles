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

  [Range(0, 0.15f)]
  public float hysteresis = 0.01f;

  protected override bool ShouldGestureActivate(Hand leftHand, Hand rightHand) {
    throw new System.NotImplementedException();
  }

  protected override bool ShouldGestureDeactivate(Hand leftHand, Hand rightHand, out DeactivationReason? deactivationReason) {
    throw new System.NotImplementedException();
  }

  private bool isHandInsideCylinder(Hand hand, Hand target, float radius, float height) {
    Vector3 palmPos = target.PalmPosition.ToVector3();
    Vector3 normalVector = target.PalmNormal.ToVector3();

    Vector3 tipPos = hand.GetIndex().Bone(Bone.BoneType.TYPE_DISTAL).NextJoint.ToVector3();
    Vector3 deltaTip = tipPos - palmPos;

    Vector3 projected = Vector3.Project(deltaTip, normalVector);

    float heightAxis = projected.magnitude;
    float radialAxis = (deltaTip - projected).magnitude;

    return heightAxis < (height / 2) && radialAxis < radius;
  }

  public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
    var leftHand = Hands.Left;
    var rightHand = Hands.Right;

    var handThatIsTapped = tappedHand == TappedHand.Left ? leftHand : rightHand;
    var handThatTaps = tappedHand == TappedHand.Left ? rightHand : leftHand;

    if (handThatIsTapped == null) {
      return;
    }

    switch (_state) {
      case State.Inactive:
        drawer.color = handThatTaps == null ? Color.gray : Color.white;
        break;
      case State.Hover:
        drawer.color = Color.yellow;
        break;
      case State.Active:
        drawer.color = Color.green;
        break;
    }



    drawer.DrawSphere(handThatIsTapped.PalmPosition.ToVector3(), 0.02f);
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
