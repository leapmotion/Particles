using Leap.Unity.GraphicalRenderer;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetTextGraphicWithSimulationAge : SimulatorTextGraphicSetter {

  public override string GetTextValue() {
    if (particleSimulator == null) return "0";

    return particleSimulator.simulationAge.ToString();
  }
}
