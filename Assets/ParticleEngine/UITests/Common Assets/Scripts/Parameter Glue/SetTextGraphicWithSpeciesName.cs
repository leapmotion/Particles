using Leap.Unity.GraphicalRenderer;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class SetTextGraphicWithSpeciesName : SetTextGraphicWithSimulatorParam {

  public override string GetTextValue() {
    if (simulator == null) return "(Simulation not configured)";

    if (simulator.currentSimulationDescription.isRandomDescription) {
      return simulator.currentSimulationDescription.name;
    }
    else {
      return simulator.currentSimulationDescription.name + " (Preset)";
    }
  }
}
