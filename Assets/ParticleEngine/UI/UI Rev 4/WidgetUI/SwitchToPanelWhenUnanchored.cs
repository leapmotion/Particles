using Leap.Unity.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchToPanelWhenUnanchored : MonoBehaviour {

  public WidgetModeController widgetModeController;

  public AnchorableBehaviour anchObj;

  void Reset() {
    if (anchObj == null) anchObj = GetComponent<AnchorableBehaviour>();
  }

  void Start() {
    anchObj.OnPostTryAnchorOnGraspEnd += onPostTryAnchorOnGraspEnd;
  }

  private void onPostTryAnchorOnGraspEnd(AnchorableBehaviour anchObj) {
    if (anchObj.preferredAnchor == null) {
      widgetModeController.TransitionToPanel();
    }
  }

}
