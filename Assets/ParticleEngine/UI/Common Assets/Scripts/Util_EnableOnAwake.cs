using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Util_EnableOnAwake : MonoBehaviour {

  public GameObject[] objectsToEnable;

  private void Awake() {
    foreach (var obj in objectsToEnable) {
      obj.SetActive(true);
    }
  }

}
