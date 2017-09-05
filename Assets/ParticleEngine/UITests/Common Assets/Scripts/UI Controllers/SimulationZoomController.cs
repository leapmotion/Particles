using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulationZoomController : MonoBehaviour {

  public float _targetZoomedInScale = 1f;
  public float _targetZoomedOutScale = 0.2F;

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
    float scale = displayAnchor.transform.localScale.x;

    scale = Mathf.Lerp(scale, _targetScale, 5F * Time.deltaTime);

    displayAnchor.transform.localScale = Vector3.one * scale;
  }

  public void ToggleZoom() {
    if (_isZoomedIn) {
      ZoomOut();
    } else {
      ZoomIn();
    }
    _isZoomedIn = !_isZoomedIn;
  }

  public void ZoomOut() {
    _targetScale = _targetZoomedOutScale;
  }

  public void ZoomIn() {
    _targetScale = _targetZoomedInScale;
  }

}
