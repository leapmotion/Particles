using Leap.Unity.GraphicalRenderer;
using Leap.Unity.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class SimulatorSliderSetTrailLength : SimulatorSliderControl {

  protected override SliderRefreshMode GetRefreshMode() {
    return SliderRefreshMode.OnEcosystemLoad;
  }

  protected override float GetSimulatorValue() {
    return simulatorSetters.GetTrailSize();
  }

  protected override void SetSimulatorValue(float sliderValue) {
    simulatorSetters.SetTrailSize(sliderValue);
  }

}

