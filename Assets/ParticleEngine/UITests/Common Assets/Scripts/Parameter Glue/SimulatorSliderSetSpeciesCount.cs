using Leap.Unity.GraphicalRenderer;
using Leap.Unity.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class SimulatorSliderSetSpeciesCount : SimulatorSliderControl {

  protected override void Reset() {
    base.Reset();

    outputFormat = "F0";
  }

  protected override float filterSliderValue(float sliderValue) {
    return Mathf.Round(slider.HorizontalSliderValue);
  }

  protected override void setSimulatorValue(float sliderValue) {
    simulatorSetters.SetSpeciesCount(sliderValue);
  }

  protected override bool getShouldRefreshWithSimulatorValue(out float value) {
    value = simulatorSetters.GetSpeciesCount();
    return true;
  }

}
