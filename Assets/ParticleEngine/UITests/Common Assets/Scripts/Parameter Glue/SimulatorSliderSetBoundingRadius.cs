using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulatorSliderSetBoundingRadius : SimulatorSliderControl {

  protected override void setSimulatorValue(float sliderValue) {
    simulatorSetters.SetBoundingRadius(sliderValue);
  }

}
