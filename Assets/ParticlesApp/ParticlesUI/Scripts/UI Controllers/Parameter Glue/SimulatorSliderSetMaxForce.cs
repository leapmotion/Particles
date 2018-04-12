using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulatorSliderSetMaxForce : SimulatorSliderControl {

  protected override void SetSimulatorValue(float sliderValue) {
    simSetters.SetMaxForce(sliderValue);
  }

  protected override float GetSimulatorValue() {
    return simSetters.GetMaxForce();
  }

  protected override SliderRefreshMode GetRefreshMode() {
    return SliderRefreshMode.OnEcosystemLoad;
  }

}
