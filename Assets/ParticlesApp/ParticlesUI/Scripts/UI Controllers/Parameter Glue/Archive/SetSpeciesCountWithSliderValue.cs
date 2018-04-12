using Leap.Unity;
using Leap.Unity.GraphicalRenderer;
using Leap.Unity.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetSpeciesCountWithSliderValue : MonoBehaviour {

  public GeneratorManager genManager;
  public InteractionSlider slider;

  [Header("Optional")]
  public LeapTextGraphic currentValueTextGraphic;
  public string prefix;
  public string postfix;

  void Update() {
    float sliderValue = slider.HorizontalSliderValue.Map(slider.minHorizontalValue,
                                                         slider.maxHorizontalValue,
                                                         0F, 1F);

    int discretizedSliderValue = (int)(sliderValue * (slider.horizontalSteps + 1) * 1.0001F);

    int minSpeciesCount = 2;
    int finalValue = discretizedSliderValue + minSpeciesCount;

    if (currentValueTextGraphic != null) {
      currentValueTextGraphic.text = prefix + finalValue.ToString() + postfix;
    }

    genManager.speciesCount = finalValue;
  }

}
