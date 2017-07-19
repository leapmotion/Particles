using Leap.Unity.GraphicalRenderer;
using Leap.Unity.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class SimulatorSliderSetTrailLength : SimulatorSliderControl {

  protected override void setSimulatorValue(float sliderValue) {
    simulatorSetters.SetTrailSize(sliderValue);
  }

}

