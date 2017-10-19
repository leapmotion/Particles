using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;
using Leap.Unity.Interaction;
using Leap.Unity.Attributes;
using Leap;
using Leap.Unity.RuntimeGizmos;

public class RegularPinchTranslate : MonoBehaviour, IRuntimeGizmoComponent {

  public GrabSwitch left, right;

  float _leftPinchStrengthLastFrame = 0.0f;
  float _rightPinchStrengthLastFrame = 0.0f;

  public const float PINCH_BEGIN_THRESHOLD = 0.7f;
  public const float PINCH_END_THRESHOLD = 0.3f;

  [Header("Additional Pinch Settings")]

  public bool requireFOVAngle = false;
  [DisableIf("requireFOVAngle", isEqualTo: false)]
  public float maximumFOVAngle = 50;

  [Header("Optional -- Grasping Exclusivity")]

  public InteractionHand leftInteractionHand;
  public InteractionHand rightInteractionHand;

  private void Update() {
    UpdateForHand(Hands.Left, ref _leftPinchStrengthLastFrame, left, leftInteractionHand);
    UpdateForHand(Hands.Right, ref _rightPinchStrengthLastFrame, right, rightInteractionHand);
  }

  private void UpdateForHand(Hand hand, ref float pinchStrengthLastFrame,
                             GrabSwitch grabSwitch,
                             Maybe<InteractionHand> maybeIntHand) {
    if (hand == null) {
      pinchStrengthLastFrame = 0.0f;
      grabSwitch.grasped = false;
      return;
    }

    grabSwitch.Position = hand.GetPredictedPinchPosition();
    grabSwitch.Rotation = hand.Rotation.ToQuaternion();

    bool graspedLastFrame = grabSwitch.grasped;

    bool canBeginGrasp = true;
    if (requireFOVAngle) {
      if (Vector3.Angle(grabSwitch.Position - Camera.main.transform.position,
                        Camera.main.transform.forward) > maximumFOVAngle) {
        canBeginGrasp = false;
      }
    }

    if (grabSwitch.grasped) {
      if (hand.PinchStrength < PINCH_END_THRESHOLD) {
        grabSwitch.grasped = false;
      }
    }
    else if (canBeginGrasp) {
      if (hand.PinchStrength > PINCH_BEGIN_THRESHOLD
          && pinchStrengthLastFrame < PINCH_BEGIN_THRESHOLD) {
        grabSwitch.grasped = true;
      }
    }

    if (maybeIntHand.hasValue) {
      var intHand = maybeIntHand.valueOrDefault;
      if (intHand.isGraspingObject) {
        grabSwitch.grasped = false;
      }
    }

    pinchStrengthLastFrame = hand.PinchStrength;
  }

  public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {

  }

}
