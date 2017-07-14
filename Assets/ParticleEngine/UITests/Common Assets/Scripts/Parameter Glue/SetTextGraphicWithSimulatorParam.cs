using Leap.Unity.GraphicalRenderer;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SetTextGraphicWithSimulatorParam : MonoBehaviour {

  public TextureSimulator simulator;
  public LeapTextGraphic textGraphic;
  public string prefix;
  public string postfix;

  public abstract string GetTextValue();

  void Reset() {
    textGraphic = GetComponent<LeapTextGraphic>();
    simulator = FindObjectOfType<TextureSimulator>();
  }

  void OnValidate() {
    if (simulator == null) simulator = FindObjectOfType<TextureSimulator>();
  }

  void Update() {
    string value = GetTextValue();

    if (textGraphic != null) {
      textGraphic.text = prefix + value + postfix;
    }
  }

}
