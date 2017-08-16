using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulatorSliderSetParticleCount : SimulatorSliderControl {

  protected override float filterSliderValue(float sliderValue) {
    return Mathf.RoundToInt(sliderValue);
  }

  protected override void setSimulatorValue(float sliderValue) {
    simulatorSetters.SetParticleCount((int)sliderValue);
  }

}
