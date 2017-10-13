using Leap.Unity.Animation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Layout {

  public class UILookPositionProvider : MonoBehaviour,
                                        IWorldPositionProvider {

    public Transform lookAnchorTransform;

    [Header("Optional")]

    [Tooltip("If this property is non-null, its 'on' localPosition (transformed into "
           + "world space) will be used to calculate the look anchor world position, "
           + "instead of relying on the current transform's position.")]
    public TranslationSwitch lookAnchorTranslationSwitch;

    #region IWorldPositionProvider

    public Vector3 GetTargetWorldPosition() {
      if (lookAnchorTranslationSwitch != null) {
        return lookAnchorTranslationSwitch.localTranslateTarget
                                          .parent
                                          .TransformPoint(lookAnchorTranslationSwitch.onLocalPosition);
      }
      else {
        return lookAnchorTransform.position;
      }
    }

    #endregion
  }


}