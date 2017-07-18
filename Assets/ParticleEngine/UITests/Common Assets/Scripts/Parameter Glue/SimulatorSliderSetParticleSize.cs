using Leap.Unity.GraphicalRenderer;
using Leap.Unity.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulatorSliderSetParticleSize : SimulatorUIControl {

  public InteractionSlider slider;
  public LeapTextGraphic textOutput;

  protected override void Reset() {
    base.Reset();

    slider = GetComponent<InteractionSlider>();
  }

  void Update() {
    simulatorSetters.SetParticleSize(slider.HorizontalSliderValue);

    if (textOutput != null) {
      textOutput.text = slider.HorizontalSliderValue.ToString();
    }
  }

}
