using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Attributes;

public class SimulationZoomController : MonoBehaviour {

  [QuickButton("Zoom In", "ZoomIn")]
  public float _targetZoomedInScale = 5f;
  [QuickButton("Zoom Out", "ZoomOut")]
  public float _targetZoomedOutScale = 0.2F;

  [EditTimeOnly]
  [Range(0f, 1f)]
  [Tooltip("Target zoom-in amount set during the Start() callback of this component. " +
           "this amount is normalized between 0 and 1.")]
  public float _initialZoomInAmount = 0f;

  /// <summary>
  /// 0 to 1, lerps between _targetZoomedInScale and _targetZoomedOutScale.
  /// </summary>
  private float _targetZoomInAmount = 0f;

  private float _targetScale = 1.0F;
  private bool _isZoomedIn = true;

  public bool isFullyZoomedIn {
    get {
      return Mathf.Abs(displayAnchor.transform.localScale.x - _targetZoomedInScale) < 0.005f;
    }
  }
  public bool isFullyZoomedOut {
    get {
      return Mathf.Abs(displayAnchor.transform.localScale.x - _targetZoomedOutScale) < 0.005f;
    }
  }

  public Transform displayAnchor;

  private void Start() {
    _targetZoomInAmount = _initialZoomInAmount;
  }

  private void Update() {
    updateTargetScale();

    float scale = displayAnchor.transform.localScale.x;

    scale = Mathf.Lerp(scale, _targetScale, 5F * Time.deltaTime);
    
    displayAnchor.transform.localScale = Vector3.one * scale;

    if (isFullyZoomedIn && !_isZoomedIn) { _isZoomedIn = true; }
    if (isFullyZoomedOut && _isZoomedIn) { _isZoomedIn = false; }
  }

  public void ToggleZoom() {
    if (_isZoomedIn) {
      ZoomOut();
    } else {
      ZoomIn();
    }
    _isZoomedIn = !_isZoomedIn;
  }

  private void updateTargetScale()  {
    _targetScale = Mathf.Lerp(_targetZoomedOutScale, _targetZoomedInScale, _targetZoomInAmount);
  }

  [ContextMenu("Zoom Out")]
  public void ZoomOut() {
    _targetZoomInAmount = 0f;
  }

  public void ZoomOut(float zoomAmount) {
    _targetZoomInAmount = Mathf.Clamp01(_targetZoomInAmount - zoomAmount);
  }

  [ContextMenu("Zoom In")]
  public void ZoomIn() {
    _targetZoomInAmount = 1f;
  }

  public void ZoomIn(float zoomAmount) {
    _targetZoomInAmount = Mathf.Clamp01(_targetZoomInAmount + zoomAmount);
  }

  public void ZoomTo(float zoomAmount) {
    _targetZoomInAmount = Mathf.Clamp01(zoomAmount);
  }

}
