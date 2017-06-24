using Leap.Unity;
using Leap.Unity.GraphicalRenderer;
using Leap.Unity.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetSpeciesCountWithSliderValue : MonoBehaviour {

  public TextureSimulator particleSimulator;
  public InteractionSlider slider;

  [Header("Optional")]
  public LeapTextGraphic currentValueTextGraphic;

  void Update() {

    float sliderValue = slider.HorizontalSliderValue.Map(slider.horizontalValueRange.x,
                                                         slider.horizontalValueRange.y,
                                                         0F, 1F);

    int discretizedSliderValue = (int)(sliderValue * (slider.horizontalSteps + 1) * 1.0001F);

    int minSpeciesCount = 2;
    int finalValue = discretizedSliderValue + minSpeciesCount;

    if (currentValueTextGraphic != null) {
      currentValueTextGraphic.text = finalValue.ToString();
    }

    particleSimulator.maxSpecies = finalValue;
  }

}
