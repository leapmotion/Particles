using Leap.Unity.Query;
using SFB;
using UnityEngine;
using UnityEngine.UI;

namespace Leap.Unity.Particles {

  public class OverlayLoadFromFileButton : OverlayButton {

    protected override void OnClick() {
      TryLoadFromFile();
    }

    public void TryLoadFromFile() {
      var path = StandaloneFileBrowser.OpenFilePanel(
        "Open Ecosystem Description", "", allowedExtensions, multiselect: false)
        .Query().FirstOrDefault();
      if (string.IsNullOrEmpty(path)) {
        // Assume intentional cancellation; no error displayed.
      }
      else {
        if (!simManager.LoadEcosystem(path)) {
          // Failed to load.
          if (failureNotification != null) {
            failureNotification.Notify();
          }
        }
        else {
          // Successfully loaded ecosystem.
          if (successNotification != null) {
            successNotification.Notify();
          }
        }
      }
    }

  }

}
