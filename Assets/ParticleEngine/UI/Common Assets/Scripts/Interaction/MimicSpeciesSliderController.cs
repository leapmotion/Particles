using Leap.Unity.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class MimicSpeciesSliderController : MonoBehaviour {

  [System.Serializable]
  public class StringEvent : UnityEvent<string> { }

  public InteractionSlider slider;
  public SimulationManager simManager;

  public StringEvent OnSpeciesColor;

  void Reset() {
    slider = GetComponent<InteractionSlider>();
  }

  public void RefreshSelectedSpecies(float unusedSliderValue = 0F) {

    int speciesIdx = slider.horizontalStepValue;

    if (simManager != null) {
      Color speciesColor = simManager.GetSpeciesColor(speciesIdx);
      string speciesColorString = ColorUtility.ToHtmlStringRGB(speciesColor);
      OnSpeciesColor.Invoke("#" + speciesColorString);
      //simManager.socialHandSpecies = speciesIdx;
      //TODO
      //(i don't think we need this anymore)
    }
  }

}
