using Leap.Unity.GraphicalRenderer;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SetTextGraphicWithSimulatorParam : MonoBehaviour {

  public SimulationManager simManager;
  public GeneratorManager genManager;
  public SimulatorSetters simulatorSetters;
  public LeapTextGraphic textGraphic;
  public string prefix;
  public string postfix;

  public abstract string GetTextValue();

  protected virtual void Reset() {
    textGraphic = GetComponent<LeapTextGraphic>();
    simManager = FindObjectOfType<SimulationManager>();
    genManager = FindObjectOfType<GeneratorManager>();
    simulatorSetters = FindObjectOfType<SimulatorSetters>();
  }

  protected void OnValidate() {
    if (simManager == null) simManager = FindObjectOfType<SimulationManager>();
    if (genManager == null) genManager = FindObjectOfType<GeneratorManager>();
    if (simulatorSetters == null) simulatorSetters = FindObjectOfType<SimulatorSetters>();
  }

  void Update() {
    string value = GetTextValue();

    if (textGraphic != null) {
      textGraphic.text = prefix + value + postfix;
    }
  }

}
