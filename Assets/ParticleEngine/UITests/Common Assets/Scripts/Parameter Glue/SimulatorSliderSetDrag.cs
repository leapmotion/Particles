using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulatorSliderSetDrag : SimulatorSliderControl {

  protected override void setSimulatorValue(float sliderValue) {
    simulatorSetters.SetDrag(sliderValue);
  }

}
