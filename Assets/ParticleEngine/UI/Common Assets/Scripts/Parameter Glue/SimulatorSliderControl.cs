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
  
  public enum SliderRefreshMode {
    EveryUpdate,
    OnEcosystemLoad
  }
  private SliderRefreshMode _refreshMode = SliderRefreshMode.EveryUpdate;

  private bool _firstUpdate = true;

  protected override void Reset() {
    base.Reset();

    slider = GetComponent<InteractionSlider>();
    outputFormat = "F2";
  }

  protected virtual void Awake() {
    simManager.OnEcosystemEndedTransition += onEcosystemEndedTransition;

    slider.HorizontalSlideEvent += onSlideEvent;
    slider.OnUnpress += onUnpress;

    _refreshMode = GetRefreshMode();
  }

  private void onEcosystemEndedTransition() {
    if (_refreshMode == SliderRefreshMode.OnEcosystemLoad) {
      refreshSimValue();
      //if (this.transform.parent.parent.parent.name.Equals("Max Force Control")) {
      //  Debug.Log("Max Force slider refreshing Sim value in OnEcosystemLoad: " + slider.HorizontalSliderValue);
      //}
    }
  }

  private void onSlideEvent(float value) {
    value = filterSliderValue(value);

    SetSimulatorValue(value);
  }

  private void onUnpress() {
    simManager.ApplySliderValues();
  }

  protected virtual void Update() {
    if (_firstUpdate) {
      refreshSimValue();
      //if (this.transform.parent.parent.parent.name.Equals("Max Force Control")) {
      //  Debug.Log("Max Force slider refreshing Sim value on FIRST update: " + slider.HorizontalSliderValue);
      //}

      _firstUpdate = false;
    }

    if (_refreshMode == SliderRefreshMode.EveryUpdate) {
      refreshSimValue();
      //if (this.transform.parent.parent.parent.name.Equals("Max Force Control")) {
      //  Debug.Log("Max Force slider refreshing Sim value in Update: " + slider.HorizontalSliderValue);
      //}
    }

    if (textOutput != null) {
      textOutput.text = slider.HorizontalSliderValue.ToString(outputFormat);
    }
  }

  private void refreshSimValue() {
    float sliderValue = slider.HorizontalSliderValue;
    float simValue = GetSimulatorValue();
    if (sliderValue != simValue) {
      slider.HorizontalSliderValue = simValue;
    }
  }

  protected virtual float filterSliderValue(float sliderValue) {
    return sliderValue;
  }

  protected abstract void SetSimulatorValue(float sliderValue);

  protected abstract float GetSimulatorValue();

  protected abstract SliderRefreshMode GetRefreshMode();

}
