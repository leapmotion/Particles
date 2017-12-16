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
  public SolarSystemIE solarIE;

  [Header("Settings")]
  public Settings pathSettings;
  public Settings spaceSettings;

  public float multiplier { get; set; }

  private void Start() {
    EnterPathMode();
  }

  private void OnEnable() {
    multiplier = 1;

    if (galaxyRenderer != null) {
      galaxyRenderer.startBrightnessMultipliers.Add(this);
    }
  }

  private void OnDisable() {
    if (galaxyRenderer != null) {
      galaxyRenderer.startBrightnessMultipliers.Remove(this);
    }
  }

  public void EnterSpaceMode() {
    applySettings(spaceSettings);

    if (galaxyIE != null) {
      galaxyIE.canAct = false;
    }

    if (solarIE != null) {
      solarIE.canAct = false;
    }

    spaceBehaviour.enabled = true;
  }

  public void EnterPathMode() {
    applySettings(pathSettings);

    if (galaxyIE != null) {
      galaxyIE.canAct = true;
    }

    if (solarIE != null) {
      solarIE.canAct = true;
    }

    spaceBehaviour.enabled = false;
  }

  public void ToggleMode() {
    bool curr = false;

    if (galaxyIE != null) {
      curr = galaxyIE.canAct;
    }

    if (solarIE != null) {
      curr = solarIE.canAct;
    }

    if (curr) {
      EnterSpaceMode();
    } else {
      EnterPathMode();
    }
  }

  private void applySettings(Settings settings) {
    leftHandRenderer.sharedMaterial = settings.leftMaterial;
    rightHandRenderer.sharedMaterial = settings.rightMaterial;

    if (galaxySim != null) {
      galaxySim.trailColor = settings.trailColor;
    }

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
