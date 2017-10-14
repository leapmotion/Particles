using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.GalaxySim {

  public class UISlider_SetMaxSimulationSpeed : UISlider {

    public override void OnSliderValue(float value) {
      GalaxyUIOperations.SetMaxSimulationSpeed(slider.normalizedHorizontalValue);
    }

    public override float GetStartingSliderValue() {
      return GalaxyUIOperations.GetMaxSimulationSpeed();
    }

  }

}
