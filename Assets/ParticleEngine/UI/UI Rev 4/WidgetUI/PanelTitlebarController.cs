using Leap.Unity;
using Leap.Unity.Interaction;
using Leap.Unity.Interaction.Internal;
using Leap.Unity.Query;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanelTitlebarController : MonoBehaviour {

  public Transform panelCenterTransform;

  public InteractionBehaviour intObj;

  public WidgetModeController widgetModeController;

  void Reset() {
    if (intObj == null) intObj = GetComponent<InteractionBehaviour>();
    if (widgetModeController == null) widgetModeController = GetComponentInParent<WidgetModeController>();
  }

  void Awake() {
    intObj.OnGraspedMovement += onGraspedMovement;

    intObj.OnGraspEnd += onGraspEnd;
  }

  void Update() {
    if (_graspingController != null) {
      var intHand = _graspingController.intHand;
      if (intHand != null) {
        updateGraspedPose(intHand.isRight ? Hands.Right.PalmPosition.ToVector3()
                                          : Hands.Left.PalmPosition.ToVector3());
      }
    }
  }

  private bool _hasLastGraspedPos = false;
  private Vector3 _lastGraspedPos = Vector3.zero;
  private InteractionController _graspingController;

  private void onGraspedMovement(Vector3 origPos, Quaternion origRot,
                                 Vector3 newPos,  Quaternion newRot,
                                 List<InteractionController> graspingControllers) {
    // Undo the movement due to grasping, we're going to move it differently.
    this.transform.position = origPos;
    intObj.rigidbody.position = origPos;
    this.transform.rotation = origRot;
    intObj.rigidbody.rotation = origRot;

    _graspingController = graspingControllers.Query().FirstOrDefault();

    updateGraspedPose(_graspingController.position);
  }

  private void onGraspEnd() {
    _hasLastGraspedPos = false;
    _lastGraspedPos = Vector3.zero;
    _graspingController = null;
  }

  private void updateGraspedPose(Vector3 graspPosition) {
    Vector3 graspedPos = graspPosition;

    if (!_hasLastGraspedPos) {
      _lastGraspedPos = graspedPos;
      _hasLastGraspedPos = true;
    }
    else {
      // Translate the panelCenterTransform by the translation due to grasping.
      Vector3 graspedPosTranslation = graspedPos - _lastGraspedPos;
      panelCenterTransform.position += graspedPosTranslation;

      // Remember the panelCenterTransform's translation from the original grasp position.
      Vector3 graspedPos_pCLocal = panelCenterTransform.InverseTransformPoint(graspedPos);

      // Rotate about the panelCenterTransform to face the camera.
      Vector3 pCToCam = -1f * (Camera.main.transform.position - panelCenterTransform.position);
      Vector3 horizonRight = Vector3.Cross(Vector3.up, Camera.main.transform.forward);
      Vector3 pCUp = Vector3.Cross(pCToCam.normalized, horizonRight.normalized);
      panelCenterTransform.rotation = Quaternion.LookRotation(pCToCam, pCUp);

      // Translate the panelCenterTransform so that the position grasped is still at the same
      // position relative to the hand.
      Vector3 misalignedGraspPoint = panelCenterTransform.TransformPoint(graspedPos_pCLocal);
      Vector3 correctionForGraspPoint = graspedPos - misalignedGraspPoint;
      panelCenterTransform.transform.position += correctionForGraspPoint;

      widgetModeController.MoveSelfToPanel();

      _lastGraspedPos = graspedPos;
    }
  }

}
