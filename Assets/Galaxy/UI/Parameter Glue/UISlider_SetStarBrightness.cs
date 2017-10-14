using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UISlider_SetStarBrightness : UISlider {

  public override float GetStartingSliderValue() {
    return GalaxyUIOperations.GetStarBrightness();
  }

  public override void OnSliderValue(float value) {
    GalaxyUIOperations.SetStarBrightness(slider.normalizedHorizontalValue);
  }



}
