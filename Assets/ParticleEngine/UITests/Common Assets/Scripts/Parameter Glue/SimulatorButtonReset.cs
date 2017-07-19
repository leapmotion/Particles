using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulatorButtonReset : SimulatorButtonControl {

  public override void onPress() {
    simulator.RestartSimulation();
  }

}
