using Leap.Unity.Attributes;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Leap.Unity.Animation {

  public class StateSwitchController : MonoBehaviour {

    #region Inspector

    [System.Serializable]
    public class StateDictionary : SerializableDictionary<string, StateSwitch> { }

    [System.Serializable]
    public struct StateSwitch {
      [SerializeField, ImplementsInterface(typeof(IPropertySwitch))]
      private MonoBehaviour _switch;
      public IPropertySwitch propertySwitch {
        get { return _switch as IPropertySwitch; }
      }
    }

    [SDictionary]
    public StateDictionary states;

    [SerializeField, OnEditorChange("curState")]
    private string _curState = "";
    public string curState {
      get { return _curState; }
      set {
        if (!curState.Equals(value)) {
          StateSwitch newStateSwitch;
          if (states.TryGetValue(value, out newStateSwitch)) {

            var oldStateSwitch = states[curState];
            if (oldStateSwitch.propertySwitch != null) {
              if (Application.isPlaying) {
                oldStateSwitch.propertySwitch.Off();
              }
              else {
                oldStateSwitch.propertySwitch.OffNow();
              }
            }
            
            _curState = value;

            if (newStateSwitch.propertySwitch != null) {
              if (Application.isPlaying) {
                newStateSwitch.propertySwitch.On();
              }
              else {
                newStateSwitch.propertySwitch.OnNow();
              }
            }
          }
        }
      }
    }

    #endregion

    #region UnityEvents

    void Start() {
      StateSwitch curStateSwitch;
      if (states.TryGetValue(curState, out curStateSwitch)) {
        if (curStateSwitch.propertySwitch != null) {
          curStateSwitch.propertySwitch.OnNow();
        }
      }
    }

    #endregion

    #region Public API

    public void TransitionTo(string state) {
      curState = state;
    }

    #endregion

  }

}
