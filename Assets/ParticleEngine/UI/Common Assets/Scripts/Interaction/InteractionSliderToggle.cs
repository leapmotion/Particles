using Leap.Unity.Attributes;
using Leap.Unity.GraphicalRenderer;
using Leap.Unity.Interaction;
using Leap.Unity.Interaction.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class InteractionSliderToggle : MonoBehaviour {

  public InteractionSlider slider;

  [EditTimeOnly]
  public bool enabledAtStart = false;

  [SerializeField]
  private UnityEvent _onToggleEnabled;
  public Action OnToggleEnabled = () => { };
  [SerializeField]
  private UnityEvent _onToggleDisabled;
  public Action OnToggleDisabled = () => { };

  private bool _isEnabled = false;

  void Reset() {
    slider = GetComponent<InteractionSlider>();
  }

  void Start() {
    if (slider.sliderType == InteractionSlider.SliderType.Horizontal) {
      slider.HorizontalSlideEvent += refreshToggleState;
    }
    else if (slider.sliderType == InteractionSlider.SliderType.Vertical) {
      slider.VerticalSlideEvent += refreshToggleState;
    }

    OnToggleEnabled  += _onToggleEnabled.Invoke;
    OnToggleDisabled += _onToggleDisabled.Invoke;

    _isEnabled = enabledAtStart;
    refreshToggleState();
  }

  private void refreshToggleState(float unusedSliderValue = 0F) {
    if (isSliderSetToEnabled() != _isEnabled) {
      if (_isEnabled == false) {
        // Slider is now enabled.
        _isEnabled = true;

        OnToggleEnabled();
      }
      else {
        // Slider is now disabled.
        _isEnabled = false;

        OnToggleDisabled();
      }
    }
  }

  private bool isSliderSetToEnabled() {
    if (slider.sliderType == InteractionSlider.SliderType.Horizontal) {
      return slider.normalizedHorizontalValue > 0.5F;
    }
    else {
      return slider.normalizedVerticalValue > 0.5F;
    }
  }

}
