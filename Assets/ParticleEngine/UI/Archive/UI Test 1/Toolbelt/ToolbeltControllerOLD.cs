using Leap.Unity.Animation;
using Leap.Unity.Attributes;
using Leap.Unity.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolbeltControllerOLD : MonoBehaviour {

  public enum AnimState {
    Idle,
    Ready,
    Maximized
  }

  public Camera mainCamera;
  public FollowingController follower;
  public Animator animator;

  [Disable]
  public AnimState animState = AnimState.Idle;

  [Header("Maximize & Minimize Button")]
  public InteractionButton maxMinButton;

  private bool _waitingForMaximizeOrMinimize = false;
  private bool _waitingForMaximize = false;

  void Reset() {
    mainCamera = Camera.main;
    follower = GetComponent<FollowingController>();
    animator = GetComponent<Animator>();
  }

  void Start() {
    if (maxMinButton != null) {
      maxMinButton.OnUnpress += doMaximizeOrMinimize;
    }

    animator.Play("Toolbelt Minimized Ready to Idle", 0, 0F);
  }

  private bool isLookingDown() {
    return mainCamera.transform.forward.y < -0.7F;
  }

  void Update() {
    // Commented-out sections force the toolbelt to go to and stay Ready.
    if (animState == AnimState.Idle
        /*&& isLookingDown()*/) {
      goIdleToReady();
    }
    else if (animState == AnimState.Ready
        && !isLookingDown()) {
      /*goReadyToIdle();*/
    }

    if (_waitingForMaximizeOrMinimize) {
      if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.9F) {
        _waitingForMaximizeOrMinimize = false;
        if (!_waitingForMaximize) {
          animState = AnimState.Ready;
          follower.enabled = true;
        }
      }
    }
  }

  private void goIdleToReady() {
    animator.Play("Toolbelt Idle to Minimized Ready", 0, 0F);
    animState = AnimState.Ready;
  }

  private void goReadyToIdle() {
    animator.Play("Toolbelt Minimized Ready to Idle", 0, 0F);
    animState = AnimState.Idle;
  }

  private void doMaximizeOrMinimize() {
    if (_waitingForMaximizeOrMinimize) return;

    if (animState == AnimState.Ready) {
      animator.Play("Toolbelt Minimized Ready to Maximized", 0, 0F);
      _waitingForMaximizeOrMinimize = true;
      _waitingForMaximize = true;
      animState = AnimState.Maximized;
      follower.enabled = false;
    }
    else if (animState == AnimState.Maximized) {
      animator.Play("Toolbelt Maximized to Minimized Ready", 0, 0F);
      _waitingForMaximizeOrMinimize = true;
      _waitingForMaximize = false;
    }
  }

}
