using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UISliderInt_SetNumGalaxies : UISliderInt {

  public override int GetMaxValue() {
    return GalaxyUIOperations.GetMaxNumGalaxies();
  }

  public override int GetMinValue() {
    return GalaxyUIOperations.GetMinNumGalaxies();
  }

  public override float GetStartingSliderValue() {
    return GalaxyUIOperations.GetNumGalaxies();
  }

  public override void OnSliderValue(int value) {
    GalaxyUIOperations.SetNumGalaxies(value);
  }

}
