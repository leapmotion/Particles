using UnityEngine;

namespace Leap.Unity.Particles {

  public class PressEscToQuit : MonoBehaviour {

    private void Update() {
      if (Input.GetKeyDown(KeyCode.Escape)) {
        Application.Quit();
      }
    }

  }

}
