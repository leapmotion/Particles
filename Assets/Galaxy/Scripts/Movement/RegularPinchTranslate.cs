using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;
using Leap.Unity.Interaction;

public class RegularPinchTranslate : MonoBehaviour {

  public GrabSwitch left, right;

  [Header("Optional -- Grasping Exclusivity")]

  public InteractionHand leftInteractionHand;
  public InteractionHand rightInteractionHand;

  private void Update() {
    if (Hands.Left != null) {
      left.Position = Hands.Left.GetPinchPosition();
      left.Rotation = Hands.Left.Rotation.ToQuaternion();

      if (left.grasped) {
        if (Hands.Left.PinchStrength < 0.3f) {
          left.grasped = false;
        }
      } else {
        if (Hands.Left.PinchStrength > 0.7f) {
          left.grasped = true;
        }
      }

      if (leftInteractionHand != null && leftInteractionHand.isGraspingObject) {
        left.grasped = false;
      }
    }
    else {
      left.grasped = false;
    }

    if (Hands.Right != null) {
      right.Position = Hands.Right.GetPinchPosition();
      right.Rotation = Hands.Right.Rotation.ToQuaternion();

      if (right.grasped) {
        if (Hands.Right.PinchStrength < 0.3f) {
          right.grasped = false;
        }
      } else {
        if (Hands.Right.PinchStrength > 0.7f) {
          right.grasped = true;
        }
      }

      if (rightInteractionHand != null && rightInteractionHand.isGraspingObject) {
        right.grasped = false;
      }
    }
    else {
      right.grasped = false;
    }
  }
}
