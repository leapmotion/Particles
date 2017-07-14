using Leap.Unity.GraphicalRenderer;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SimulatorTextGraphicSetter : MonoBehaviour {

  public TextureSimulator particleSimulator;
  public LeapTextGraphic textGraphic;
  public string prefix;
  public string postfix;

  public abstract string GetTextValue();

  void Reset() {
    textGraphic = GetComponent<LeapTextGraphic>();
    particleSimulator = FindObjectOfType<TextureSimulator>();
  }

  void Update() {
    string value = GetTextValue();

    if (textGraphic != null) {
      textGraphic.text = prefix + value + postfix;
    }
  }

}
