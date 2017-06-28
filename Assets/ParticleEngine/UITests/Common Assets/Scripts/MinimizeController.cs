using Leap.Unity.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class MinimizeController : MonoBehaviour {

  public Animator minimizeAnimator;

  [Tooltip("If checked, will automatically deactivate InteractionButtons in its "
         + "children.")]
  public bool autoDeactivateButtons = true;
  public UnityEvent OnShouldDeactivateUI;

  [Tooltip("If checked, will automatically reactivate InteractionButtons in its "
         + "children.")]
  public bool autoReactivateButtons = true;
  public UnityEvent OnShouldReactivateUI;

  private List<InteractionButton> _buttons = new List<InteractionButton>();

  void Start() {
    _buttons.AddRange(GetComponentsInChildren<InteractionButton>());
  }

  public void SetMinimized(bool isMinimized) {
    minimizeAnimator.SetBool("isMinimized", isMinimized);
  }

  public void DeactivateUI() {
    if (autoDeactivateButtons) {
      foreach (var button in _buttons) {
        button.gameObject.SetActive(false);
      }
    }

    OnShouldDeactivateUI.Invoke();
  }

  public void ReactivateUI() {
    if (autoReactivateButtons) {
      foreach (var button in _buttons) {
        button.gameObject.SetActive(true);
      }
    }

    OnShouldReactivateUI.Invoke();
  }

}
