using UnityEngine;

public class TextureSimulatorSetters : MonoBehaviour {

  [SerializeField]
  private Material _skybox;

  private TextureSimulator _backingSim;
  private TextureSimulator _sim {
    get {
      if (_backingSim == null) {
        _backingSim = GetComponent<TextureSimulator>();
      }
      return _backingSim;
    }
  }

  public void SetEcosystem(string name) {
    name = name.ToLower();
    switch (name) {
      case "red menace":
        _sim.RestartSimulation(TextureSimulator.EcosystemPreset.RedMenace);
        break;
      case "chase":
        _sim.RestartSimulation(TextureSimulator.EcosystemPreset.Chase);
        break;
      case "planets":
        _sim.RestartSimulation(TextureSimulator.EcosystemPreset.Planets);
        break;
      case "mitosis":
        _sim.RestartSimulation(TextureSimulator.EcosystemPreset.Mitosis);
        break;
      case "bodymind":
        _sim.RestartSimulation(TextureSimulator.EcosystemPreset.BodyMind);
        break;
      case "fluidy":
        _sim.RestartSimulation(TextureSimulator.EcosystemPreset.Fluidy);
        break;
      case "globules":
        _sim.RestartSimulation(TextureSimulator.EcosystemPreset.Globules);
        break;
      case "Layers":
        _sim.RestartSimulation(TextureSimulator.EcosystemPreset.Layers);
        break;
      case "body mind":
        _sim.RestartSimulation(TextureSimulator.EcosystemPreset.BodyMind);
        break;
	  case "Nova":
        _sim.RestartSimulation(TextureSimulator.EcosystemPreset.Nova);
        break;
	  case "EnergyConserving":
        _sim.RestartSimulation(TextureSimulator.EcosystemPreset.EnergyConserving);
        break;
      case "SolarSystem":
        _sim.RestartSimulation(TextureSimulator.EcosystemPreset.SolarSystem );
        break;
      case "Comets":
        _sim.RestartSimulation(TextureSimulator.EcosystemPreset.Comets);
        break;
      case "Capillary":
        _sim.RestartSimulation(TextureSimulator.EcosystemPreset.Capillary);
        break;
       case "Worms":
        _sim.RestartSimulation(TextureSimulator.EcosystemPreset.Worms);
        break;
       case "StringTheory":
        _sim.RestartSimulation(TextureSimulator.EcosystemPreset.StringTheory);
        break;
       case "OrbFlow":
        _sim.RestartSimulation(TextureSimulator.EcosystemPreset.OrbFlow);
        break;
       case "Tutorial_2_Attract":
        _sim.RestartSimulation(TextureSimulator.EcosystemPreset.Tutorial_2_Attract);
        break;
       case "Tutorial_2_Repel":
        _sim.RestartSimulation(TextureSimulator.EcosystemPreset.Tutorial_2_Repel);
        break;
       case "Tutorial_2_Chase":
        _sim.RestartSimulation(TextureSimulator.EcosystemPreset.Tutorial_2_Chase);
        break;
      case "Tutorial_3_Attract_Line":
        _sim.RestartSimulation(TextureSimulator.EcosystemPreset.Tutorial_3_Attract_Line);
        break;
      case "Tutorial_3_Attract_Loop":
        _sim.RestartSimulation(TextureSimulator.EcosystemPreset.Tutorial_3_Attract_Loop);
        break;
      case "Tutorial_100_Attract":
        _sim.RestartSimulation(TextureSimulator.EcosystemPreset.Tutorial_100_Attract );
        break;
      case "Tutorial_100_Repel":
        _sim.RestartSimulation(TextureSimulator.EcosystemPreset.Tutorial_100_Repel );
        break;
      case "Tutorial_1000_Chase":
        _sim.RestartSimulation(TextureSimulator.EcosystemPreset.Tutorial_1000_Chase );
        break;
      case "Tutorial_3000_3_Chase":
        _sim.RestartSimulation(TextureSimulator.EcosystemPreset.Tutorial_3000_3_Chase );
        break;
     case "Tutorial_3000_2_Ranges":
        _sim.RestartSimulation(TextureSimulator.EcosystemPreset.Tutorial_3000_2_Ranges );
        break;
       default:
        Debug.LogError("No ecosystem with name " + name);
        break;
    }
  }

  public void SetSpeciesCount(float count) {
    _sim.randomEcosystemSettings.speciesCount = Mathf.RoundToInt(count);
  }

  public float GetSpeciesCount() {
    return _sim.randomEcosystemSettings.speciesCount;
  }

  public void SetParticleCount(int count) {
    _sim.randomEcosystemSettings.particleCount = count;
  }

  public int GetParticleCount() {
    return _sim.randomEcosystemSettings.particleCount;
  }

  public void SetMaxForce(float maxForce) {
    _sim.randomEcosystemSettings.maxSocialForce = maxForce;
  }

  public float GetMaxForce() {
    return _sim.randomEcosystemSettings.maxSocialForce;
  }

  public void SetMaxForceSteps(float maxForceSteps) {
    _sim.randomEcosystemSettings.maxForceSteps = Mathf.RoundToInt(maxForceSteps);
  }

  public float GetMaxForceSteps() {
    return _sim.randomEcosystemSettings.maxForceSteps;
  }

  public void SetMaxRange(float maxRange) {
    _sim.randomEcosystemSettings.maxSocialRange = maxRange;
  }

  public float GetMaxRange() {
    return _sim.randomEcosystemSettings.maxSocialRange;
  }
  
  public void SetDrag(float drag) {
    _sim.randomEcosystemSettings.dragCenter = drag;
  }

  public float GetDrag() {
    return _sim.randomEcosystemSettings.dragCenter;
  }

  public void SetParticleSize(float particleSize) {
    _sim.displayProperties.SetFloat("_Size", particleSize);
  }

  public float GetParticleSize() {
    return _sim.displayProperties.GetFloat("_Size");
  }

  public void SetTrailSize(float trailSize) {
    _sim.displayProperties.SetFloat("_TrailLength", trailSize);
  }

  public float GetTrailSize() {
    return _sim.displayProperties.GetFloat("_TrailLength");
  }

  public void SetBoundingForce(float boundingForce) {
    _sim.fieldForce = boundingForce;
  }

  public float GetBoundingForce() {
    return _sim.fieldForce;
  }

  public void SetBoundingRadius(float boundingRadius) {
    _sim.fieldRadius = boundingRadius;
  }

  public float GetBoundingRadius() {
    return _sim.fieldRadius;
  }

  public void SetTimescale(float timescale) {
    _sim.simulationTimescale = timescale;
  }

  public float GetTimescale() {
    return _sim.simulationTimescale;
  }

  public void SetDisplayMode(string mode) {
    mode = mode.ToLower();
    if (mode.Contains("species")) {
      _sim.colorMode = TextureSimulator.ColorMode.BySpecies;
    } else if (mode.Contains("speed")) {
      _sim.colorMode = TextureSimulator.ColorMode.BySpeciesWithMagnitude;
    } else if (mode.Contains("direction")) {
      _sim.colorMode = TextureSimulator.ColorMode.ByVelocity;
    }
  }

  public void SetColorMode(TextureSimulator.ColorMode mode) {
    _sim.colorMode = mode;
  }

  public TextureSimulator.ColorMode GetColorMode() {
    return _sim.colorMode;
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

  public void LoadRandomEcosystem(LabelController controller) {
    var name = _sim.GetComponent<NameGenerator>().GenerateName();
    controller.SetLabel(name);
    _sim.RandomizeSimulation(name, TextureSimulator.ResetBehavior.SmoothTransition);
  }

  private void setSkyColor(Color c) {
    _skybox.SetColor("_TopColor", c * 1.1f);
    _skybox.SetColor("_MiddleColor", c);
    _skybox.SetColor("_BottomColor", c * 0.9f);
  }
}
