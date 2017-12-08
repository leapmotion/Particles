using System;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Animation;

public class SpaceVsPathMode : MonoBehaviour, IPropertyMultiplier {

  public Renderer leftHandRenderer;
  public Renderer rightHandRenderer;
  public GalaxySimulation galaxySim;
  public GalaxyRenderer galaxyRenderer;
  public Behaviour spaceBehaviour;
  public GalaxyIE galaxyIE;

  [Header("Settings")]
  public Settings pathSettings;
  public Settings spaceSettings;

  public float multiplier { get; set; }

  private void OnEnable() {
    multiplier = 1;
    galaxyRenderer.startBrightnessMultipliers.Add(this);
  }

  private void OnDisable() {
    galaxyRenderer.startBrightnessMultipliers.Remove(this);
  }

  public void EnterSpaceMode() {
    applySettings(spaceSettings);
    galaxyIE.canAct = false;
    spaceBehaviour.enabled = true;
  }

  public void EnterPathMode() {
    applySettings(pathSettings);
    galaxyIE.canAct = true;
    spaceBehaviour.enabled = false;
  }

  public void ToggleMode() {
    if (galaxyIE.canAct) {
      EnterSpaceMode();
    } else {
      EnterPathMode();
    }
  }

  private void applySettings(Settings settings) {
    leftHandRenderer.sharedMaterial = settings.leftMaterial;
    rightHandRenderer.sharedMaterial = settings.rightMaterial;
    galaxySim.trailColor = settings.trailColor;
    multiplier = settings.starBrightness;
  }

  [Serializable]
  public struct Settings {
    public Material leftMaterial;
    public Material rightMaterial;
    public Color trailColor;
    public float starBrightness;
  }
}
