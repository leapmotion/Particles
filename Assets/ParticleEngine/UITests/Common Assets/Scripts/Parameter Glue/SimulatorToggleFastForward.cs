using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulatorToggleFastForward : SimulatorToggleControl {

  void Start() {
    if (simulator != null) {
      simulator.dynamicTimestepEnabled = true;
    }
  }

  protected override void onToggle() {
    Debug.Log("toggled");
    simulator.simulationTimescale = 2F;
  }

  protected override void onUntoggle() {
    Debug.Log("untoggled");
    simulator.simulationTimescale = 1F;
  }

}
