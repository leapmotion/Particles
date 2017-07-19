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

    slider.HorizontalSlideEvent += onSlideEvent;
  }

  private void onSlideEvent(float value) {
    value = filterSliderValue(value);

    setSimulatorValue(value);
  }

  void Update() {
    maybeRefreshSimValue();

    if (textOutput != null) {
      textOutput.text = slider.HorizontalSliderValue.ToString(outputFormat);
    }
  }

  private void maybeRefreshSimValue() {
    float sliderValue = slider.HorizontalSliderValue;
    float simValue;
    bool shouldRefresh = refreshWithSimulatorValue(out simValue);
    if (shouldRefresh && sliderValue != simValue) {
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
