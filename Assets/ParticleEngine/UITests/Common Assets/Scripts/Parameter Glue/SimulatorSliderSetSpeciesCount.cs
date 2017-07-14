using Leap.Unity.GraphicalRenderer;
using Leap.Unity.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulatorSliderSetSpeciesCount : SimulatorUIControl {

  public InteractionSlider slider;
  public LeapTextGraphic textOutput;

  protected override void Reset() {
    base.Reset();

    slider = GetComponent<InteractionSlider>();
  }

  void Update() {
    int speciesCount = Mathf.RoundToInt(slider.HorizontalSliderValue);

    simulatorSetters.SetSpeciesCount(speciesCount);

    if (textOutput != null) {
      textOutput.text = speciesCount.ToString();
    }
  }

}
