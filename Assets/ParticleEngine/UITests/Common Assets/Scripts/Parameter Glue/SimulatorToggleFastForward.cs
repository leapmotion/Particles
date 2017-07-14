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
    simulator.simulationTimescale = 2F;
  }

  protected override void onUntoggle() {
    simulator.simulationTimescale = 1F;
  }

}
