using Leap.Unity;
using Leap.Unity.Animation;
using Leap.Unity.Interaction;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolbeltController : MonoBehaviour {

  [Tooltip("The controller that makes the toolbelt anchor follow the player.")]
  public FollowingController followingController;

  [Tooltip("The target transform to move when animating the position of the toolbelt "
         + "relative to the player. This transform is manipulated in local space.")]
  public Transform toolbeltAnchor;
  
  public InteractionButton openCloseButton;

  [Header("Open/Close Animation")]
  public State _state = State.Closed;
  public enum State { Closed, Opened }

  [Space]
  public float openCloseTime = 1f;

  //public Pose localClosedPose = Pose.zero;

  public Vector3 localClosedPosition = Vector3.zero;
  public Vector3 localClosedEuler    = Vector3.zero;

  //public Pose localOpenPose   = Pose.zero;

  public Vector3 localOpenPosition = Vector3.zero;
  public Vector3 localOpenEuler    = Vector3.zero;

  public Action OnOpenBegin     = () => { };
  public Action OnOpenComplete  = () => { };
  public Action OnCloseBegin    = () => { };
  public Action OnCloseComplete = () => { };

  private Tween _openCloseTween;
  private float _openCloseTweenTime;
  private Pose _baseLocalTargetPose;

  void Start() {
    openCloseButton.OnPress += onPress;

    createOpenCloseTween();

    _baseLocalTargetPose = toolbeltAnchor.ToLocalPose();
  }

  private void createOpenCloseTween() {
    _openCloseTween = Tween.Persistent()
                           .Value(0f, 1f, (x) => { _openCloseTweenTime = x; })
                           .OverTime(openCloseTime)
                           .Smooth(SmoothType.Smooth)
                           .OnReachEnd(OnOpenComplete)
                           .OnReachStart(OnCloseComplete);
  }

  void OnDestroy() {
    if (_openCloseTween.isValid) {
      _openCloseTween.Release();
    }
  }

  private void onPress() {
    if (_state == State.Closed) {
      transitionToOpen();
    }
    else if (_state == State.Opened) {
      transitionToClosed();
    }
  }

  private void transitionToOpen() {
    _openCloseTween.Play(Direction.Forward);

    _state = State.Opened;

    OnOpenBegin();
  }

  private void transitionToClosed() {
    _openCloseTween.Play(Direction.Backward);

    _state = State.Closed;

    OnCloseBegin();
  }

  private float _lastOpenCloseTweenTime = -1f;

  void Update() {
    if (_lastOpenCloseTweenTime != _openCloseTweenTime) {
      var localClosedPose = new Pose(localClosedPosition, Quaternion.Euler(localClosedEuler));
      var localOpenPose   = new Pose(localOpenPosition,   Quaternion.Euler(localOpenEuler));
      var localUpdatePose = Pose.Interpolate(localClosedPose, localOpenPose, _openCloseTweenTime);

      toolbeltAnchor.transform.SetLocalPose(_baseLocalTargetPose + localUpdatePose);

      toolbeltAnchor.transform.localPosition = localUpdatePose.position;
      toolbeltAnchor.transform.localRotation = localUpdatePose.rotation;

      _lastOpenCloseTweenTime = _openCloseTweenTime;
    }
  }

}
