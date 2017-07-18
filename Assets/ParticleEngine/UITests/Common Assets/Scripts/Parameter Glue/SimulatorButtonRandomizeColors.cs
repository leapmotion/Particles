using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulatorButtonRandomizeColors : SimulatorButtonControl {

  public override void onPress() {
    simulator.RandomizeEcosystemColors();
  }

}
