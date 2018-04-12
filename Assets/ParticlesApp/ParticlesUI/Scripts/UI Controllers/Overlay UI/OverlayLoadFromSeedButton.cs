using Leap.Unity.Query;
using SFB;
using UnityEngine;
using UnityEngine.UI;

namespace Leap.Unity.Particles {

  public class OverlayLoadFromSeedButton : OverlayButton {

    [Header("Seed Field Input")]
    public InputField inputField;

    protected override void OnClick() {
      TryLoadFromSeed();
    }

    /// <summary>
    /// Will try to load from the seed, but if the name matches a preset's name, it
    /// will load the preset instead.
    /// </summary>
    public void TryLoadFromSeed() {
      var inputText = inputField.text;
      simSetters.LoadEcosystemPresetOrSeed(inputText);
    }

  }

}
