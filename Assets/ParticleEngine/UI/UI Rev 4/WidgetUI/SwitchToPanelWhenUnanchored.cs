using Leap.Unity.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchToPanelWhenUnanchored : MonoBehaviour {

  public WidgetModeController widgetModeController;

  public AnchorableBehaviour anchObj;
  private InteractionBehaviour _intObj;

  void Reset() {
    if (anchObj == null) anchObj = GetComponent<AnchorableBehaviour>();
  }

  void Start() {
    anchObj.OnPostTryAnchorOnGraspEnd += onPostTryAnchorOnGraspEnd;

    _intObj = anchObj.interactionBehaviour;
    _intObj.OnGraspBegin += onGraspBegin;
  }

  private int _counter = 0;

  void Update() {
    if (_eligibleForPanel && !_intObj.isGrasped) {
      _counter++;
      if (_counter > 3) {
        widgetModeController.TransitionToPanel();

        _eligibleForPanel = false;
        _counter = 0;
      }
    }
    else {
      _counter = 0;
    }
  }

  private bool _eligibleForPanel = false;

  private void onPostTryAnchorOnGraspEnd() {
    if (anchObj.preferredAnchor == null) {
      _eligibleForPanel = true;
    }
  }

  private void onGraspBegin() {
    _eligibleForPanel = false;
  }

}
