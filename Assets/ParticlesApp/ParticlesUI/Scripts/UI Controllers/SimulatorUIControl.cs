using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulatorUIControl : MonoBehaviour {

  public SimulationManager simManager;
  public GeneratorManager genManager;

  public SimulatorSetters simSetters;

  protected virtual void OnValidate() {
    if (simManager == null) simManager = FindObjectOfType<SimulationManager>();
    if (genManager == null) genManager = FindObjectOfType<GeneratorManager>();
    if (simSetters == null) simSetters = FindObjectOfType<SimulatorSetters>();
  }

  protected virtual void Reset() {
    if (simManager == null) simManager = FindObjectOfType<SimulationManager>();
    if (genManager == null) genManager = FindObjectOfType<GeneratorManager>();
    if (simSetters == null) simSetters = FindObjectOfType<SimulatorSetters>();
  }

}
