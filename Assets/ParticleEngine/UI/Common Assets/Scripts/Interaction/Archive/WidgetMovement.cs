using Leap.Unity.Attributes;
using Leap.Unity.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(InteractionBehaviour))]
public class WidgetMovement : MonoBehaviour {

  private InteractionBehaviour _intObj;
  private Quaternion _targetRotation;

  public bool lockXAxis = false;

  [DisableIf("lockXAxis", isEqualTo: false)]
  public float targetXRotation = 0F;

  void Awake() {
    _intObj = GetComponent<InteractionBehaviour>();
  }

  void OnEnable() {
    _intObj.OnGraspedMovement += onGraspedMovement;
    _intObj.manager.OnPostPhysicalUpdate += updateRotation;

    _targetRotation = GetTargetRotation(_intObj.rigidbody.rotation);
  }

  void OnDisable() {
    _intObj.OnGraspedMovement -= onGraspedMovement;
    _intObj.manager.OnPostPhysicalUpdate -= updateRotation;
  }

  private void onGraspedMovement(Vector3 prevPos, Quaternion prevRot,
                                 Vector3 curPos,  Quaternion curRot,
                                 List<InteractionController> controllers) {
    _targetRotation = GetTargetRotation(curRot);
    _intObj.rigidbody.rotation = Quaternion.LookRotation(prevRot * Vector3.forward, Vector3.up);
  }

  private Quaternion GetTargetRotation(Quaternion fromRot) {
    var targetRotation = Quaternion.LookRotation(fromRot * Vector3.forward, Vector3.up);
    if (lockXAxis) {
      Vector3 euler = targetRotation.eulerAngles;
      euler.x = targetXRotation;
      targetRotation = Quaternion.Euler(euler);
    }
    return targetRotation;
  }

  private void updateRotation() {
    _intObj.rigidbody.rotation = Quaternion.Slerp(_intObj.rigidbody.rotation, _targetRotation, 10F * Time.fixedDeltaTime);
  }

}
