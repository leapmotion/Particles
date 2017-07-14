using Leap.Unity.GraphicalRenderer;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetTextGraphicWithSimulationAge : SetTextGraphicWithSimulatorParam {

  public override string GetTextValue() {
    if (simulator == null) return "0";

    return simulator.simulationAge.ToString();
  }
}
