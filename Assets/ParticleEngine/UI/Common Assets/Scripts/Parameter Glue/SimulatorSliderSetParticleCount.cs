using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulatorSliderSetParticleCount : SimulatorSliderControl {

  protected override void Update() {
    base.Update();

    // Set the slider maximum value based on the recommended setting (IE can't handle
    // 4096 particles!)
    slider.horizontalValueRange = new Vector2(slider.horizontalValueRange.x,
                                              simManager.GetRecommendedMaxParticles());

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
