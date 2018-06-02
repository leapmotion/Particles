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
      slider.HorizontalSliderValue = value;
      slider.HorizontalSliderValue = filterSliderValue(slider.HorizontalSliderValue);
      onSlideEvent(slider.HorizontalSliderValue);
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
    slider.OnContactEnd += onContactEnd;

    _refreshMode = GetRefreshMode();
  }

  private void onEcosystemEndedTransition() {
    if (_refreshMode == SliderRefreshMode.OnEcosystemLoad) {
      refreshSimValue();
    }
  }

  private void onSlideEvent(float value) {
    value = filterSliderValue(value);

    SetSimulatorValue(value);
  }

  private void onUnpress() {
    simManager.ApplySliderValues();
  }

  private float _timeSinceLastContactEnd = 100f;
  private float _contactEndWait = 0.5f;
  /// <summary>
  /// Sliders send contact end pretty often because the collision checks are imperfect--
  /// arguably this is a bug in the Interaction Engine, as a workaround for now we wait
  /// for contact end callbacks to stop happening for half a second before assuming 
  /// contact has "truly" ended, and then we update the simulation with the slider's
  /// value.
  /// </summary>
  private void onContactEnd() {
    _timeSinceLastContactEnd = 0f;
  }

  protected virtual void Update() {
    if (_firstUpdate) {
      refreshSimValue();

      _firstUpdate = false;
    }

    if (_timeSinceLastContactEnd <= _contactEndWait) {
      _timeSinceLastContactEnd += Time.deltaTime;

      if (_timeSinceLastContactEnd > _contactEndWait) {
        simManager.ApplySliderValues();
      }
    }

    if (_refreshMode == SliderRefreshMode.EveryUpdate) {
      refreshSimValue();
    }

    if (textOutput != null) {
      textOutput.text = slider.HorizontalSliderValue.ToString(outputFormat);
    }
  }

  public static void RefreshAllSliders() {
    foreach (var slider in FindObjectsOfType<SimulatorSliderControl>()) {
      slider.refreshSimValue();
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
