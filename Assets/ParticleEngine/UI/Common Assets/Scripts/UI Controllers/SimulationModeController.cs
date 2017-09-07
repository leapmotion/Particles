using Leap.Unity.Attributes;
using Leap.Unity.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulationModeController : MonoBehaviour {

  public const float NORMAL_TO_ATOMIC_CONVERSION_FACTOR = 0.5f;

  #region Inspector

  public enum SimulationMode { Normal, Atomic }

  [SerializeField, OnEditorChange("mode")]
  private SimulationMode _mode;
  public SimulationMode mode {
    get { return _mode; }
    set {
      if (value != mode) {
        _mode = value;

        switch (_mode) {
          case SimulationMode.Normal:
            switchToNormalMode();
            break;
          case SimulationMode.Atomic:
            switchToAtomicMode();
            break;
        }
      }
    }
  }

  public InteractionSlider fieldForceSlider;
  public InteractionSlider fieldRadiusSlider;
  public InteractionSlider socialForceSlider;
  public InteractionSlider socialRadiusSlider;
  public InteractionSlider dragSlider;

  [Header("Zoom")]

  public SimulationZoomController zoomController;
  private float _atomicZoomDefault = 0.5f;
  private float _normalZoomDefault = 0f;

  [Header("Normal Slider Ranges - Automatic On Start")]

  public Vector2 normalFieldForceRange   = new Vector2(  0f,   0f);
  public Vector2 normalFieldRadiusRange  = new Vector2(  0f,   0f);
  public Vector2 normalSocialForceRange  = new Vector2(  0f,   0f);
  public Vector2 normalSocialRadiusRange = new Vector2(  0f,   0f);
  public Vector2 normalDragRange         = new Vector2(  0f,   0f);

  [Header("Normal to Atomic Conversion Factor -- Readonly")]
  [Disable]
  public float normalToAtomicConversion = NORMAL_TO_ATOMIC_CONVERSION_FACTOR;

  [Header("Atomic Slider Ranges - Automatic On Start")]

  public Vector2 atomicFieldForceRange   = new Vector2(  0f,   0f);
  public Vector2 atomicFieldRadiusRange  = new Vector2(  0f,   0f);
  public Vector2 atomicSocialForceRange  = new Vector2(  0f,   0f);
  public Vector2 atomicSocialRadiusRange = new Vector2(  0f,   0f);
  public Vector2 atomicDragRange         = new Vector2(  0f,   0f);

  #endregion

  #region Unity Events

  void Reset() {
    if (fieldForceSlider == null) {
      fieldForceSlider = NewUtils.FindObjectInHierarchy<SimulatorSliderSetBoundingForce>().GetComponent<InteractionSlider>();
    }
    if (fieldRadiusSlider == null) {
      fieldRadiusSlider = NewUtils.FindObjectInHierarchy<SimulatorSliderSetBoundingRadius>().GetComponent<InteractionSlider>();
    }
    if (socialForceSlider == null) {
      socialForceSlider = NewUtils.FindObjectInHierarchy<SimulatorSliderSetMaxForce>().GetComponent<InteractionSlider>();
    }
    if (socialRadiusSlider == null) {
      socialRadiusSlider = NewUtils.FindObjectInHierarchy<SimulatorSliderSetMaxRange>().GetComponent<InteractionSlider>();
    }
    if (dragSlider == null) {
      dragSlider = NewUtils.FindObjectInHierarchy<SimulatorSliderSetDrag>().GetComponent<InteractionSlider>();
    }

    if (zoomController == null) {
      zoomController = NewUtils.FindObjectInHierarchy<SimulationZoomController>();
    }
  }

  void Start() {
    normalFieldForceRange   = fieldForceSlider.horizontalValueRange;
    normalFieldRadiusRange  = fieldRadiusSlider.horizontalValueRange;
    normalSocialForceRange  = socialForceSlider.horizontalValueRange;
    normalSocialRadiusRange = socialRadiusSlider.horizontalValueRange;
    normalDragRange         = dragSlider.horizontalValueRange;

    float conversionFactor = NORMAL_TO_ATOMIC_CONVERSION_FACTOR;

    atomicFieldForceRange   = normalFieldForceRange   * conversionFactor;
    atomicFieldRadiusRange  = normalFieldRadiusRange  * conversionFactor;
    atomicSocialForceRange  = normalSocialForceRange  * conversionFactor;
    atomicSocialRadiusRange = normalSocialRadiusRange * conversionFactor;
    atomicDragRange         = normalDragRange * conversionFactor * conversionFactor;
  }

  #endregion

  #region Internal

  private void switchToNormalMode() {
    fieldForceSlider.horizontalValueRange   = normalFieldForceRange;
    fieldRadiusSlider.horizontalValueRange  = normalFieldRadiusRange;
    socialForceSlider.horizontalValueRange  = normalSocialForceRange;
    socialRadiusSlider.horizontalValueRange = normalSocialRadiusRange;
    dragSlider.horizontalValueRange         = normalDragRange;

    //zoomController.ZoomTo(_normalZoomDefault);
  }
  
  private void switchToAtomicMode() {
    fieldForceSlider.horizontalValueRange   = atomicFieldForceRange;
    fieldRadiusSlider.horizontalValueRange  = atomicFieldRadiusRange;
    socialForceSlider.horizontalValueRange  = atomicSocialForceRange;
    socialRadiusSlider.horizontalValueRange = atomicSocialRadiusRange;
    dragSlider.horizontalValueRange         = atomicDragRange;

    //zoomController.ZoomTo(_atomicZoomDefault);
  }

  #endregion

}
