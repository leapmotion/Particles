using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OverlayToggleController : MonoBehaviour {

  public KeyCode toggleKey = KeyCode.F12;

  public GameObject[] objectsToToggle;

  private void Update() {
    if (Input.GetKeyDown(toggleKey)) {
      if (objectsToToggle != null && objectsToToggle.Length > 0) {
        for (int i = 0; i < objectsToToggle.Length; i++) {
          var obj = objectsToToggle[i];
          obj.SetActive(!obj.activeSelf);
        }
      }
    }
  }

}
