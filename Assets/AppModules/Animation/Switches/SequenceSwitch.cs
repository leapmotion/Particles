using Leap.Unity.Attributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Leap.Unity.Animation {

  public class SequenceSwitch : TweenSwitch {

    #region Inspector

    [Header("Sequence Provider (elements require IPropertySwitch)")]
    
    // TODO: Can't / shouldn't this be ISequenceProvider<GameObject> ?
    [ImplementsInterface(typeof(IGameObjectSequenceProvider))]
    public MonoBehaviour sequenceProvider;
    private IGameObjectSequenceProvider _objSequenceProvider {
      get { return sequenceProvider as IGameObjectSequenceProvider; }
    }

    #endregion

    #region Switch Implementation

    protected override void updateSwitch(float time, bool immediately = false) {
      int totalNumObjects = _objSequenceProvider.Count;
      int numOnObjects = (int)(time * totalNumObjects);

      for (int i = 0; i < totalNumObjects; i++) {
        var propertySwitch = _objSequenceProvider[i].GetComponent<IPropertySwitch>();

        // Each element needs a component that implements IPropertySwitch for the
        // sequence switch to function properly.
        if (propertySwitch == null) {
          Debug.LogError("Unable to switch " + _objSequenceProvider[i].name + ";"
                       + "it must have a component that implements IPropertySwitch.",
                       _objSequenceProvider[i]);
          continue;
        }

        if (i < numOnObjects) {
          if (immediately) {
            propertySwitch.OnNow();
          }
          else {
            propertySwitch.On();
          }
        }
        else {
          if (immediately) {
            propertySwitch.OffNow();
          }
          else {
            propertySwitch.Off();
          }
        }
      }
    }

    #endregion

  }

}

