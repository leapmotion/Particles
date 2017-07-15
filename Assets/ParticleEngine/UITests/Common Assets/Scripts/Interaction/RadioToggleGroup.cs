using Leap.Unity.Attributes;
using Leap.Unity.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RadioToggleGroup : MonoBehaviour {

  [EditTimeOnly]
  public List<InteractionToggle> toggles;

  void Awake() {
    for (int i = 0; i < toggles.Count; i++) {
      var toggle = toggles[i];

      toggle.OnToggle += () => { toggle.controlEnabled = false; };

      for (int j = 0; j < toggles.Count; j++) {
        if (j == i) continue;

        var otherToggle = toggles[j];
        toggle.OnToggle += () => {
          otherToggle.controlEnabled = true;
          otherToggle.isToggled = false;
        };
      }
    }
  }

}
