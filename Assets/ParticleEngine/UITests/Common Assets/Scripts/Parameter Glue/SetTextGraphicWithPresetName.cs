using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetTextGraphicWithPresetName : SetTextGraphicWithSimulatorParam {

  public PresetController _controller;

  protected override void Reset() {
    base.Reset();

    if (_controller == null) _controller = GetComponentInParent<PresetController>();
  }

  public override string GetTextValue() {
    return _controller.GetCurrentPresetName();
  }

}
