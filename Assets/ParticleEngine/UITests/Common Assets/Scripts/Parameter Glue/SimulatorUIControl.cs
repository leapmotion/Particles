using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulatorUIControl : MonoBehaviour {

  public SimulationManager simManager;
  public GeneratorManager genManager;

  public SimulatorSetters simulatorSetters;

  protected virtual void OnValidate() {
    simManager = FindObjectOfType<SimulationManager>();
    genManager = FindObjectOfType<GeneratorManager>();
    simulatorSetters = FindObjectOfType<SimulatorSetters>();
  }

  protected virtual void Reset() {
    simManager = FindObjectOfType<SimulationManager>();
    genManager = FindObjectOfType<GeneratorManager>();
    simulatorSetters = FindObjectOfType<SimulatorSetters>();
  }

}
