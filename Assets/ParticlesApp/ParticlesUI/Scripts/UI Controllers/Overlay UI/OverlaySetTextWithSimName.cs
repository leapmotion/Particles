using UnityEngine;
using UnityEngine.UI;

namespace Leap.Unity.Particles {

  public class OverlaySetTextWithSimName : MonoBehaviour {

    public SimulatorSetters simSetters;
    public Text uiText;
    public string prefix = "";
    public bool newlineAfterPrefix = true;
    public string addlPresetOnlyPrefix = "";
    public string postfix = "";

    private void Reset() {
      if (simSetters == null) simSetters = FindObjectOfType<SimulatorSetters>();
      if (uiText == null) uiText = GetComponent<Text>();
    }
    
    void Update() {
      if (uiText != null && simSetters != null) {
        var isPreset = simSetters.IsCurrentEcosystemAPreset();

        uiText.text = prefix + (newlineAfterPrefix ? "\n" : "")
          + (isPreset ? addlPresetOnlyPrefix : "") + simSetters.GetEcosystemName()
          + postfix;
      }
    }
  }

}
