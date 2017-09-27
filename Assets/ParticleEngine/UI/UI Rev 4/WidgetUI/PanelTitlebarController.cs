using Leap.Unity.Interaction;
using Leap.Unity.Interaction.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanelTitlebarController : MonoBehaviour {

  public InteractionBehaviour intObj;

  private Action<Vector3, Quaternion, Vector3, Quaternion, List<InteractionController>>
    onGraspedMovementAction;

  void Reset() {
    if (intObj == null) intObj = GetComponent<InteractionBehaviour>();
  }

  void Start() {
    onGraspedMovementAction = onGraspedMovement;
    intObj.OnGraspedMovement += onGraspedMovementAction;
  }

  private void onGraspedMovement(Vector3 origPos, Quaternion origRot,
                                 Vector3 newPos,  Quaternion newRot,
                                 List<InteractionController> graspingControllers) {

  }

}
