using Leap.Unity.Query;
using Leap.Unity.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Leap.Unity.Attributes;
using Leap.Unity;

public class ButtonActivationController : MonoBehaviour {

  [Header("Auto-detected Buttons")]
  [SerializeField, Disable]
  [Tooltip("These buttons are auto-detected and cannot be changed.")]
  private List<InteractionButton> _buttons;

  [Header("(Optional) Reversed Buttons")]
  [Tooltip("These buttons will activate when minimized and deactivate when maximized.")]
  public List<InteractionButton> reversedButtons;

  void OnValidate() {
    refreshButtons();
  }

  void Start() {
    refreshButtons();
  }

  private void refreshButtons() {
    _buttons.Clear();
    _buttons.AddRange(GetComponentsInChildren<InteractionButton>());

    List<int> indicesToRemove = Pool<List<int>>.Spawn();
    try {
      for (int i = 0; i < _buttons.Count; i++) {
        if (reversedButtons.Query().Contains(_buttons[i])) {
          indicesToRemove.Add(i);
        }
      }

      foreach (var idx in indicesToRemove) {
        _buttons.RemoveAt(idx);
      }
    }
    finally {
      indicesToRemove.Clear();
      Pool<List<int>>.Recycle(indicesToRemove);
    }
  }

  public void DeactivateButtons() {
    foreach (var button in _buttons) {
      button.gameObject.SetActive(false);
    }

    foreach (var reversedButton in reversedButtons) {
      reversedButton.gameObject.SetActive(true);
    }
  }

  public void ReactivateUI() {
    foreach (var button in _buttons) {
      button.gameObject.SetActive(true);
    }

    foreach (var reversedButton in reversedButtons) {
      reversedButton.gameObject.SetActive(false);
    }
  }

}
