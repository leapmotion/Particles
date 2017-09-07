using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulationZoomController : MonoBehaviour {

  public float _targetZoomedInScale = 1f;
  public float _targetZoomedOutScale = 0.2F;

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

  void Update() {
    updateTargetZoom();

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

  private void updateTargetZoom()
  {
    _targetScale = Mathf.Lerp(_targetZoomedOutScale, _targetZoomedInScale, _targetZoomInAmount);
  }

  public void ZoomOut()
  {
    _targetZoomInAmount = 0f;
  }

  public void ZoomOut(float zoomAmount)
  {
    _targetZoomInAmount = Mathf.Clamp01(_targetZoomInAmount - zoomAmount);
  }

  public void ZoomIn()
  {
    _targetZoomInAmount = 1f;
  }

  public void ZoomIn(float zoomAmount)
  {
    _targetZoomInAmount = Mathf.Clamp01(_targetZoomInAmount + zoomAmount);
  }

  public void ZoomTo(float zoomAmount) {
    _targetZoomInAmount = Mathf.Clamp01(zoomAmount);
  }

}
