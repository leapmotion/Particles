using SFB;
using UnityEngine;
using UnityEngine.UI;

namespace Leap.Unity.Particles {

  public abstract class OverlayButton : SimulatorUIControl {

    public Button button;

    [Header("Optional")]
    public OverlayNotification successNotification;
    public OverlayNotification failureNotification;

    private ExtensionFilter[] _backingAllowedExtensions = null;
    protected ExtensionFilter[] allowedExtensions {
      get {
        if (_backingAllowedExtensions == null) {
          _backingAllowedExtensions = new ExtensionFilter[] {
            new ExtensionFilter("Ecosystem Description Files", "json")
          };
        }
        return _backingAllowedExtensions;
      }
    }

    protected override void Reset() {
      base.Reset();

      if (button == null) button = GetComponent<Button>();
    }

    protected virtual void OnEnable() {
      if (simManager == null) {
        Debug.LogError("No SimulationManager set, disabling.", this);
        this.enabled = false;
      }
    }

    protected virtual void Start() {
      button.onClick.AddListener(onClick);
    }

    private void onClick() {
      var clickResult = DoClickOperation();
      if (clickResult == EventResult.Success && successNotification != null) {
        successNotification.Notify();
      }
      if (clickResult == EventResult.Failure && failureNotification != null) {
        failureNotification.Notify();
      }
    }

    public enum EventResult { Nothing, Success, Failure }

    /// <summary>
    /// Return EventResult.Nothing to avoid firing any success or failure notifications,
    /// otherwise return the appropriate EventResult to automatically activate those 
    /// notifications if the appropriate notifications are attached.
    /// </summary>
    protected abstract EventResult DoClickOperation();

  }

}
