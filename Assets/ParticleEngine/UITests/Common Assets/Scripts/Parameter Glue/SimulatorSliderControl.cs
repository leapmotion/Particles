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

  public float value {
    get {
      return slider.HorizontalSliderValue;
    }
    set {
      float newValue = filterSliderValue(value);
      slider.HorizontalSliderValue = newValue;
      onSlideEvent(newValue);
    }
  }

  protected override void Reset() {
    base.Reset();

    slider = GetComponent<InteractionSlider>();
    outputFormat = "F2";
  }

  void Awake() {
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
    bool shouldRefresh = getShouldRefreshWithSimulatorValue(out simValue);
    if (shouldRefresh && sliderValue != simValue) {
      slider.HorizontalSliderValue = simValue;
    }
  }

  /// <summary>
  /// Implement this method to return whether the slider should refresh its own value
  /// with the simulation's value. The method must also provide what the simulation's
  /// value is.
  /// </summary>
  protected virtual bool getShouldRefreshWithSimulatorValue(out float value) {
    value = 0F;
    return false;
  }

  protected virtual float filterSliderValue(float sliderValue) {
    return sliderValue;
  }

  protected abstract void setSimulatorValue(float sliderValue);

}
