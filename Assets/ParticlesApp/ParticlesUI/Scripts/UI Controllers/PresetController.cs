using Leap.Unity.Attributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PresetController : MonoBehaviour {

  public SimulationManager simManager;

  [SerializeField, OnEditorChange("presetIdx")]
  private int _curPresetIdx = 0;
  public int curPresetIdx {
    get { return _curPresetIdx; }
    set { _curPresetIdx = Mathf.Clamp(value, 0, _numPresets - 1); }
  }

  private int _numPresets;
  private string[] _presetNames;

  private void Reset() {
    if (simManager == null) {
      simManager = FindObjectOfType<SimulationManager>();
    }
  }

  private void OnValidate() {
    initPresets();
  }

  private void Awake() {
    initPresets();
  }

  private void initPresets() {
    _numPresets = System.Enum.GetNames(typeof(EcosystemPreset)).Length;
    _presetNames = new string[_numPresets];

    for (int i = 0; i < _numPresets; i++) {
      _presetNames[i] = ((EcosystemPreset)i).ToString();
    }

    // Make sure presetIdx is still valid.
    curPresetIdx = curPresetIdx;
  }

  public void MoveNextPreset() {
    curPresetIdx += 1;
  }

  public void MovePrevPreset() {
    curPresetIdx -= 1;
  }

  public void LoadPreset() {
    simManager.RestartSimulation((EcosystemPreset)curPresetIdx);
  }


  public string GetCurrentPresetName() {
    return _presetNames[_curPresetIdx];
  }

}
