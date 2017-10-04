using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Interaction;

public class GrabManager : MonoBehaviour {

  public InteractionBehaviour left, right;
  public GrabSwitch switchLeft, switchRight;

  private Vector3 _localLeft;
  private Vector3 _localRight;

  private Quaternion _leftToRight;
  private Quaternion _rightToLeft;

  private void Start() {
    _localLeft = transform.InverseTransformPoint(left.transform.position);
    _localRight = transform.InverseTransformPoint(right.transform.position);
  }

  private void Update() {
    switchLeft.grasped = false;
    switchRight.grasped = false;

    if (left.isGrasped && right.isGrasped) {
      switchLeft.grasped = true;
      switchRight.grasped = true;
      transform.position = (left.transform.position + right.transform.position) * 0.5f;

      _leftToRight = right.transform.rotation * Quaternion.Inverse(left.transform.rotation);
      _rightToLeft = left.transform.rotation * Quaternion.Inverse(right.transform.rotation);
    } else if (left.isGrasped) {
      switchLeft.grasped = true;
      switchRight.grasped = true;

      _localLeft = transform.InverseTransformPoint(left.transform.position);
      _localRight = _localLeft;
      _localRight *= -1;
      Vector3 globalRight = transform.TransformPoint(_localRight);
      right.transform.position = globalRight;
      right.transform.rotation = _leftToRight * left.transform.rotation;
    } else if (right.isGrasped) {
      switchLeft.grasped = true;
      switchRight.grasped = true;

      _localRight = transform.InverseTransformPoint(right.transform.position);
      _localLeft = _localRight;
      _localLeft *= -1;
      Vector3 globalLeft = transform.TransformPoint(_localLeft);
      left.transform.position = globalLeft;
      left.transform.rotation = _rightToLeft * right.transform.rotation;
    } else if (GetComponent<InteractionBehaviour>().isGrasped) {
      //switchLeft.grasped = true;
      //switchRight.grasped = true;
    } else {
      _localLeft = _localLeft.normalized * 0.1f / 0.07941258f;
      _localRight = _localRight.normalized * 0.1f / 0.07941258f;
      _leftToRight = Quaternion.identity;
      _rightToLeft = Quaternion.identity;
    }

    if (!left.isGrasped && !right.isGrasped) {
      left.transform.position = transform.TransformPoint(_localLeft);
      right.transform.position = transform.TransformPoint(_localRight);
    }

    switchLeft.Position = left.transform.position;
    switchLeft.Rotation = left.transform.rotation;

    switchRight.Position = right.transform.position;
    switchRight.Rotation = right.transform.rotation;
  }
}
