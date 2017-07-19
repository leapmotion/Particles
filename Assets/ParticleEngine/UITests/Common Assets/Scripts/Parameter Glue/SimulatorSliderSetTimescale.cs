using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulatorSliderSetTimescale : SimulatorSliderControl {

  protected override bool refreshWithSimulatorValue(out float value) {
    value = simulatorSetters.GetTimescale();
    return true;
  }

  //protected override float filterSliderValue(float sliderValue) {
  //  return Mathf.Round(sliderValue * 10F) / 10F;
  //}

  protected override void setSimulatorValue(float sliderValue) {
    simulatorSetters.SetTimescale(sliderValue);
  }

}
