using Leap.Unity.GraphicalRenderer;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class SetTextGraphicWithSpeciesName : TextGraphicSetter {

  public TextureSimulator particleSimulator;

  public override string GetTextValue() {
    if (particleSimulator == null) return "(Simulation not configured)";

    return particleSimulator.currentSpecies;
  }
}
