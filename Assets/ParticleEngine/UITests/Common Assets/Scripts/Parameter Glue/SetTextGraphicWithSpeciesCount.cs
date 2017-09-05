using Leap.Unity.GraphicalRenderer;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class SetTextGraphicWithSpeciesCount : SetTextGraphicWithSimulatorParam {

  public override string GetTextValue() {
    if (genManager == null) return "(Generation Manager not configured)";

    return genManager.speciesCount.ToString();
  }
}
