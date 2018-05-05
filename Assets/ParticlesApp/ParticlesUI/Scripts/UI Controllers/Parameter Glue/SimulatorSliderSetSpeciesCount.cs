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

  protected override void Update() {
    base.Update();

    // Species count slider isn't valid while a Preset ecosystem is loaded.
    if (simManager.currentDescription.isRandomDescription) {
      slider.controlEnabled = false;
      slider.ignoreGrasping = true;
    }
    else {
      slider.controlEnabled = true;
      slider.ignoreGrasping = false;
    }
  }

  protected override float filterSliderValue(float sliderValue) {
    return Mathf.Round(slider.HorizontalSliderValue);
  }

  protected override void SetSimulatorValue(float sliderValue) {
    simSetters.SetSpeciesCount(sliderValue);
  }

  protected override float GetSimulatorValue() {
    return simSetters.GetSpeciesCount();
  }

  protected override SliderRefreshMode GetRefreshMode() {
    return SliderRefreshMode.OnEcosystemLoad;
  }
}
