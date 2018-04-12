using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulatorSliderSetBoundingForce : SimulatorSliderControl {

  protected override void SetSimulatorValue(float sliderValue) {
    simSetters.SetBoundingForce(sliderValue);
  }

  protected override float GetSimulatorValue() {
    return simSetters.GetBoundingForce();
  }

  protected override SliderRefreshMode GetRefreshMode() {
    return SliderRefreshMode.OnEcosystemLoad;
  }

}
