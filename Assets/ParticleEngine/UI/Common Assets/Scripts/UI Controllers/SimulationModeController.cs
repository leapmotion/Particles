using Leap.Unity.Attributes;
using Leap.Unity.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulationModeController : MonoBehaviour {

  public const float NORMAL_TO_ATOMIC_CONVERSION_FACTOR = 0.10f;

  #region Inspector

  public SimulationManager simManager;
    
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
            simManager.simulationMethod = SimulationMethod.Texture;
            break;
          case SimulationMode.Atomic:
            switchToAtomicMode();
            simManager.simulationMethod = SimulationMethod.InteractionEngine;
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
  private float _atomicZoomDefault = 0.9f;
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
    if (simManager == null) {
      simManager = FindObjectOfType<SimulationManager>();
    }

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
    normalFieldForceRange   = new Vector2(fieldForceSlider.minHorizontalValue, fieldForceSlider.maxHorizontalValue);
    normalFieldRadiusRange  = new Vector2(fieldRadiusSlider.minHorizontalValue, fieldForceSlider.maxHorizontalValue);
    normalSocialForceRange  = new Vector2(socialForceSlider.minHorizontalValue, socialForceSlider.maxHorizontalValue);
    normalSocialRadiusRange = new Vector2(socialRadiusSlider.minHorizontalValue, socialRadiusSlider.maxHorizontalValue);
    normalDragRange         = new Vector2(dragSlider.minHorizontalValue, dragSlider.maxHorizontalValue);

    float conversionFactor = NORMAL_TO_ATOMIC_CONVERSION_FACTOR;

    atomicFieldForceRange   = normalFieldForceRange   * conversionFactor;
    atomicFieldRadiusRange  = normalFieldRadiusRange  * conversionFactor;
    atomicSocialForceRange  = normalSocialForceRange  * conversionFactor;
    atomicSocialRadiusRange = normalSocialRadiusRange * conversionFactor;
    atomicDragRange         = normalDragRange * Mathf.Sqrt(conversionFactor);
  }

  #endregion

  #region Internal

  private void switchToNormalMode() {
    fieldForceSlider.minHorizontalValue     = normalFieldForceRange.x;
    fieldForceSlider.maxHorizontalValue     = normalFieldForceRange.y;

    fieldRadiusSlider.minHorizontalValue    = normalFieldRadiusRange.x;
    fieldRadiusSlider.maxHorizontalValue    = normalFieldRadiusRange.y;

    socialForceSlider.minHorizontalValue    = normalSocialForceRange.x;
    socialForceSlider.maxHorizontalValue    = normalSocialForceRange.y;

    socialRadiusSlider.minHorizontalValue   = normalSocialRadiusRange.x;
    socialRadiusSlider.maxHorizontalValue   = normalSocialRadiusRange.y;

    dragSlider.minHorizontalValue           = normalDragRange.x;
    dragSlider.maxHorizontalValue           = normalDragRange.y;

    zoomController.ZoomTo(_normalZoomDefault);
  }
  
  private void switchToAtomicMode() {
    fieldForceSlider.minHorizontalValue     = atomicFieldForceRange.x;
    fieldForceSlider.maxHorizontalValue     = atomicFieldForceRange.y;

    fieldRadiusSlider.minHorizontalValue    = atomicFieldRadiusRange.x;
    fieldRadiusSlider.maxHorizontalValue    = atomicFieldRadiusRange.y;

    socialForceSlider.minHorizontalValue    = atomicSocialForceRange.x;
    socialForceSlider.maxHorizontalValue    = atomicSocialForceRange.y;

    socialRadiusSlider.minHorizontalValue   = atomicSocialRadiusRange.x;
    socialRadiusSlider.maxHorizontalValue   = atomicSocialRadiusRange.y;

    dragSlider.minHorizontalValue           = atomicDragRange.x;
    dragSlider.maxHorizontalValue           = atomicDragRange.y;

    zoomController.ZoomTo(_atomicZoomDefault);
  }

  #endregion

}
