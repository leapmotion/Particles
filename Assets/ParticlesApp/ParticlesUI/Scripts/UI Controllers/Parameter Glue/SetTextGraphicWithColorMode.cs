using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetTextGraphicWithColorMode : SetTextGraphicWithSimulatorParam {

  public string mode1Text = "(1: Normal)";
  public string mode2Text = "(2: Light by Velocity)";
  public string mode3Text = "(3: Velocity to RGB)";
  public string mode4Text = "(4: Darken by Velocity)";

  public override string GetTextValue() {
    var colorMode = simulatorSetters.GetColorMode();
    // Note: These enum names are totally confused!
    // SpeciesWithMagnitude and ByInverseVelocity are the inverse of one another,
    // ByVelocity is Velocity -> RGB!
    switch (colorMode) {
      case ColorMode.BySpecies:
        return mode1Text;
      case ColorMode.BySpeciesWithMagnitude:
        return mode2Text;
      case ColorMode.ByVelocity:
        return mode3Text;
      case ColorMode.ByInverseVelocity:
        return mode4Text;
      default:
        return mode1Text;
    }
  }

}
