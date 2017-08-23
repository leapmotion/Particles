using System.IO;
using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Leap.Unity;
using Leap.Unity.Interaction;

public class UserTestController : MonoBehaviour {

  public TextureSimulator sim;
  public InteractionButton nextButton;
  public Text textLabel;
  public StreamingFolder textFolder;
  public StreamingFolder ecosystemFolder;

  IEnumerator Start() {
    while (!isButtonPressed) {
      yield return null;
    }
    while (isButtonPressed) {
      yield return null;
    }

    var ecosystemPaths = Directory.GetFiles(ecosystemFolder.Path).
                                   Where(p => p.EndsWith(".json")).
                                   OrderBy(p => p).
                                   ToList();

    foreach (var ecosystemPath in ecosystemPaths) {
      string ecosystemName = Path.GetFileNameWithoutExtension(ecosystemPath);

      var desc = JsonUtility.FromJson<TextureSimulator.SimulationDescription>(File.ReadAllText(ecosystemPath));
      sim.RestartSimulation(desc, TextureSimulator.ResetBehavior.SmoothTransition);
      Debug.Log("Loaded ecosystem for " + ecosystemName);

      for (int scriptIndex = 0; scriptIndex < 100; scriptIndex++) {
        string scriptPath = Path.Combine(textFolder.Path, ecosystemName + " " + scriptIndex + ".txt");
        if (!File.Exists(scriptPath)) {
          scriptPath = Path.Combine(textFolder.Path, ecosystemName + " 0" + scriptIndex + ".txt");
        }

        if (!File.Exists(scriptPath)) {
          continue;
        }

        Debug.Log("Loaded text for " + Path.GetFileName(scriptPath));
        textLabel.text = File.ReadAllText(scriptPath);

        while (!isButtonPressed) {
          yield return null;
        }
        while (isButtonPressed) {
          yield return null;
        }
      }
    }
  }

  private bool isButtonPressed {
    get {
      return nextButton.isPressed || Input.GetKey(KeyCode.F10);
    }
  }
}
