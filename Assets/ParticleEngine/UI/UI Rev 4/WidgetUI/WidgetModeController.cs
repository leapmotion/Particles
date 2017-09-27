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

  [Header("Ball")]

  [SerializeField, ImplementsInterface(typeof(IPropertySwitch))]
  public MonoBehaviour _ballAppearVanish;
  public IPropertySwitch ballAppearVanish {
    get { return _ballAppearVanish as IPropertySwitch; }
  }

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

    this.transform.SetLocalPose(ballPose);
    ballTransform.SetWorldPose(ballPose);
  }

  public void FaceCamera() {
    Vector3 awayFromCamera = this.transform.position - Camera.main.transform.position;

    this.transform.rotation = Quaternion.LookRotation(awayFromCamera);
  }

}
