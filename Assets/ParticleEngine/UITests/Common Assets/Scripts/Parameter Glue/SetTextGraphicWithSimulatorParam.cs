using Leap.Unity.GraphicalRenderer;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SetTextGraphicWithSimulatorParam : MonoBehaviour {

  public TextureSimulator simulator;
  public TextureSimulatorSetters simulatorSetters;
  public LeapTextGraphic textGraphic;
  public string prefix;
  public string postfix;

  public abstract string GetTextValue();

  protected virtual void Reset() {
    textGraphic = GetComponent<LeapTextGraphic>();
    simulator = FindObjectOfType<TextureSimulator>();
    simulatorSetters = FindObjectOfType<TextureSimulatorSetters>();
  }

  protected void OnValidate() {
    if (simulator == null) simulator = FindObjectOfType<TextureSimulator>();
  }

  void Update() {
    string value = GetTextValue();

    if (textGraphic != null) {
      textGraphic.text = prefix + value + postfix;
    }
  }

}
