using Leap.Unity.Animation;
using Leap.Unity.Attributes;
using Leap.Unity.GraphicalRenderer;
using Leap.Unity.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Particles {

  public class SimpleAdvancedMenuSwitchButton : MonoBehaviour {

    public InteractionButton button;
    public MenuMode currentMode = MenuMode.Simple;

    [Header("Button Graphic Changes")]

    public LeapGraphic[] simpleModeButtonsGraphics;
    public LeapGraphic[] advancedModeButtonsGraphics;

    [Header("Advanced Mode Content")]
    
    [SerializeField]
    [ImplementsInterface(typeof(IPropertySwitch))]
    private MonoBehaviour _advancedModeSwitchBehaviour;
    public IPropertySwitch advancedModeSwitch {
      get {
        return _advancedModeSwitchBehaviour as IPropertySwitch;
      }
    }

    private void Reset() {
      if (button == null) button = GetComponent<InteractionButton>();
    }
    private void OnValidate() {
      if (button == null) button = GetComponent<InteractionButton>();
    }

    private void Start() {
      button.OnUnpress += SwitchMode;

      if (currentMode == MenuMode.Simple
          && advancedModeSwitch.GetIsOnOrTurningOn()) {
        advancedModeSwitch.OffNow();
      }
      else if (currentMode == MenuMode.Advanced
               && advancedModeSwitch.GetIsOffOrTurningOff()) {
        advancedModeSwitch.OnNow();
      }
    }

    private void Update() {
      var advancedModeSwitch = this.advancedModeSwitch;

      if (currentMode == MenuMode.Simple
          && advancedModeSwitch.GetIsOnOrTurningOn()) {
        advancedModeSwitch.AutoOff();
      }
      else if (currentMode == MenuMode.Advanced
               && advancedModeSwitch.GetIsOffOrTurningOff()) {
        advancedModeSwitch.AutoOn();
      }
    }

    public void SwitchMode() {
      if (currentMode == MenuMode.Advanced) {
        SetMode(MenuMode.Simple);
      }
      else {
        SetMode(MenuMode.Advanced);
      }
    }

    public void SetMode(MenuMode mode) {
      currentMode = mode;
      if (button == null) return;
      switch (mode) {
        case MenuMode.Advanced:
          disableGraphics(simpleModeButtonsGraphics);
          enableGraphics(advancedModeButtonsGraphics);
          break;
        case MenuMode.Simple:
          disableGraphics(advancedModeButtonsGraphics);
          enableGraphics(simpleModeButtonsGraphics);
          break;
      }
    }

    private void disableGraphics(LeapGraphic[] graphics) {
      if (graphics == null) return;
      for (int i = 0; i < graphics.Length; i++) {
        graphics[i].gameObject.SetActive(false);
      }
    }

    private void enableGraphics(LeapGraphic[] graphics) {
      if (graphics == null) return;
      for (int i = 0; i < graphics.Length; i++) {
        graphics[i].gameObject.SetActive(true);
      }
    }

  }

}
