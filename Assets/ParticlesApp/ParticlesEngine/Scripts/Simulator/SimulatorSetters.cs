using UnityEngine;

public class SimulatorSetters : MonoBehaviour {

  [SerializeField]
  private Material _skybox;

  private SimulationManager simManager {
    get {
      return GetComponentInChildren<SimulationManager>();
    }
  }

  private GeneratorManager genManager {
    get {
      return GetComponentInChildren<GeneratorManager>();
    }
  }

  /// <summary>
  /// Throws System.ArgumentException if the argument name does not correspond to
  /// a preset ecosystem.
  /// </summary>
  public void SetEcosystemPreset(string presetName) {
    presetName = presetName.ToLower();
    switch (presetName) {
      case "red menace":
        simManager.RestartSimulation(EcosystemPreset.RedMenace);
        break;
      case "chase":
        simManager.RestartSimulation(EcosystemPreset.Chase);
        break;
      case "planets":
        simManager.RestartSimulation(EcosystemPreset.Planets);
        break;
      case "mitosis":
        simManager.RestartSimulation(EcosystemPreset.Mitosis);
        break;
      case "bodymind":
        simManager.RestartSimulation(EcosystemPreset.BodyMind);
        break;
      case "fluidy":
        simManager.RestartSimulation(EcosystemPreset.Fluidy);
        break;
      case "globules":
        simManager.RestartSimulation(EcosystemPreset.Globules);
        break;
      case "Layers":
        simManager.RestartSimulation(EcosystemPreset.Layers);
        break;
      case "body mind":
        simManager.RestartSimulation(EcosystemPreset.BodyMind);
        break;
      case "Nova":
        simManager.RestartSimulation(EcosystemPreset.Nova);
        break;
      case "EnergyConserving":
        simManager.RestartSimulation(EcosystemPreset.EnergyConserving);
        break;
      case "SolarSystem":
        simManager.RestartSimulation(EcosystemPreset.SolarSystem);
        break;
      case "Comets":
        simManager.RestartSimulation(EcosystemPreset.Comets);
        break;
      case "Capillary":
        simManager.RestartSimulation(EcosystemPreset.Capillary);
        break;
      case "Worms":
        simManager.RestartSimulation(EcosystemPreset.Worms);
        break;
      case "StringTheory":
        simManager.RestartSimulation(EcosystemPreset.StringTheory);
        break;
      case "OrbFlow":
        simManager.RestartSimulation(EcosystemPreset.OrbFlow);
        break;
      case "SemiRandom":
        simManager.RestartSimulation(EcosystemPreset.SemiRandom);
        break;
      case "Pulse":
        simManager.RestartSimulation(EcosystemPreset.Pulse);
        break;
      case "Tutorial_2_Attract":
        simManager.RestartSimulation(EcosystemPreset.Tutorial_2_Attract);
        break;
      case "Tutorial_2_Repel":
        simManager.RestartSimulation(EcosystemPreset.Tutorial_2_Repel);
        break;
      case "Tutorial_2_Chase":
        simManager.RestartSimulation(EcosystemPreset.Tutorial_2_Chase);
        break;
      case "Tutorial_3_Attract_Line":
        simManager.RestartSimulation(EcosystemPreset.Tutorial_3_Attract_Line);
        break;
      case "Tutorial_3_Attract_Loop":
        simManager.RestartSimulation(EcosystemPreset.Tutorial_3_Attract_Loop);
        break;
      case "Tutorial_100_Attract":
        simManager.RestartSimulation(EcosystemPreset.Tutorial_100_Attract);
        break;
      case "Tutorial_100_Repel":
        simManager.RestartSimulation(EcosystemPreset.Tutorial_100_Repel);
        break;
      case "Tutorial_1000_Chase":
        simManager.RestartSimulation(EcosystemPreset.Tutorial_1000_Chase);
        break;
      case "Tutorial_3000_3_Chase":
        simManager.RestartSimulation(EcosystemPreset.Tutorial_3000_3_Chase);
        break;
      case "Tutorial_3000_2_Ranges":
        simManager.RestartSimulation(EcosystemPreset.Tutorial_3000_2_Ranges);
        break;
      default:
        throw new System.ArgumentException(
          "No ecosystem with name " + presetName);
    }
  }

  public void SetEcosystemSeed(string seed) {
    simManager.RandomizeSimulation(seed, ResetBehavior.FadeInOut);
  }

  public void SetSpeciesCount(float count) {
    genManager.speciesCount = Mathf.RoundToInt(count);
  }

  public float GetSpeciesCount() {
    return genManager.speciesCount;
  }

  public void SetParticleCount(int count) {
    genManager.particleCount = count;
  }

  public int GetParticleCount() {
    return genManager.particleCount;
  }

  public void SetMaxForce(float maxForce) {
    genManager.maxSocialForce = maxForce;
  }

  public float GetMaxForce() {
    return genManager.maxSocialForce;
  }

  public void SetMaxForceSteps(float maxForceSteps) {
    genManager.maxForceSteps = Mathf.RoundToInt(maxForceSteps);
  }

  public float GetMaxForceSteps() {
    return genManager.maxForceSteps;
  }

  public void SetMaxRange(float maxRange) {
    genManager.maxSocialRange = maxRange;
  }

  public float GetMaxRange() {
    return genManager.maxSocialRange;
  }

  public void SetDrag(float drag) {
    genManager.dragCenter = drag;
  }

  public float GetDrag() {
    return genManager.dragCenter;
  }

  public void SetParticleSize(float particleSize) {
    simManager.particleRadius = particleSize;
  }

  public float GetParticleSize() {
    return simManager.particleRadius;
  }

  public void SetTrailSize(float trailSize) {
    simManager.trailSize = trailSize;
  }

  public float GetTrailSize() {
    return simManager.trailSize;
  }

  public void SetBoundingForce(float boundingForce) {
    simManager.fieldForce = boundingForce;
  }

  public float GetBoundingForce() {
    return simManager.fieldForce;
  }

  public void SetBoundingRadius(float boundingRadius) {
    simManager.fieldRadius = boundingRadius;
  }

  public float GetBoundingRadius() {
    return simManager.fieldRadius;
  }

  public void SetTimescale(float timescale) {
    simManager.simulationTimescale = timescale;
  }

  public float GetTimescale() {
    return simManager.simulationTimescale;
  }

  public void SetDisplayMode(string mode) {
    mode = mode.ToLower();
    if (mode.Contains("species")) {
      simManager.colorMode = ColorMode.BySpecies;
    } else if (mode.Contains("speed")) {
      simManager.colorMode = ColorMode.BySpeciesWithMagnitude;
    } else if (mode.Contains("direction")) {
      simManager.colorMode = ColorMode.ByVelocity;
    }
  }

  public void SetColorMode(ColorMode mode) {
    simManager.colorMode = mode;
  }

  public ColorMode GetColorMode() {
    return simManager.colorMode;
  }

  public void SetSkyRed(float red) {
    Color c = _skybox.GetColor("_MiddleColor");
    c.r = red;
    setSkyColor(c);
  }

  public void SetSkyGreen(float green) {
    Color c = _skybox.GetColor("_MiddleColor");
    c.g = green;
    setSkyColor(c);
  }

  public void SetSkyBlue(float blue) {
    Color c = _skybox.GetColor("_MiddleColor");
    c.b = blue;
    setSkyColor(c);
  }

  private void setSkyColor(Color c) {
    _skybox.SetColor("_TopColor", c * 1.1f);
    _skybox.SetColor("_MiddleColor", c);
    _skybox.SetColor("_BottomColor", c * 0.9f);
  }

  public string GetEcosystemName() {
    return simManager.currentDescription.name;
  }

  /// <summary>
  /// Checks the input string (non-case-sensitively) against a preset name, and loads
  /// the preset if it matches one, or interprets the input as a random simulation seed
  /// and loads that instead.
  /// </summary>
  public void LoadEcosystemPresetOrSeed(string presetOrSeed) {
    try {
      // First, check if the input corresponds to a preset.
      SetEcosystemPreset(presetOrSeed);
    }
    catch (System.ArgumentException) {
      // Throwing an argument exception implies no preset; assume seed instead.
      SetEcosystemSeed(presetOrSeed);
    }
  }

}
