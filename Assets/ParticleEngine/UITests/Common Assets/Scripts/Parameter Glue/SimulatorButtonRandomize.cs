using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulatorButtonRandomize : SimulatorButtonControl {

  public override void onPress() {
    simulator.RandomizeSimulation(forcePositionReset: false);
  }

}
