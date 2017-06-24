using Leap.Unity.GraphicalRenderer;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetTextGraphicWithSimulationAge : MonoBehaviour {

  public TextureSimulator particleSimulator;
  public LeapTextGraphic textGraphic;

  void OnValidate() {
    if (textGraphic == null) {
      textGraphic = GetComponent<LeapTextGraphic>();
    }
  }

  void Update() {
    if (particleSimulator != null && textGraphic != null) {
      textGraphic.text = particleSimulator.simulationAge + " ticks";
    }
  }

}
