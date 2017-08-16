using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FaceCameraOnUpdate : MonoBehaviour {

  private Camera _mainCamera;

  void Start() {
    _mainCamera = Camera.main;
  }

  void Update() {
    if (_mainCamera == null) {
      _mainCamera = Camera.main;
    }
    this.transform.rotation = Quaternion.LookRotation(_mainCamera.transform.position
                                                      - this.transform.position);
  }

}
