using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulatorZoomInOut : SimulatorButtonControl {

  public ZoomInOutGraphicStateController stateController;

  private float _targetScale = 1.0F;

  protected override void Reset() {
    base.Reset();

    stateController = GetComponent<ZoomInOutGraphicStateController>();
  }

  public override void onPress() {
    stateController.ToggleState();

    if (stateController.isZoomedOut) {
      _targetScale = 0.2F;
    }
    else {
      _targetScale = 1F;
    }
  }

  void Update() {
    float scale = simulator.transform.localScale.x;

    scale = Mathf.Lerp(scale, _targetScale, 5F * Time.deltaTime);

    simulator.transform.localScale = Vector3.one * scale;
  }

}
