using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulatorSliderSetDrag : SimulatorSliderControl {

  protected override void SetSimulatorValue(float sliderValue) {
    simSetters.SetDrag(sliderValue);
  }

  protected override float GetSimulatorValue() {
    return simSetters.GetDrag();
  }

  protected override SliderRefreshMode GetRefreshMode() {
    return SliderRefreshMode.OnEcosystemLoad;
  }

}
