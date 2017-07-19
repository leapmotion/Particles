using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulatorSliderSetMaxForceSteps : SimulatorSliderControl {

  protected override void Reset() {
    base.Reset();

    outputFormat = "F0";
  }

  protected override float filterSliderValue(float sliderValue) {
    return Mathf.Round(sliderValue);
  }

  protected override void setSimulatorValue(float sliderValue) {
    simulatorSetters.SetMaxForceSteps(sliderValue);
  }

}
