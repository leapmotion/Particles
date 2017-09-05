using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulatorSliderSetParticleCount : SimulatorSliderControl {

  protected override void Update() {
    base.Update();

    // Species count slider isn't valid while a Preset ecosystem is loaded.
    slider.controlEnabled = simManager.currentDescription.isRandomDescription;
  }

  protected override float filterSliderValue(float sliderValue) {
    return Mathf.RoundToInt(sliderValue);
  }

  protected override SliderRefreshMode GetRefreshMode() {
    return SliderRefreshMode.OnEcosystemLoad;
  }

  protected override float GetSimulatorValue() {
    return simulatorSetters.GetParticleCount();
  }

  protected override void SetSimulatorValue(float sliderValue) {
    simulatorSetters.SetParticleCount((int)sliderValue);
  }

}
