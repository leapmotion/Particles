﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulatorSliderSetMaxRange : SimulatorSliderControl {

  protected override SliderRefreshMode GetRefreshMode() {
    return SliderRefreshMode.OnEcosystemLoad;
  }

  protected override float GetSimulatorValue() {
    return simSetters.GetMaxRange();
  }

  protected override void SetSimulatorValue(float sliderValue) {
    simSetters.SetMaxRange(sliderValue);
  }

}
