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

  protected override void SetSimulatorValue(float sliderValue) {
    simSetters.SetMaxForceSteps(sliderValue);
  }

  protected override float GetSimulatorValue() {
    return simSetters.GetMaxForceSteps();
  }

  protected override SliderRefreshMode GetRefreshMode() {
    return SliderRefreshMode.OnEcosystemLoad;
  }
}
