using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolbeltFollower : MonoBehaviour {

  public Transform target;
  public float lerpCoeffPerSec = 20F;
  public float slerpCoeffPerSec = 10F;

  public bool stiffened = false;

  private float _effLerpCoeffPerSec;
  private float _effSlerpCoeffPerSec;

  void Start() {
    _effLerpCoeffPerSec = lerpCoeffPerSec;
    _effSlerpCoeffPerSec = slerpCoeffPerSec;
  }

  void Update() {
    if (target != null) {
      float targetLerpCoeffPerSec = lerpCoeffPerSec * (stiffened ? 0.2F : 1F);
      _effLerpCoeffPerSec = Mathf.Lerp(_effLerpCoeffPerSec, targetLerpCoeffPerSec, 20F * Time.deltaTime);

      Vector3 delta = target.transform.position - this.transform.position;
      this.transform.position = Vector3.Lerp(this.transform.position, target.transform.position + (delta * 1F), _effLerpCoeffPerSec * Time.deltaTime);

      float targetSlerpCoeffPerSec = slerpCoeffPerSec * (stiffened ? 0.2F : 1F);
      _effSlerpCoeffPerSec = Mathf.Lerp(_effSlerpCoeffPerSec, targetSlerpCoeffPerSec, 20F * Time.deltaTime);
      
      Quaternion targetRotation = Quaternion.LookRotation(target.transform.forward.ProjectOnPlane(Vector3.up));
      this.transform.rotation = Quaternion.Slerp(this.transform.rotation, targetRotation, _effSlerpCoeffPerSec * Time.deltaTime);
    }
  }

  public void StiffenPosition() {
    stiffened = true;
  }

  public void RelaxPosition() {
    stiffened = false;
  }

}

public static class ToolbeltFollowerVector3Extensions {

  public static Vector3 ProjectOnPlane(this Vector3 v, Vector3 n) {
    return Vector3.ProjectOnPlane(v, n);
  }

}