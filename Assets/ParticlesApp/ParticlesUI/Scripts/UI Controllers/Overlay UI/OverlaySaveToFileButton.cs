using Leap.Unity.Query;
using SFB;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace Leap.Unity.Particles {

  public class OverlaySaveToFileButton : OverlayButton {

    protected override void OnClick() {
      TrySaveToFile();
    }

    public void TrySaveToFile() {
      var currentEcosystemName = simSetters.GetEcosystemName();
      var defaultFileName = Path.ChangeExtension(currentEcosystemName, ".json");

      var path = StandaloneFileBrowser.SaveFilePanel(
        "Save Ecosystem Description", "", defaultFileName, allowedExtensions);

      if (string.IsNullOrEmpty(path)) {
        // Assume intentional cancellation; no error displayed.
      }
      else {
        if (!simManager.SaveEcosystem(path)) {
          // Failed to save.
          if (failureNotification != null) {
            failureNotification.Notify();
          }
        }
        else {
          // Successfully saved ecosystem.
          if (successNotification != null) {
            successNotification.Notify();
          }
        }
      }
    }

  }

}
