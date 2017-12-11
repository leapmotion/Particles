using UnityEngine;
using Leap;
using Leap.Unity;
using Leap.Unity.Attributes;
using Leap.Unity.Interaction;

public class CometIEBehaviour : MonoBehaviour {

  public LeapProvider provider;
  public InteractionBehaviour ie;
  public Renderer[] renderers;

  [MinMax(0, 0.15f)]
  public Vector2 distanceToMultiplier;

  [Header("Speed Control")]
  public Transform speedAnchor;
  public float speedToDistance = 1;
  public float maxPinchDist = 0.05f;
  [MinMax(0, 1)]
  public Vector2 pinchRange = new Vector2(0.2f, 0.4f);

  private bool _isPinched = false;
  private int _pinchingId = 0;

  public void UpdateState(SolarSystemSimulator.CometState comet) {
    transform.localPosition = comet.position;
    transform.localRotation = Quaternion.LookRotation(comet.velocity);
    speedAnchor.localPosition = new Vector3(0, 0, comet.velocity.magnitude * speedToDistance);
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
      float grabDist = Vector3.Distance(hand.PalmPosition.ToVector3(), transform.position);
      float pinchDist = Vector3.Distance(hand.GetPredictedPinchPosition(), speedAnchor.position);

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
      comet.velocity = (transform.localRotation * Vector3.forward) * speedAnchor.localPosition.z / speedToDistance;
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

      float speedDistance;
      Vector3 pinchDelta = pinchingHand.GetPinchPosition() - speedAnchor.parent.position;
      if (Vector3.Dot(pinchDelta, speedAnchor.parent.forward) < 0) {
        speedDistance = 0;
      } else {
        Vector3 projected = Vector3.Project(pinchDelta, speedAnchor.parent.forward);
        Vector3 local = speedAnchor.parent.InverseTransformVector(projected);
        speedDistance = local.magnitude;
      }

      speedAnchor.localPosition = new Vector3(0, 0, speedDistance);
    } else {
      Hand pinchingHand = null;
      foreach (var hand in provider.CurrentFrame.Hands) {
        if (hand.PinchStrength > pinchRange.y &&
            Vector3.Distance(hand.GetPinchPosition(), speedAnchor.position) < maxPinchDist) {
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
