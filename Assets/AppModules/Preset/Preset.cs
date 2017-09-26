using System;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity {

  public abstract class PresetBase { }

  [Serializable]
  public abstract class Preset<T> : PresetBase {

    [SerializeField]
    protected int _currPresetIndex = 0;

    [SerializeField]
    protected List<string> _presetNames = new List<string>();

    [SerializeField]
    protected List<T> _presets = new List<T>();

    public int CurrentIndex {
      get {
        return _currPresetIndex;
      }
      set {
        _currPresetIndex = Mathf.Clamp(value, 0, _presets.Count - 1);
      }
    }

    public int Count {
      get {
        return _presets.Count;
      }
    }

    public T Current {
      get {
        if (_presets.Count == 0) {
          throw new InvalidOperationException("Cannot access a preset if there are none defined.");
        }
        _currPresetIndex = Mathf.Clamp(_currPresetIndex, 0, _presets.Count - 1);
        return _presets[_currPresetIndex];
      }
    }
  }
}
