using Leap.Unity.GraphicalRenderer;
using Leap.Unity.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SimulatorSliderControl : SimulatorUIControl {

  public InteractionSlider slider;

  [Header("Optional")]
  public LeapTextGraphic textOutput;
  public string outputFormat = "F2";

  protected override void Reset() {
    base.Reset();

    slider = GetComponent<InteractionSlider>();
    outputFormat = "F2";
  }

  void Start() {
    maybeRefreshSimValue();
  }

  void Update() {
    float value = slider.HorizontalSliderValue;
    value = filterSliderValue(value);
    setSimulatorValue(value);

    maybeRefreshSimValue();

    if (textOutput != null) {
      textOutput.text = value.ToString(outputFormat);
    }
  }

  private void maybeRefreshSimValue() {
    float simValue;
    bool shouldRefresh = refreshWithSimulatorValue(out simValue);
    if (shouldRefresh) {
      slider.HorizontalSliderValue = simValue;
    }
  }

  protected virtual bool refreshWithSimulatorValue(out float value) {
    value = 0F;
    return false;
  }

  protected virtual float filterSliderValue(float sliderValue) {
    return sliderValue;
  }

  protected abstract void setSimulatorValue(float sliderValue);

}
