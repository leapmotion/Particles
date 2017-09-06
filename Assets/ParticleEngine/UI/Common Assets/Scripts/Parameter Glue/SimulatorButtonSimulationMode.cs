using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulatorButtonSimulationMode : SimulatorButtonControl {

  public SimulationMethod modeOnPress;

  public override void onPress() {
    simManager.simulationMethod = modeOnPress;
  }

}
