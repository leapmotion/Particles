using UnityEngine;

public class TextureSimulatorSetters : MonoBehaviour {

  [SerializeField]
  private Material _skybox;

  private TextureSimulator _sim;

  void Awake() {
    _sim = GetComponent<TextureSimulator>();
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
      case "TEST_OneParticle":
        _sim.RestartSimulation(TextureSimulator.EcosystemPreset.TEST_OneParticle);
        break;
      case "TEST_TwoParticles":
        _sim.RestartSimulation(TextureSimulator.EcosystemPreset.TEST_TwoParticles);
        break;
      case "TEST_ThreeParticles":
        _sim.RestartSimulation(TextureSimulator.EcosystemPreset.TEST_ThreeParticles);
        break;
      case "TEST_ThreeSpecies":
        _sim.RestartSimulation(TextureSimulator.EcosystemPreset.TEST_ThreeSpecies);
        break;
      default:
        Debug.LogError("No ecosystem with name " + name);
        break;
    }
  }

  public void SetSpeciesCount(float count) {
    _sim.randomEcosystemSettings.speciesCount = Mathf.RoundToInt(count);
  }

  public void SetParticleCount(float count) {
    //TODO
    //_sim.particlesToSimulate = Mathf.RoundToInt(Mathf.Lerp(1, TextureSimulator.MAX_PARTICLES, count));
  }

  public void SetMaxForce(float maxForce) {
    _sim.randomEcosystemSettings.maxSocialForce = maxForce;
  }

  public void SetMaxForceSteps(float maxForceSteps) {
    _sim.randomEcosystemSettings.maxForceSteps = Mathf.RoundToInt(maxForceSteps);
  }

  public void SetMaxRange(float maxRange) {
    _sim.randomEcosystemSettings.maxSocialRange = maxRange;
  }

  private float _dragDiff = -1;
  public void SetDrag(float drag) {
    if (_dragDiff < 0F) _dragDiff = _sim.randomEcosystemSettings.maxDrag - _sim.randomEcosystemSettings.minDrag;
    _sim.randomEcosystemSettings.minDrag = Mathf.Clamp01(drag - _dragDiff * 0.5f);
    _sim.randomEcosystemSettings.maxDrag = Mathf.Clamp01(drag + _dragDiff * 0.5f);
  }

  public float GetParticleSize() {
    return _sim.displayProperties.GetFloat("_Size");
  }

  public void SetParticleSize(float particleSize) {
    _sim.displayProperties.SetFloat("_Size", particleSize);
  }

  public float GetTrailSize() {
    return _sim.displayProperties.GetFloat("_TrailLength");
  }

  public void SetTrailSize(float trailSize) {
    _sim.displayProperties.SetFloat("_TrailLength", trailSize);
  }

  public void SetBoundingForce(float boundingForce) {
    _sim.fieldForce = boundingForce;
  }

  public void SetBoundingRadius(float boundingRadius) {
    _sim.fieldRadius = boundingRadius;
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
    _sim.RandomizeSimulation(name);
  }

  private void setSkyColor(Color c) {
    _skybox.SetColor("_TopColor", c * 1.1f);
    _skybox.SetColor("_MiddleColor", c);
    _skybox.SetColor("_BottomColor", c * 0.9f);
  }
}
