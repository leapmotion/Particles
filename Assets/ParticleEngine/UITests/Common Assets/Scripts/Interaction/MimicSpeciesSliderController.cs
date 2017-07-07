using Leap.Unity.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class MimicSpeciesSliderController : MonoBehaviour {

  [System.Serializable]
  public class StringEvent : UnityEvent<string> { }

  public InteractionSlider slider;
  public TextureSimulator particleSimulator;

  public StringEvent OnSpeciesColor;

  void Reset() {
    slider = GetComponent<InteractionSlider>();
  }

  public void RefreshSelectedSpecies(float unusedSliderValue = 0F) {

    int speciesIdx = slider.horizontalStepValue;

    if (particleSimulator != null) {
      Color speciesColor = particleSimulator.GetSpeciesColor(speciesIdx);
      string speciesColorString = ColorUtility.ToHtmlStringRGB(speciesColor);
      OnSpeciesColor.Invoke("#" + speciesColorString);
      particleSimulator.socialHandSpecies = speciesIdx;
    }
  }

}
