using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;
using Leap.Unity.Query;
using Leap.Unity.Interaction;

public class RegularPinchTranslate : MonoBehaviour {

  public GrabSwitch left, right;
  public bool twoHandedOnly = false;
  public InteractionManager manager;

  private void OnDisable() {
    left.grasped = false;
    right.grasped = false;
  }

  private void Update() {
    if (Hands.Left != null) {
      left.Position = Hands.Left.GetPinchPosition();
      left.Rotation = Hands.Left.Rotation.ToQuaternion();

      if (left.grasped) {
        if (Hands.Left.PinchStrength < 0.3f) {
          left.grasped = false;
        }
      } else {
        if (Hands.Left.PinchStrength > 0.7f &&
            Vector3.Angle(Camera.main.transform.forward,
                          Hands.Left.GetPinchPosition() - Camera.main.transform.position) < 50) {
          left.grasped = true;
        }
      }
    } else {
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
        if (Hands.Right.PinchStrength > 0.7f &&
            Vector3.Angle(Camera.main.transform.forward,
                          Hands.Right.GetPinchPosition() - Camera.main.transform.position) < 50) {
          right.grasped = true;
        }
      }
    } else {
      right.grasped = false;
    }

    if (twoHandedOnly) {
      if (right.grasped != left.grasped) {
        right.grasped = false;
        left.grasped = false;
      }
    }

    //Can't move the world if any interaction hand is grasping anything
    if (manager.interactionControllers.Query().Any(t => t.isGraspingObject)) {
      left.grasped = false;
      right.grasped = false;
    }
  }
}
