using UnityEngine;
using UnityEngine.UI;

namespace Leap.Unity.Particles {

  public class OverlayLoadFromSeedButton : OverlayButton {

    [Header("Seed Field Input")]
    public InputField inputField;

    protected override EventResult DoClickOperation() {
      return TryLoadFromSeed();
    }

    /// <summary>
    /// Will try to load from the seed, but if the name matches a preset's name, it
    /// will load the preset instead.
    /// </summary>
    public EventResult TryLoadFromSeed() {
      try {
        var inputText = inputField.text;
        simSetters.LoadEcosystemPresetOrSeed(inputText);
        return EventResult.Success;
      }
      catch (System.Exception) {
        return EventResult.Failure;
      }
    }

  }

}
