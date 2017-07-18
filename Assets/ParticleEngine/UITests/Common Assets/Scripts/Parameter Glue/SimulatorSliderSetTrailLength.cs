using Leap.Unity.GraphicalRenderer;
using Leap.Unity.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulatorSliderSetTrailLength : SimulatorUIControl {

  public InteractionSlider slider;
  public LeapTextGraphic textOutput;

  protected override void Reset() {
    base.Reset();

    slider = GetComponent<InteractionSlider>();
  }

  void Update() {
    simulatorSetters.SetTrailSize(slider.HorizontalSliderValue);

    if (textOutput != null) {
      textOutput.text = slider.HorizontalSliderValue.ToString();
    }
  }

}
