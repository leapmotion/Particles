using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulatorButtonZoomInOut : SimulatorButtonControl {

  public ZoomInOutGraphicStateController stateController;

  public SimulationZoomController zoomController;

  protected override void Reset() {
    base.Reset();

    stateController = GetComponent<ZoomInOutGraphicStateController>();
  }

  public override void onPress() {
    stateController.ToggleState();

    zoomController.ToggleZoom();
  }

}
