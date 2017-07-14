using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulatorUIControl : MonoBehaviour {

  public TextureSimulator simulator;
  public TextureSimulatorSetters simulatorSetters;

  protected virtual void OnValidate() {
    simulator = FindObjectOfType<TextureSimulator>();
    simulatorSetters = FindObjectOfType<TextureSimulatorSetters>();
  }

  protected virtual void Reset() {
    simulator = FindObjectOfType<TextureSimulator>();
    simulatorSetters = FindObjectOfType<TextureSimulatorSetters>();
  }

}
