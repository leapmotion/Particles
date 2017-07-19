using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulatorSliderSetMaxRange : SimulatorSliderControl {

  protected override void setSimulatorValue(float sliderValue) {
    simulatorSetters.SetMaxRange(sliderValue);
  }

}
