using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulatorToggleFastForward : SimulatorToggleControl {

  void Start() {
    if (simManager != null) {
      //TODO
      //simManager.dynamicTimestepEnabled = true;
    }
  }

  protected override void onToggle() {
    simManager.simulationTimescale = 2F;
  }

  protected override void onUntoggle() {
    simManager.simulationTimescale = 1F;
  }

}
