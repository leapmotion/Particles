using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UISlider_SetStarGamma : UISlider {

  public override float GetStartingSliderValue() {
    return GalaxyUIOperations.GetStarGamma();
  }

  public override void OnSliderValue(float value) {
    GalaxyUIOperations.SetStarGamma(slider.normalizedHorizontalValue);
  }

}
