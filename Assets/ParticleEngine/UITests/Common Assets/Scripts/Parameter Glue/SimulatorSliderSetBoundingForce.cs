using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulatorSliderSetBoundingForce : SimulatorSliderControl {

  protected override void setSimulatorValue(float sliderValue) {
    simulatorSetters.SetBoundingForce(sliderValue);
  }

}
