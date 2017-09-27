using Leap.Unity.Attributes;
using Leap.Unity.Query;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Animation {

  public class ObjectSwitch : MonoBehaviour, IPropertySwitch {

    #region Inspector

    [Header("Attached Tween Overrides")]

    [Tooltip("If checked, you can specify the tween times for all tween-based "
           + "switches attached to this object switch.")]
    public bool overrideTweenTime = false;

    [DisableIf("overrideTweenTime", isEqualTo: false)]
    public float tweenTime = 1f;

    #endregion

    #region Attached Switches

    private List<IPropertySwitch> _switches = new List<IPropertySwitch>();
    public ReadonlyList<IPropertySwitch> switches {
      get { return _switches; }
    }

    private bool _refreshed = false;

    #endregion

    #region Unity Events

    protected virtual void Reset() {
      RefreshSwitches();
    }

    protected virtual void OnValidate() {
      RefreshSwitches();
    }

    protected virtual void Start() {
      RefreshSwitches();
    }

    public void RefreshSwitches() {
      GetComponents<MonoBehaviour>().Query()
                                    .Where(c => c is IPropertySwitch
                                                && !(c == this)
                                                && c.enabled)
                                    .Select(c => c as IPropertySwitch)
                                    .FillList(_switches);

      _refreshed = true;

      if (Application.isPlaying && overrideTweenTime) {
        foreach (var tweenSwitch in _switches.Query()
                                             .Where(s => s is TweenSwitch)
                                             .Cast<TweenSwitch>()) {
          tweenSwitch.tweenTime = tweenTime;
        }
      }
    }

    #endregion

    #region Switch Implementation

    public void On() {
      if (!_refreshed) RefreshSwitches();
      
      foreach (var propertySwitch in _switches) {
        propertySwitch.On();
      }
    }

    public void OnNow() {
      foreach (var propertySwitch in _switches) {
        propertySwitch.OnNow();
      }
    }

    public bool GetIsOnOrTurningOn() {
      return _switches.Query().Any(c => c.GetIsOnOrTurningOn());
    }

    public void Off() {
      foreach (var propertySwitch in _switches) {
        propertySwitch.Off();
      }
    }

    public void OffNow() {
      foreach (var propertySwitch in _switches) {
        propertySwitch.OffNow();
      }
    }

    public bool GetIsOffOrTurningOff() {
      return _switches.Query().Any(c => c.GetIsOffOrTurningOff());
    }

    #endregion

  }

}
