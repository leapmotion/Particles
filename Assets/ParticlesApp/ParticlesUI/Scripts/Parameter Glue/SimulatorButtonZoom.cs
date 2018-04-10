using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulatorButtonZoom : SimulatorButtonControl {

  public enum ZoomMode { ZoomIn, ZoomOut }
  public ZoomMode mode;

  public SimulationZoomController zoomController;

  private bool _zooming = false;

  private float _zoomingTime = 0f;
  private float _rampTime = 1f;
  private float _zoomSpeed = 1f;

  protected override void Reset() {
    base.Reset();

    if (zoomController == null) zoomController = FindObjectOfType<SimulationZoomController>();
  }

  void Update() {
    if (_zooming) {
      _zoomingTime += Time.deltaTime;

      float zoomAmount = Mathf.Clamp(_zoomingTime, 0f, _rampTime) / _rampTime
                         * _zoomSpeed * Time.deltaTime;

      switch (mode) {
        case ZoomMode.ZoomIn:
          zoomController.ZoomIn(zoomAmount);
          break;
        case ZoomMode.ZoomOut:
          zoomController.ZoomOut(zoomAmount);
          break;
      }
    }
    else {
      _zoomingTime = 0f;
    }
  }

  public override void onPress() {
    _zooming = true;
  }

  public override void onUnpress() {
    _zooming = false;
  }

}
