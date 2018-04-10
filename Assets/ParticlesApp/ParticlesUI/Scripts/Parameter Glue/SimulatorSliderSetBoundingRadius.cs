using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulatorSliderSetBoundingRadius : SimulatorSliderControl {

  protected override void SetSimulatorValue(float sliderValue) {
    simulatorSetters.SetBoundingRadius(sliderValue);
    genManager.spawnRadius = sliderValue;
  }

  protected override float GetSimulatorValue() {
    return simulatorSetters.GetBoundingRadius();
  }

  protected override SliderRefreshMode GetRefreshMode() {
    return SliderRefreshMode.OnEcosystemLoad;
  }

}
