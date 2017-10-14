using Leap.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GalaxyUIOperations {

  #region Lazy Application Bindings

  private static GalaxyRenderer s_backingGalaxyRenderer = null;
  private static GalaxyRenderer s_galaxyRenderer {
    get {
      if (s_backingGalaxyRenderer == null) {
        s_backingGalaxyRenderer = Utils.FindObjectInHierarchy<GalaxyRenderer>();
      }
      return s_backingGalaxyRenderer;
    }  
  }

  private static GalaxySimulation s_backingGalaxySimulation = null;
  private static GalaxySimulation s_galaxySimulation {
    get {
      if (s_backingGalaxySimulation == null) {
        s_backingGalaxySimulation = Utils.FindObjectInHierarchy<GalaxySimulation>();
      }
      return s_backingGalaxySimulation;
    }
  }

  private static PresetLoader s_backingPresetLoader = null;
  private static PresetLoader s_presetLoader {
    get {
      if (s_backingPresetLoader == null) {
        s_backingPresetLoader = Utils.FindObjectInHierarchy<PresetLoader>();
      }
      return s_backingPresetLoader;
    }
  }

  #endregion

  #region UI Operations

  public static void LoadRenderPreset(PresetLoader.PresetSelection preset) {
    s_presetLoader.renderMode = preset;
  }

  public static void SetStarBrightness(float normalizedValue) {
    s_galaxyRenderer.starBrightness = normalizedValue;
  }

  public static float GetStarBrightness() {
    return s_galaxyRenderer.starBrightness;
  }

  public static void ResetSimulation() {
    s_galaxySimulation.ResetSimulation();
  }

  public static void AddBlackHole() {
    s_galaxySimulation.blackHoleCount += 1;
  }

  public static void RemoveBlackHole() {
    s_galaxySimulation.blackHoleCount -= 1;
  }

  public static void SetMaxSimulationSpeed(float normalizedValue) {
    s_galaxySimulation.timestepCoefficient = normalizedValue;
  }

  public static float GetMaxSimulationSpeed() {
    return s_galaxySimulation.timestepCoefficient;
  }

  #endregion

}
