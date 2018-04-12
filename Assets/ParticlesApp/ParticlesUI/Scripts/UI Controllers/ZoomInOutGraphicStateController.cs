using Leap.Unity;
using Leap.Unity.Attributes;
using Leap.Unity.GraphicalRenderer;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZoomInOutGraphicStateController : MonoBehaviour {

  public Transform zoomInParent;

  [SerializeField, Disable]
  private LeapGraphic[] _zoomInGraphics;

  [Space(20)]
  public Transform zoomOutParent;

  [SerializeField, Disable]
  private LeapGraphic[] _zoomOutGraphics;

  void OnValidate() {
    if (zoomInParent != null) {
      _zoomInGraphics = zoomInParent.GetComponentsInChildren<LeapGraphic>();
    }

    if (zoomOutParent != null) {
      _zoomOutGraphics = zoomOutParent.GetComponentsInChildren<LeapGraphic>();
    }
  }

  void Start() {
    if (_isZoomedOut) {
      disableGraphics(_zoomOutGraphics);
    }
    else {
      disableGraphics(_zoomInGraphics);
    }
  }

  private bool _isZoomedOut = false;
  public bool isZoomedOut { get { return _isZoomedOut; } }

  public void ToggleState() {
    _isZoomedOut = !_isZoomedOut;

    if (_isZoomedOut) {
      disableGraphics(_zoomOutGraphics);
      enableGraphics(_zoomInGraphics);
    }
    else {
      disableGraphics(_zoomInGraphics);
      enableGraphics(_zoomOutGraphics);
    }
  }

  private void disableGraphics(LeapGraphic[] graphics) {
    setGraphicState(enabled: false, graphics: graphics);
  }
  private void disableGraphic(LeapGraphic graphic) {
    setGraphicState(enabled: false, graphic: graphic);
  }
  private void enableGraphics(LeapGraphic[] graphics) {
    setGraphicState(enabled: true, graphics: graphics);
  }
  private void enableGraphic(LeapGraphic graphic) {
    setGraphicState(enabled: true, graphic: graphic);
  }

  private void setGraphicState(bool enabled, LeapGraphic[] graphics) {
    foreach (var graphic in graphics) {
      setGraphicState(enabled: enabled, graphic: graphic);
    }
  }
  private void setGraphicState(bool enabled, LeapGraphic graphic) {
    //var alpha = enabled ? 1F : 0F;

    graphic.enabled = enabled;

    //if (graphic is LeapTextGraphic) {
    //  var text = graphic as LeapTextGraphic;
    //  text.color = text.color.WithAlpha(alpha);
    //}
    //else {
    //  //graphic.SetRuntimeTint(graphic.GetRuntimeTint().WithAlpha(alpha));
    //}

  }

}
