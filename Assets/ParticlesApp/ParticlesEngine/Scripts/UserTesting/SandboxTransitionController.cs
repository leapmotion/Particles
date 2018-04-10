using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using Leap.Unity;
using Leap.Unity.GraphicalRenderer;

public class SandboxTransitionController : MonoBehaviour {

  public GameObject sandboxAnchor;
  public LeapTextGraphic textLabel;
  public StreamingAsset congratsFile;

  private string congratsText;

  private void OnEnable() {
    congratsText = File.ReadAllText(congratsFile.Path);
  }

  public void BeginSandboxTransition() {
    sandboxAnchor.SetActive(true);
    textLabel.text = congratsText;
  }

  public void OnFinalTransition() {
    SceneManager.LoadSceneAsync(1);
  }

  private void Update() {
    if (sandboxAnchor.activeInHierarchy) {
      if (Input.GetKeyDown(KeyCode.UpArrow)) {
        OnFinalTransition();
      }
    }
  }
}
