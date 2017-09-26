using Leap.Unity.Attributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Leap.Unity.Animation {

  public class SequenceAppearVanishController : TweenAppearVanishController {

    [Header("Sequence Provider (Must have IAppearVanishControllers)")]
    [ImplementsInterface(typeof(IGameObjectSequenceProvider))]
    public MonoBehaviour sequenceProvider;

    private IGameObjectSequenceProvider _objSequenceProvider {
      get { return sequenceProvider as IGameObjectSequenceProvider; }
    }

    protected override void updateAppearVanish(float time, bool immediately = false) {
      int numObjects = _objSequenceProvider.Count;
      int numApparentObjects = (int)(time * numObjects);

      for (int i = 0; i < numObjects; i++) {
        var appearVanishController = _objSequenceProvider[i].GetComponent<IAppearVanishController>();
        if (appearVanishController == null) {
          Debug.LogError("Unable to make " + _objSequenceProvider[i].name + " appear or "
                       + "vanish; it must have a component that implements "
                       + "IAppearVanishController");
          continue;
        }

        if (i < numApparentObjects) {
          if (immediately) {
            appearVanishController.AppearNow();
          }
          else {
            appearVanishController.Appear();
          }
        }
        else {
          if (immediately) {
            appearVanishController.VanishNow();
          }
          else {
            appearVanishController.Vanish();
          }
        }
      }
    }

  }

}

