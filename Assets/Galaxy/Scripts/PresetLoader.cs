using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.DevGui;

public class PresetLoader : MonoBehaviour {

  public enum PresetSelection {
    Basic,
    SpeedRamp,
    AccelRamp,
    ByDirection,
    ByStartingBlackHole,
    HeatMap
  }

  [SerializeField]
  private PresetSelection _presetSelection = PresetSelection.Basic;

  [SerializeField]
  public RenderPreset _basic;
  [SerializeField]
  public RenderPreset _speedRamp;
  [SerializeField]
  public RenderPreset _accelRamp;
  [SerializeField]
  public RenderPreset _byDirection;
  [SerializeField]
  public RenderPreset _byStartingBlackHole;
  [SerializeField]
  public RenderPreset _heatMap;

  [DevCategory("Star Rendering")]
  [DevValue]
  public PresetSelection renderMode {
    get {
      return _presetSelection;
    }
    set {
      _presetSelection = value;
      switch (value) {
        case PresetSelection.Basic:
          GetComponent<GalaxyRenderer>().SetPreset(_basic);
          break;
        case PresetSelection.SpeedRamp:
          GetComponent<GalaxyRenderer>().SetPreset(_speedRamp);
          break;
        case PresetSelection.AccelRamp:
          GetComponent<GalaxyRenderer>().SetPreset(_accelRamp);
          break;
        case PresetSelection.ByDirection:
          GetComponent<GalaxyRenderer>().SetPreset(_byDirection);
          break;
        case PresetSelection.ByStartingBlackHole:
          GetComponent<GalaxyRenderer>().SetPreset(_byStartingBlackHole);
          break;
        case PresetSelection.HeatMap:
          GetComponent<GalaxyRenderer>().SetPreset(_heatMap);
          break;
      }
    }
  }
}
