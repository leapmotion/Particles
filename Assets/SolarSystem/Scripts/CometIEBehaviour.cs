using UnityEngine;
using Leap;
using Leap.Unity;
using Leap.Unity.Attributes;
using Leap.Unity.Interaction;

public class CometIEBehaviour : MonoBehaviour {

  public SolarSystemIE system;
  public LeapProvider provider;
  public InteractionBehaviour ie;
  public Renderer[] renderers;
  public Transform rotationAnchor;

  [MinMax(0, 0.15f)]
  public Vector2 distanceToMultiplier;

  [Header("Speed Control")]
  public Transform speedHandle;
  public Transform rodHandle;
  public float speedToDistance = 1;
  public float maxPinchDist = 0.05f;
  [MinMax(0, 1)]
  public Vector2 pinchRange = new Vector2(0.2f, 0.4f);

  private bool _isPinched = false;
  private int _pinchingId = 0;

  public void UpdateState(SolarSystemSimulator.CometState comet) {
    transform.localPosition = comet.position;
    transform.localRotation = Quaternion.LookRotation(comet.velocity);
    rotationAnchor.localRotation = Quaternion.identity;

    float dist = comet.velocity.magnitude * speedToDistance;
    speedHandle.localPosition = new Vector3(0, 0, dist);
    rodHandle.localScale = new Vector3(1, 1, dist);
  }

  public float GetMultiplier(SolarSystemSimulator.CometState comet) {
    if (ie.isGrasped) {
      return 0;
    }

    if (_isPinched) {
      return 0;
    }

    float minDist = distanceToMultiplier.y;
    foreach (var hand in provider.CurrentFrame.Hands) {
      float grabDist = Mathf.Min(Vector3.Distance(hand.PalmPosition.ToVector3(), transform.position),
                                 Vector3.Distance(hand.GetIndex().TipPosition.ToVector3(), transform.position));
      float pinchDist = Mathf.Min(Vector3.Distance(hand.GetPredictedPinchPosition(), speedHandle.position),
                                  Vector3.Distance(hand.GetIndex().TipPosition.ToVector3(), speedHandle.position));

      minDist = Mathf.Min(grabDist, minDist);
      minDist = Mathf.Min(pinchDist, minDist);
    }

    return Mathf.InverseLerp(distanceToMultiplier.x, distanceToMultiplier.y, minDist);
  }

  public void OnMultiplierChange(float multiplier) {
    foreach (var renderer in renderers) {
      renderer.material.color = renderer.material.color.WithAlpha(1 - multiplier);
    }
  }

  public bool GetModifiedState(ref SolarSystemSimulator.CometState comet) {
    if (ie.isGrasped || _isPinched) {
      comet.position = transform.localPosition;
      comet.velocity = (transform.parent.InverseTransformRotation(rotationAnchor.rotation) * Vector3.forward) * speedHandle.localPosition.z / speedToDistance;
      return true;
    } else {
      return false;
    }
  }

  private void Update() {
    updatePinchLogic();
  }

  private void updatePinchLogic() {
    if (_isPinched) {
      Hand pinchingHand = null;
      foreach (var hand in provider.CurrentFrame.Hands) {
        if (hand.Id == _pinchingId) {
          pinchingHand = hand;
          break;
        }
      }

      if (pinchingHand == null || pinchingHand.PinchStrength < pinchRange.x) {
        _isPinched = false;
        return;
      }

      if (ie.isGrasped || system.canArrowControlOrientation) {
        rotationAnchor.LookAt(pinchingHand.GetPinchPosition());
      }

      float speedDistance;
      Vector3 pinchDelta = pinchingHand.GetPinchPosition() - speedHandle.parent.position;
      if (Vector3.Dot(pinchDelta, speedHandle.parent.forward) < 0) {
        speedDistance = 0;
      } else {
        Vector3 projected = Vector3.Project(pinchDelta, speedHandle.parent.forward);
        Vector3 local = speedHandle.parent.InverseTransformVector(projected);
        speedDistance = local.magnitude;
      }

      speedHandle.localPosition = new Vector3(0, 0, speedDistance);
      rodHandle.localScale = new Vector3(1, 1, speedDistance);
    } else {
      Hand pinchingHand = null;
      foreach (var hand in provider.CurrentFrame.Hands) {
        if (hand.PinchStrength > pinchRange.y &&
            Vector3.Distance(hand.GetPinchPosition(), speedHandle.position) < maxPinchDist) {
          pinchingHand = hand;
          break;
        }
      }

      if (pinchingHand != null) {
        _isPinched = true;
        _pinchingId = pinchingHand.Id;
      }
    }
  }
}
