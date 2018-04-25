using Leap.Unity.GraphicalRenderer;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class SetTextGraphicWithSpeciesName : SetTextGraphicWithSimulatorParam {

  public override string GetTextValue() {
    if (simManager == null) return "(Simulation not configured)";

    if (simManager.isPerformingTransition) {
      return "(Transitioning...)";
    }
    else {
      if (simManager.currentDescription.isRandomDescription) {
        return simManager.currentDescription.name;
      }
      else {
        return simManager.currentDescription.name + " (Preset)";
      }
    }
  }
}
