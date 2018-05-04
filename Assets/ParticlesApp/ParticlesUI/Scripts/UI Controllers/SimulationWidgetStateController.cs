using Leap.Unity.Interaction;
using UnityEngine;

public class SimulationWidgetStateController : MonoBehaviour {

  public InteractionBehaviour intObj;
  public SimulationWidgetTransitionController transitionController;
  public SimulationZoomController zoomController;

  public enum State {
    Idle,
    Warm,
    Active
  }
  private State _state = State.Idle;

  public float delayToIdle = 1f;

  [Header("Is-Being-Activated feedback")]
  public GraphicPaletteController paletteController;
  public int notBeingActivatedColorIdx = 1;
  public int beingActivatedColorIdx = 2;

  private float maxActivationDistance = 0.065f;

  private float _deactivateTimer = 100f;
  private bool _activationInProcess = false;

  void Reset() {
    if (intObj == null) {
      intObj = GetComponent<InteractionBehaviour>();
    }

    if (transitionController == null) {
      transitionController = GetComponent<SimulationWidgetTransitionController>();
    }

    if (zoomController == null) {
      zoomController = GetComponent<SimulationZoomController>();
    }
  }

  void Update() {
    if (!zoomController.isFullyZoomedOut) {
      transitionController.HideGraphics();
    }
    else {
      transitionController.ShowGraphics();
    }

    if (_activationInProcess) {
      transitionToActive();
    }
    else {
      if (intObj.isPrimaryHovered
          && (intObj.primaryHoverDistance < maxActivationDistance
              || (transitionController.activationAmount > 0f
                  && intObj.primaryHoverDistance < maxActivationDistance * 1.2f))) {
        _deactivateTimer = 0f;

        if (paletteController != null) {
          paletteController.restingColorIdx = beingActivatedColorIdx;
        }

        if (_state == State.Idle) {
          transitionToWarm();
        }
        if (_state == State.Warm) {
          transitionToActive();
        }
      }
      else {
        if (paletteController != null) {
          paletteController.restingColorIdx = notBeingActivatedColorIdx;
        }

        if (_state == State.Active) {
          if (_deactivateTimer < delayToIdle) {
            _deactivateTimer += Time.deltaTime;
          }

          if (_deactivateTimer >= delayToIdle) {
            transitionToWarm();
          }
        }

        if (_state == State.Warm) {
          transitionToIdle();
        }
      }
    }

    refreshInteractionObjectState();

    transitionController.TransitionUpdate();
  }

  private void refreshInteractionObjectState() {
    intObj.ignoreContact = true;

    switch (_state) {
      case State.Idle:
        intObj.ignoreGrasping = true;
        break;
      case State.Warm:
        intObj.ignoreGrasping = true;
        break;
      case State.Active:
        intObj.ignoreGrasping = false;
        break;
    }
  }

  private void transitionToWarm() {
    if (transitionController.TransitionToWarm()) {
      _state = State.Warm;
    }
  }

  private void transitionToActive() {
    if (transitionController.TransitionToActive()) {
      _state = State.Active;
      _activationInProcess = false;
    }
    else {
      _activationInProcess = true;
    }
  }

  private void transitionToIdle() {
    if (transitionController.TransitionToIdle()) {
      _state = State.Idle;
    }
  }

}
