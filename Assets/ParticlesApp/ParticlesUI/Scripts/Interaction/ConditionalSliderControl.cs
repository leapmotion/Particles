using Leap.Unity.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConditionalSliderControl : MonoBehaviour {

  public InteractionSliderToggle toggleSlider;
  public InteractionSlider dependentSlider;

  void Reset() {
    dependentSlider = this.GetComponent<InteractionSlider>();
  }

  void Start() {
    toggleSlider.OnToggleEnabled += enableDependentSliders;
    toggleSlider.OnToggleDisabled += disableDependentSliders;
  }

  private void enableDependentSliders() {
    dependentSlider.controlEnabled = true;
  }

  private void disableDependentSliders() {
    dependentSlider.controlEnabled = false;
  }

}
