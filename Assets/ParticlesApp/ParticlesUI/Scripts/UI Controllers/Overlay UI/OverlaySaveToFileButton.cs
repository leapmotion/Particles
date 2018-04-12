using SFB;
using System.IO;

namespace Leap.Unity.Particles {

  public class OverlaySaveToFileButton : OverlayButton {

    protected override EventResult DoClickOperation() {
      return TrySaveToFile();
    }

    public EventResult TrySaveToFile() {
      var currentEcosystemName = simSetters.GetEcosystemName();
      var defaultFileName = Path.ChangeExtension(currentEcosystemName, ".json");

      var path = StandaloneFileBrowser.SaveFilePanel(
        "Save Ecosystem Description", "", defaultFileName, allowedExtensions);

      if (string.IsNullOrEmpty(path)) {
        // Assume intentional cancellation; no error displayed.
        return EventResult.Nothing;
      }
      else {
        if (!simManager.SaveEcosystem(path)) {
          // Failed to save.
          return EventResult.Failure;
        }
        else {
          // Successfully saved ecosystem.
          return EventResult.Success;
        }
      }
    }

  }

}
