using Leap.Unity;
using Leap.Unity.Interaction;
using Leap.Unity.RuntimeGizmos;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WidgetPinchTranslate : MonoBehaviour, IRuntimeGizmoComponent {
  
  [Header("Grab Switches")]

  [SerializeField]
  private GrabSwitch _switchA;

  [SerializeField]
  private GrabSwitch _switchB;

  [Header("Widget Settings")]

  [SerializeField]
  private float _widgetRadius = 0.40f;

  [Header("Optional - Interaction Hand (primary hover / grasping exclusivity)")]

  [SerializeField]
  private InteractionHand _leftIntHand;

  [SerializeField]
  private InteractionHand _rightIntHand;

  [Header("Runtime Debug Gizmos")]

  [SerializeField]
  private bool _drawDebug = false;

  private bool _switchAWithinRange = false;
  private bool _switchBWithinRange = false;

  #region Unity Events

  private float _lastLeftPinchStrength = 0f;
  private float _lastRightPinchStrength = 0f;

  void Update() {

    updateForHand(Hands.Left,
                  _switchA,
                  ref _lastLeftPinchStrength,
                  ref _switchAWithinRange,
                  _leftIntHand);

    updateForHand(Hands.Right,
                  _switchB,
                  ref _lastRightPinchStrength,
                  ref _switchBWithinRange,
                  _rightIntHand);

  }

  #endregion

  #region Switch Update
  
  private void updateForHand(Leap.Hand hand, GrabSwitch grabSwitch,
                             ref float lastPinchStrength,
                             ref bool switchWithinRange,
                             InteractionHand optionalIntHand) {

    if (hand == null) {
      grabSwitch.grasped = false;
      lastPinchStrength = 0.0f;
      return;
    }

    var curPinchStrength = hand.PinchStrength;
    var curPinchPosition = hand.GetPredictedPinchPosition();

    switchWithinRange = Vector3.Distance(curPinchPosition, this.transform.position)
                          < _widgetRadius
                        && (optionalIntHand == null
                            || (!optionalIntHand.isGraspingObject
                                && (!optionalIntHand.isPrimaryHovering
                                    || optionalIntHand.primaryHoverDistance > 0.15f)));

    if (curPinchStrength > 0.7f
        && lastPinchStrength < 0.7f
        && switchWithinRange) {
      grabSwitch.grasped = true;
    }

    if (curPinchStrength < 0.3f) {
      grabSwitch.grasped = false;
    }

    grabSwitch.Position = curPinchPosition;
    grabSwitch.Rotation = hand.Rotation.ToQuaternion();

    lastPinchStrength = hand.PinchStrength;
  }

  #endregion

  private float _targetWidgetAlphaMult = 0f;
  private float _widgetAlphaMult = 0f;

  public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
    if (!this.enabled) return;
    if (!_drawDebug) return;

    // Alpha multiplier
    _targetWidgetAlphaMult = 0f;
    if (_switchAWithinRange || _switchBWithinRange) {
      _targetWidgetAlphaMult = 1f;
    }

    // Sphere gizmo

    drawer.color = LeapColor.forest.WithAlpha(0.2f);
    float initAlpha = 0.2f;
    int numRings = 8;
    for (int i = 0; i < numRings; i++) {
      drawer.color = LeapColor.forest.WithAlpha(Mathf.Lerp(initAlpha, 0f, i / (float)numRings) * _widgetAlphaMult);
      float r = _widgetRadius - (_widgetRadius * (i / (float)numRings));
      drawer.DrawWireSphere(this.transform.position, r);
    }

    var dir = Camera.main.transform.position.From(this.transform.position).normalized;
    drawer.color = LeapColor.forest;
    int numDirRings = 16;
    float angleStep = 1f;
    float initDirAlpha = 0.6f;
    for (int i = 0; i < numDirRings; i++) {
      drawer.color = drawer.color.WithAlpha(Mathf.Lerp(initDirAlpha, 0f, i / (float)numDirRings) * _widgetAlphaMult);
      drawer.DrawWireArc(this.transform.position + (_widgetRadius * dir * Mathf.Sin(angleStep * i * Mathf.Deg2Rad)),
                         dir, dir.Perpendicular(),
                         _widgetRadius * Mathf.Cos(angleStep * i * Mathf.Deg2Rad),
                         1.0f, 32);
    }


    // Grasp gizmos

    float graspGizmoRadius = 0.03f;

    if (_switchA != null) {
      if (_switchA.grasped) {
        drawer.color = LeapColor.mint.WithAlpha(0.7f);
        drawer.DrawWireSphere(_switchA.Position, graspGizmoRadius);

        var dirA = Camera.main.transform.position.From(_switchA.Position);
        drawer.DrawWireArc(_switchA.Position,
                           dirA, dirA.Perpendicular(),
                           graspGizmoRadius, 1.0f, 24);
      }
      else if (_switchAWithinRange) {
        drawer.color = LeapColor.mint.WithAlpha(0.4f);
        drawer.DrawWireSphere(_switchA.Position, graspGizmoRadius * 0.7f);
      }
    }
    if (_switchB != null) {
      if (_switchB.grasped) {
        drawer.color = LeapColor.mint.WithAlpha(0.7f);
        drawer.DrawWireSphere(_switchB.Position, graspGizmoRadius);

        var dirB = Camera.main.transform.position.From(_switchB.Position);
        drawer.DrawWireArc(_switchB.Position,
                           dirB, dirB.Perpendicular(),
                           graspGizmoRadius, 1.0f, 24);
      }
      else if (_switchBWithinRange) {
        drawer.color = LeapColor.mint.WithAlpha(0.4f);
        drawer.DrawWireSphere(_switchB.Position, graspGizmoRadius * 0.7f);
      }
    }

    bool switchAGrasped = _switchA != null && _switchA.grasped;
    bool switchBGrasped = _switchB != null && _switchB.grasped;

    if (switchAGrasped && switchBGrasped) {
      drawer.color = LeapColor.mint.WithAlpha(0.7f);

      var ATowardsB = (_switchB.Position - _switchA.Position).normalized;
      drawer.DrawLine(_switchA.Position + (ATowardsB) * graspGizmoRadius,
                      _switchB.Position - (ATowardsB) * graspGizmoRadius);
    }

    _widgetAlphaMult = Mathf.Lerp(_widgetAlphaMult, _targetWidgetAlphaMult, 5f * Time.deltaTime);
  }

}
