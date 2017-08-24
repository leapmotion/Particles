using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulationWidgetTransitionController : MonoBehaviour {

  public SimulationWidgetStateController stateController;
  public GameObject animationClipTarget;

  [Header("Idle to Warm")]
  public float warmUpSpeed = 1f;
  public AnimationClip idleToWarmClip;

  [Header("Warm To Activated Animation")]
  public float activationSpeed = 2f;
  public AnimationClip warmToActiveClip;

  [Header("Hide/Show Graphics (binary)")]
  public AnimationClip hideShowClip;

  private float _warmUpAmount = 0f;
  private float _activationAmount = 0f;
  public float activationAmount {
    get { return _activationAmount; }
  }

  private bool _warmedUpThisFrame = false;

  void Reset() {
    if (stateController == null) {
      stateController = GetComponent<SimulationWidgetStateController>();
    }

    if (stateController != null && stateController.transitionController == null) {
      stateController.transitionController = this;
    }
  }

  /// <summary>
  /// Called manually by the SimulationWidgetStateController instead of Update() to
  /// guarantee call order.
  /// </summary>
  public void TransitionUpdate() {
    // Decay the "warm up" timer.
    if (!_warmedUpThisFrame && _activationAmount == 0f) {
      _warmUpAmount -= Time.deltaTime * warmUpSpeed;
      if (_warmUpAmount < 0f) _warmUpAmount = 0f;
    }
    _warmedUpThisFrame = false;

    updateAnimationState();
  }

  private void updateAnimationState() {
    if (_activationAmount > 0f) {
      warmToActiveClip.SampleAnimation(animationClipTarget, warmToActiveClip.length * _activationAmount);
    }
    else {
      idleToWarmClip.SampleAnimation(animationClipTarget, idleToWarmClip.length * _warmUpAmount);
    }
  }

  public bool TransitionToWarm() {
    _activationAmount -= Time.deltaTime * activationSpeed;
    if (_activationAmount <= 0f) {
      _activationAmount = 0f;
    }

      _warmUpAmount += Time.deltaTime * warmUpSpeed;
    _warmedUpThisFrame = true;

    if (_warmUpAmount >= 1f) {
      _warmUpAmount = 1f;
      return true;
    }

    return false;
  }

  public bool TransitionToActive() {
    _activationAmount += Time.deltaTime * activationSpeed;

    if (_activationAmount >= 1f) {
      _activationAmount = 1f;
      return true;
    }

    return false;
  }

  public bool TransitionToIdle() {
    _activationAmount -= Time.deltaTime * activationSpeed;

    if (_activationAmount <= 0f) {
      _activationAmount = 0f;
      return true;
    }

    return false;
  }

  public void HideGraphics() {
    hideShowClip.SampleAnimation(animationClipTarget, hideShowClip.length * 0f);
  }

  public void ShowGraphics() {
    hideShowClip.SampleAnimation(animationClipTarget, hideShowClip.length * 1f);
  }

}
