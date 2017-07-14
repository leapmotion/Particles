using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulatorRandomizeOnButtonPress : SimulatorButtonControl {

  public override void onPress() {
    simulator.LoadRandomEcosystem(dontResetPositions: true);
  }

}
