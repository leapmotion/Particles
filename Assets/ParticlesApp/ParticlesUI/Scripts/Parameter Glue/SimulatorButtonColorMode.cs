using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulatorButtonColorMode : SimulatorButtonControl {

  [Header("Which Color Mode")]
  public ColorMode modeOnPress;

  public override void onPress() {
    simulatorSetters.SetColorMode(modeOnPress);
  }

}
