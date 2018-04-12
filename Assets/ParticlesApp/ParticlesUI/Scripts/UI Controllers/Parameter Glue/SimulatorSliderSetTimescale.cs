using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulatorSliderSetTimescale : SimulatorSliderControl {

  protected override SliderRefreshMode GetRefreshMode() {
    return SliderRefreshMode.EveryUpdate;
  }

  protected override float GetSimulatorValue() {
    return simSetters.GetTimescale();
  }

  protected override void SetSimulatorValue(float sliderValue) {
    simSetters.SetTimescale(sliderValue);
  }

}
