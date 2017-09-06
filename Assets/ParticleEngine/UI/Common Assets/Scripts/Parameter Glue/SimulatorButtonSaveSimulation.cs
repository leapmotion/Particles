using Leap.Unity.GraphicalRenderer;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulatorButtonSaveSimulation : SimulatorButtonControl {

  public LeapTextGraphic buttonLabelGraphic;

  private string baseText;

  void Start() {
    baseText = buttonLabelGraphic.text;
  }

  public override void onPress() {
    if (simManager.SaveEcosystem()) {
      buttonLabelGraphic.text = baseText + " (Success)";
    }
    else {
      buttonLabelGraphic.text = baseText + " (Failed)";
    }
  }

}
