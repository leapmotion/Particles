using Leap.Unity.GraphicalRenderer;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetTextGraphicWithSimulationAge : SetTextGraphicWithSimulatorParam {

  public override string GetTextValue() {
    if (simManager == null) return "0";

    return simManager.simulationAge.ToString();
  }
}
