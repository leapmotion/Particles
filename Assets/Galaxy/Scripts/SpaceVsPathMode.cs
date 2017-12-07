using System;
using System.Collections.Generic;
using UnityEngine;

public class SpaceVsPathMode : MonoBehaviour {

  public Renderer leftHandRenderer;
  public Renderer rightHandRenderer;
  public GalaxySimulation galaxySim;
  public GalaxyIE galaxyIE;

  [Header("Settings")]
  public Settings pathSettings;
  public Settings spaceSettings;

  public void EnterSpaceMode() {
    applySettings(spaceSettings);
    galaxyIE.canAct = false;
  }

  public void EnterPathMode() {
    applySettings(pathSettings);
    galaxyIE.canAct = true;

  }

  private void applySettings(Settings settings) {
    leftHandRenderer.sharedMaterial = settings.leftMaterial;
    rightHandRenderer.sharedMaterial = settings.rightMaterial;
    galaxySim.trailColor = settings.trailColor;
  }

  [Serializable]
  public struct Settings {
    public Material leftMaterial;
    public Material rightMaterial;
    public Color trailColor;
  }
}
