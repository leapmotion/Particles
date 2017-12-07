using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Leap;
using Leap.Unity;
using Leap.Unity.RuntimeGizmos;

public class PalmTapGesture : MonoBehaviour, IRuntimeGizmoComponent {

  public TappedHand tappedHand = TappedHand.Left;
  public TargetSide targetSide = TargetSide.Palm;

  [Range(0, 0.15f)]
  public float areaRadius = 0.05f;

  [Range(0, 0.15f)]
  public float areaHeight = 0.03f;

  [Range(0, 0.15f)]
  public float hysteresis = 0.01f;

  public UnityEvent OnTap;

  private State _state = State.Inactive;

  enum State {
    Inactive,
    Hover,
    Active
  }

  private void Update() {
    var leftHand = Hands.Left;
    var rightHand = Hands.Right;

    if (leftHand == null || rightHand == null) {
      _state = State.Inactive;
      return;
    }

    var handThatIsTapped = tappedHand == TappedHand.Left ? leftHand : rightHand;
    var handThatTaps = tappedHand == TappedHand.Left ? rightHand : leftHand;

    switch (_state) {
      case State.Inactive:
        if (isHandInsideCylinder(handThatTaps, handThatIsTapped, 2, 2, targetSide) &&
           !isHandInsideCylinder(handThatTaps, handThatIsTapped, areaRadius - hysteresis * 0.5f, areaHeight - hysteresis * 0.5f, targetSide)) {
          _state = State.Hover;
          break;
        }

        break;
      case State.Hover:
        if (!isHandInsideCylinder(handThatTaps, handThatIsTapped, 2, 2, targetSide)) {
          _state = State.Inactive;
          break;
        }

        if (isHandInsideCylinder(handThatTaps, handThatIsTapped, areaRadius - hysteresis * 0.5f, areaHeight - hysteresis * 0.5f, targetSide)) {
          _state = State.Active;
          OnTap.Invoke();
          break;
        }

        break;
      case State.Active:
        if (!isHandInsideCylinder(handThatTaps, handThatIsTapped, areaRadius + hysteresis * 0.5f, areaHeight + hysteresis * 0.5f, TargetSide.Back | TargetSide.Palm)) {
          _state = State.Inactive;
          break;
        }

        break;
    }
  }

  private bool isHandInsideCylinder(Hand hand, Hand target, float radius, float height, TargetSide side) {
    Vector3 palmPos = target.PalmPosition.ToVector3();
    Vector3 normalVector = target.PalmNormal.ToVector3();

    Vector3 tipPos = hand.GetIndex().Bone(Bone.BoneType.TYPE_DISTAL).NextJoint.ToVector3();
    Vector3 deltaTip = tipPos - palmPos;

    var tipSide = Vector3.Dot(deltaTip, normalVector) > 0 ? TargetSide.Palm : TargetSide.Back;
    if ((tipSide & side) == 0) {
      return false;
    }

    Vector3 projected = Vector3.Project(deltaTip, normalVector);

    float heightAxis = projected.magnitude;
    float radialAxis = (deltaTip - projected).magnitude;

    return heightAxis < height && radialAxis < radius;
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
