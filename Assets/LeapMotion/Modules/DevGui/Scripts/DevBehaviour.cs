using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.DevGui {

  public class DevBehaviour : MonoBehaviour {

    private void Awake() {
      Dev.Register(this);
    }

    private void OnDestroy() {
      Dev.Unregister(this);
    }

  }
}
