using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulatorSliderSetBoundingForce : SimulatorSliderControl {

  protected override void SetSimulatorValue(float sliderValue) {
    simulatorSetters.SetBoundingForce(sliderValue);
  }

  protected override float GetSimulatorValue() {
    return simulatorSetters.GetBoundingForce();
  }

  protected override SliderRefreshMode GetRefreshMode() {
    return SliderRefreshMode.OnEcosystemLoad;
  }

}
