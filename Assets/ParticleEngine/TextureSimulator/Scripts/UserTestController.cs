using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;
using Leap.Unity.Interaction;
using Leap.Unity.GraphicalRenderer;

public class UserTestController : MonoBehaviour {

  public TextureSimulator sim;
  public InteractionButton nextButton;
  public LeapTextGraphic textLabel;
  public StreamingFolder textFolder;
  public StreamingFolder ecosystemFolder;

  private List<string> _ecosystemPaths;
  private List<string> _scriptPaths = new List<string>();
  private int _currEcosystem = -1;
  private int _currScript = 0;

  void Start() {
    _ecosystemPaths = Directory.GetFiles(ecosystemFolder.Path).
                                Where(p => p.EndsWith(".json")).
                                OrderBy(p => p).
                                ToList();

    //foreach (var ecosystemPath in ecosystemPaths) {
    //  string ecosystemName = Path.GetFileNameWithoutExtension(ecosystemPath);

    //  var desc = JsonUtility.FromJson<TextureSimulator.SimulationDescription>(File.ReadAllText(ecosystemPath));
    //  sim.RestartSimulation(desc, TextureSimulator.ResetBehavior.SmoothTransition);
    //  Debug.Log("Loaded ecosystem for " + ecosystemName);

    //  for (int scriptIndex = 0; scriptIndex < 100; scriptIndex++) {
    //    string scriptPath = Path.Combine(textFolder.Path, ecosystemName + " " + scriptIndex + ".txt");
    //    if (!File.Exists(scriptPath)) {
    //      scriptPath = Path.Combine(textFolder.Path, ecosystemName + " 0" + scriptIndex + ".txt");
    //    }

    //    if (!File.Exists(scriptPath)) {
    //      continue;
    //    }

    //    Debug.Log("Loaded text for " + Path.GetFileName(scriptPath));
    //    textLabel.text = File.ReadAllText(scriptPath);
    //  }
    //}
  }

  void Update() {
    if (Input.GetKeyDown(KeyCode.RightArrow)) {
      OnNext();
    }

    if (Input.GetKeyDown(KeyCode.LeftArrow)) {
      OnPrev();
    }

    if (Input.GetKeyDown(KeyCode.DownArrow)) {
      OnRestart();
    }
  }

  public void OnNext() {
    if (_currScript == _scriptPaths.Count - 1 && _currEcosystem == _ecosystemPaths.Count - 1) {
      return;
    }

    _currScript++;
    if (_currScript >= _scriptPaths.Count) {
      _currEcosystem++;
      _currScript = 0;

      loadScripts();
      transition(TextureSimulator.ResetBehavior.ResetPositions);
    }

    updateText();
  }

  public void OnPrev() {
    if (_currEcosystem <= 0 && _currScript <= 0) {
      return;
    }

    _currScript--;
    if (_currScript < 0) {
      _currEcosystem--;
      loadScripts();
      _currScript = _scriptPaths.Count - 1;

      transition(TextureSimulator.ResetBehavior.ResetPositions);
    }

    updateText();
  }

  public void OnRestart() {
    _currScript = 0;
    transition(TextureSimulator.ResetBehavior.ResetPositions);
    updateText();
  }

  private void loadScripts() {
    string currEcosystemPath = _ecosystemPaths[_currEcosystem];
    string ecosystemName = Path.GetFileNameWithoutExtension(currEcosystemPath);
    _scriptPaths = Directory.GetFiles(textFolder.Path).
                             Where(p => p.Contains(ecosystemName)).
                             Where(p => p.EndsWith(".txt")).
                             OrderBy(p => p.Replace(ecosystemName, "")).
                             ToList();
  }

  private void transition(TextureSimulator.ResetBehavior resetBehavior) {
    var desc = JsonUtility.FromJson<TextureSimulator.SimulationDescription>(File.ReadAllText(_ecosystemPaths[_currEcosystem]));
    sim.RestartSimulation(desc, resetBehavior);
  }

  private void updateText() {
    textLabel.text = File.ReadAllText(_scriptPaths[_currScript]);
  }



}
