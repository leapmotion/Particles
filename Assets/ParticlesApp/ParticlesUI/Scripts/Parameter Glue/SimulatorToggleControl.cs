using Leap.Unity.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SimulatorToggleControl : SimulatorUIControl {

  public InteractionToggle toggle;

  protected override void Reset() {
    base.Reset();

    toggle = GetComponent<InteractionToggle>();
  }

  void Start() {
    OnValidate();
  }

  void OnEnable() {
    toggle.OnToggle   += onToggle;
    toggle.OnUntoggle += onUntoggle;
  }

  void OnDisable() {
    toggle.OnToggle   -= onToggle;
    toggle.OnUntoggle -= onUntoggle;
  }

  protected abstract void onToggle();

  protected abstract void onUntoggle();

}
