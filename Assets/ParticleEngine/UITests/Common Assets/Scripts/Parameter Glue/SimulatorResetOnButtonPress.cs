using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulatorResetOnButtonPress : SimulatorButtonControl {

  public override void onPress() {
    simulator.ResetPositions();
  }

}
