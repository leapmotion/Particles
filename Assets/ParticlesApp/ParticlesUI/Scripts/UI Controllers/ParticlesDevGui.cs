﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.DevGui;
using Leap.Unity.Attributes;

public class ParticlesDevGui : MonoBehaviour {


  public SimulatorSetters setters;
  public SimulationManager simManager;
  public GeneratorManager genManager;
  public ResetToDefaultsController resetController;

  private int _timeToReset = 0;

  [DevCategory("Basic Controls")]
  [DevValue]
  [DevRange(1, 16)]
  private int numberOfSpecies {
    get {
      return genManager.speciesCount;
    }
    set {
      if (value < 1) {
        value = 1;
      }

      if (value > 31) {
        value = 31;
      }

      genManager.speciesCount = value;
      ensureReset();
      SimulatorSliderControl.RefreshAllSliders();
    }
  }

  [DevValue]
  private bool fastForward {
    get {
      return simManager.simulationTimescale != 1.0f;
    }
    set {
      if (value) {
        simManager.simulationTimescale = 3.0f;
      } else {
        simManager.simulationTimescale = 1.0f;
      }
      SimulatorSliderControl.RefreshAllSliders();
    }
  }

  [DevCategory("Display")]
  [DevValue]
  [DevRange(0.003f, 0.02f)]
  private float particleSize {
    get {
      return simManager.particleRadius;
    }
    set {
      simManager.particleRadius = value;
      SimulatorSliderControl.RefreshAllSliders();
    }
  }

  [DevValue]
  [DevRange(0, 200)]
  private float trailLength {
    get {
      return simManager.trailSize;
    }
    set {
      simManager.trailSize = value;
      SimulatorSliderControl.RefreshAllSliders();
    }
  }

  [DevValue]
  private ColorMode colorMode {
    get {
      return simManager.colorMode;
    }
    set {
      simManager.colorMode = value;
      SimulatorSliderControl.RefreshAllSliders();
    }
  }

  [DevCategory("Generation")]
  [DevValue]
  [DevRange(0, 0.01f)]
  private float maxForce {
    get {
      return genManager.maxSocialForce;
    }
    set {
      genManager.maxSocialForce = value;
      simManager.RestartSimulation(ResetBehavior.None);
      SimulatorSliderControl.RefreshAllSliders();
    }
  }

  [DevValue]
  [DevRange(0, 1)]
  private float maxRange {
    get {
      return genManager.maxSocialRange;
    }
    set {
      genManager.maxSocialRange = value;
      simManager.RestartSimulation(ResetBehavior.None);
      SimulatorSliderControl.RefreshAllSliders();
    }
  }

  [DevValue]
  [DevRange(0, 63)]
  private int maxDelay {
    get {
      return genManager.maxForceSteps;
    }
    set {
      genManager.maxForceSteps = value;
      simManager.RestartSimulation(ResetBehavior.None);
      SimulatorSliderControl.RefreshAllSliders();
    }
  }

  [DevValue]
  [DevRange(0.05f, 0.5f)]
  private float drag {
    get {
      return genManager.dragCenter;
    }
    set {
      genManager.dragCenter = value;
      simManager.RestartSimulation(ResetBehavior.None);
      SimulatorSliderControl.RefreshAllSliders();
    }
  }

  [DevCategory("Simulation")]
  [DevValue]
  [DevRange(0, 0.1f)]
  private float boundingForce {
    get {
      return simManager.fieldForce;
    }
    set {
      simManager.fieldForce = value;
      SimulatorSliderControl.RefreshAllSliders();
    }
  }

  [DevValue]
  [DevRange(0, 4)]
  private float boundingRadius {
    get {
      return simManager.fieldRadius;
    }
    set {
      simManager.fieldRadius = value;
      SimulatorSliderControl.RefreshAllSliders();
    }
  }

  [DevValue]
  [DevRange(1, 4096)]
  private int particleCount {
    get {
      return genManager.particleCount;
    }
    set {
      genManager.particleCount = value;
      ensureReset();
      SimulatorSliderControl.RefreshAllSliders();
    }
  }

  [DevValue]
  [DevRange(0, 10)]
  private float timescale {
    get {
      return simManager.simulationTimescale;
    }
    set {
      simManager.simulationTimescale = value;
      SimulatorSliderControl.RefreshAllSliders();
    }
  }

  [DevCategory("Presets")]
  [DevValue]
  private EcosystemPreset preset {
    get {
      return (EcosystemPreset)(-100);
    }
    set {
      simManager.RestartSimulation(value, ResetBehavior.ResetPositions);
      SimulatorSliderControl.RefreshAllSliders();
    }
  }

  [DevCategory("Generation")]
  [DevButton("Reset To Defaults")]
  private void resetGenerationValues() {
    resetController.ResetToDefaults();
  }

  [DevButton("Randomize")]
  [DevCategory("Basic Controls")]
  private void randomizeEcosystem() {
    simManager.RandomizeSimulation(ResetBehavior.ResetPositions);
  }

  [DevButton("Reset")]
  private void resetEcosystem() {
    simManager.RestartSimulation(ResetBehavior.ResetPositions);
  }

  [DevButton("Randomize Colors")]
  [DevCategory("Display")]
  private void randomizeColors() {
    simManager.RandomizeSimulationColors();
  }

  private void ensureReset() {
    _timeToReset = 25;
  }

  private void Update() {
    _timeToReset--;
    if (_timeToReset == 0) {
      simManager.RestartSimulation(ResetBehavior.ResetPositions);
    }
  }
}
