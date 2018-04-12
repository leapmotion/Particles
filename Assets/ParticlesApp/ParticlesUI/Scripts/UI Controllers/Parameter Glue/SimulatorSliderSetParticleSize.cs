using Leap.Unity.GraphicalRenderer;
using Leap.Unity.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class SimulatorSliderSetParticleSize : SimulatorSliderControl {

  protected override SliderRefreshMode GetRefreshMode() {
    return SliderRefreshMode.OnEcosystemLoad;
  }

  protected override float GetSimulatorValue() {
    return simSetters.GetParticleSize();
  }

  protected override void SetSimulatorValue(float sliderValue) {
    simSetters.SetParticleSize(sliderValue);
  }

}
