using Leap.Unity.Interaction;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIButton_SetRenderPreset : UIButton {

  [Header("Set Color Mode")]

  public PresetLoader.PresetSelection loadOnPress;

  public override void OnPress() {
    GalaxyUIOperations.LoadRenderPreset(loadOnPress);
  }

}
