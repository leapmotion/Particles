using Leap.Unity.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanelOutlineStateController : MonoBehaviour {

  public enum State {
    Open,
    Outline,
    Closed
  }

  public InteractionBehaviour widget;
  public Transform panel;
  public Transform panelOutline;
  private float _outlineHintDelay = 0.5F;

  [Header("Auto")]
  public State state = State.Open;

  private float _grabIdleDuration = 0F;
  private Vector3 _lastGrabPosition;
  private float _grabIdleThreshold = 0.01F;

  void Start() {
    widget.OnGraspBegin += onGraspBegin;
  }

  void Update() {
    if (widget.isGrasped && state == State.Open) {
      transitionToClosed();
    }

    if (widget.isGrasped) {
      Vector3 grabPosition = widget.rigidbody.position;

      _grabIdleDuration += Time.deltaTime;

      float displacement = (grabPosition - _lastGrabPosition).magnitude;
      if (displacement > _grabIdleThreshold) {
        _grabIdleDuration = 0F;
      }

      if (_grabIdleDuration > _outlineHintDelay && state == State.Closed) {
        transitionToOutline();
      }
      else if (_grabIdleDuration < _outlineHintDelay && state == State.Outline) {
        transitionToClosed();
      }

      _lastGrabPosition = grabPosition;
    }

    if (!widget.isGrasped && (state == State.Outline || state == State.Closed)) {
      transitionToOpen();
    }
  }

  private void onGraspBegin() {
    _grabIdleDuration = 0F;
    _lastGrabPosition = widget.rigidbody.position;
  }

  private void transitionToClosed() {
    panel.gameObject.SetActive(false);
    panelOutline.gameObject.SetActive(false);

    state = State.Closed;
  }

  private void transitionToOutline() {
    panel.gameObject.SetActive(false);
    panelOutline.gameObject.SetActive(true);

    state = State.Outline;
  }

  private void transitionToOpen() {
    panel.gameObject.SetActive(true);
    panelOutline.gameObject.SetActive(false);

    state = State.Open;
  }

}
