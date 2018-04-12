using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulatorButtonSimulationMode : SimulatorButtonControl {

  public SimulationModeController simulationModeController;

  public SimulationModeController.SimulationMode modeOnPress;

  protected override void Reset() {
    base.Reset();

    if (simulationModeController == null) {
      simulationModeController = FindObjectOfType<SimulationModeController>();
    }
  }

  public override void onPress() {
    simulationModeController.mode = modeOnPress;
  }

}
