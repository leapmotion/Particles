using Leap.Unity;
using Leap.Unity.Animation;
using Leap.Unity.Attributes;
using Leap.Unity.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WidgetModeController : MonoBehaviour {

  [Header("Panel")]
  [SerializeField, ImplementsInterface(typeof(IPropertySwitch))]
  private MonoBehaviour _panelAppearVanish;
  public IPropertySwitch panelAppearVanish {
    get { return _panelAppearVanish as IPropertySwitch; }
  }

  [QuickButton("Move Self To",
               "MoveSelfToPanel",
               "Calls MoveSelfToPanel(), moving this transform to match the panel's "
             + "pose while preserving the panel's own position in world space.")]
  public Transform panelTransform;

  [Header("Ball")]

  [SerializeField, ImplementsInterface(typeof(IPropertySwitch))]
  public MonoBehaviour _ballAppearVanish;
  public IPropertySwitch ballAppearVanish {
    get { return _ballAppearVanish as IPropertySwitch; }
  }

  [QuickButton("Move Self To",
               "MoveSelfToBall",
               "Calls MoveSelfToBall(), moving this transform to match the ball's "
             + "pose while preserving the ball's own position in world space.")]
  public Transform ballTransform;

  public void TransitionToBall() {
    panelAppearVanish.Off();

    ballAppearVanish.On();
  }

  public void TransitionToPanel() {
    MoveSelfToBall();

    FaceCamera();

    panelAppearVanish.On();

    ballAppearVanish.Off();
  }

  public void TransitionToBallNow() {
    panelAppearVanish.OffNow();

    ballAppearVanish.OnNow();
  }

  public void TransitionToPanelNow() {
    panelAppearVanish.OnNow();

    ballAppearVanish.OffNow();
  }

  public void MoveSelfToBall() {
    Pose ballPose = new Pose(ballTransform.position, ballTransform.rotation);

    this.transform.SetWorldPose(ballPose);
    ballTransform.SetWorldPose(ballPose);
  }

  public void MoveSelfToPanel() {
    Pose panelPose = new Pose(panelTransform.position, panelTransform.rotation);

    this.transform.SetWorldPose(panelPose);
    panelTransform.SetWorldPose(panelPose);
  }

  public void FaceCamera() {
    Vector3 awayFromCamera = this.transform.position - Camera.main.transform.position;

    this.transform.rotation = Quaternion.LookRotation(awayFromCamera);
  }

}
