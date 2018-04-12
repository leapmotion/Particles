using Leap.Unity.Query;
using SFB;

namespace Leap.Unity.Particles {

  public class OverlayLoadFromFileButton : OverlayButton {

    protected override EventResult DoClickOperation() {
      return TryLoadFromFile();
    }

    public EventResult TryLoadFromFile() {
      var path = StandaloneFileBrowser.OpenFilePanel(
        "Open Ecosystem Description", "", allowedExtensions, multiselect: false)
        .Query().FirstOrDefault();
      if (string.IsNullOrEmpty(path)) {
        // Assume intentional cancellation; no error displayed.
        return EventResult.Nothing;
      }
      else {
        if (!simManager.LoadEcosystem(path)) {
          // Failed to load.
          return EventResult.Failure;
        }
        else {
          // Successfully loaded ecosystem.
          return EventResult.Success;
        }
      }
    }

  }

}
