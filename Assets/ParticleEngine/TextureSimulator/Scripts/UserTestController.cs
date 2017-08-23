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
    var ecosystemPaths = Directory.GetFiles(ecosystemFolder.Path).
                                   OrderBy(p => int.Parse(p.Split()[0])).
                                   ToList();

    foreach (var ecosystemPath in ecosystemPaths) {
      string ecosystemName = ecosystemPath.Split()[1].Replace(".json", "");

      var desc = JsonUtility.FromJson<TextureSimulator.SimulationDescription>(File.ReadAllText(ecosystemPath));
      sim.RestartSimulation(desc, TextureSimulator.ResetBehavior.SmoothTransition);

      for (int scriptIndex = 0; scriptIndex < 100; scriptIndex++) {
        string scriptPath = scriptIndex + " " + ecosystemName + ".txt";
        if (!File.Exists(scriptPath)) {
          continue;
        }

        textLabel.text = File.ReadAllText(scriptPath);

        while (!nextButton.isPressed) {
          yield return null;
        }
        while (nextButton.isPressed) {
          yield return null;
        }
      }
    }
  }
}
