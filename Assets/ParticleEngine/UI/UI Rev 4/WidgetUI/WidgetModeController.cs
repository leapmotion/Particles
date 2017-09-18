using Leap.Unity.Animation;
using Leap.Unity.Attributes;
using Leap.Unity.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WidgetModeController : MonoBehaviour {

  [Header("Panel")]
  [SerializeField, ImplementsInterface(typeof(IAppearVanishController))]
  private MonoBehaviour _panelAppearVanish;
  public IAppearVanishController panelAppearVanish {
    get { return _panelAppearVanish as IAppearVanishController; }
  }

  [Header("Ball")]
  [SerializeField, ImplementsInterface(typeof(IAppearVanishController))]
  public MonoBehaviour _ballAppearVanish;
  public IAppearVanishController ballAppearVanish {
    get { return _ballAppearVanish as IAppearVanishController; }
  }

  public void TransitionToBall() {
    panelAppearVanish.Vanish();
    ballAppearVanish.Appear();
  }

  public void TransitionToPanel() {
    panelAppearVanish.Appear();
    ballAppearVanish.Vanish();
  }

  public void TransitionToBallNow() {
    panelAppearVanish.VanishNow();
    ballAppearVanish.AppearNow();
  }

  public void TransitionToPanelNow() {
    panelAppearVanish.AppearNow();
    ballAppearVanish.VanishNow();
  }

}
