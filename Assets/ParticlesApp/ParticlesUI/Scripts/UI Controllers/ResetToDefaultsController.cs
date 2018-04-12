using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResetToDefaultsController : MonoBehaviour {

  public GameObject findSliderControlsWithin;

  private List<SimulatorSliderControl> _simulatorSliderControls = new List<SimulatorSliderControl>();

  void Start() {
    findSliderControlsWithin.GetComponentsInChildren<SimulatorSliderControl>(true, _simulatorSliderControls);
  }

  public void ResetToDefaults() {
    foreach (var control in _simulatorSliderControls) {
      control.value = control.slider.defaultHorizontalValue;
    }
  }

}
