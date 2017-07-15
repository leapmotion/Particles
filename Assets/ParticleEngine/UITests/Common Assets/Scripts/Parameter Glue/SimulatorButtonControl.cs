using Leap.Unity.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SimulatorButtonControl : SimulatorUIControl {

  public InteractionButton button;

  protected override void Reset() {
    base.Reset();

    button = GetComponent<InteractionButton>();
  }

  void Start() {
    OnValidate();
  }

  void OnEnable() {
    button.OnPress += onPress;
  }

  void OnDisable() {
    button.OnPress -= onPress;
  }

  public abstract void onPress();

}
