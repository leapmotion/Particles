using UnityEngine;

public class TextureSimulatorSetters : MonoBehaviour {

  [SerializeField]
  private Material _skybox;

  private TextureSimulator _sim;

  private void Awake() {
    _sim = GetComponent<TextureSimulator>();
  }

  public void SetEcosystem(string name) {
    name = name.ToLower();
    switch (name) {
      case "red menace":
        _sim.LoadPresetEcosystem(TextureSimulator.EcosystemPreset.RedMenace);
        break;
      case "chase":
        _sim.LoadPresetEcosystem(TextureSimulator.EcosystemPreset.Chase);
        break;
      case "planets":
        _sim.LoadPresetEcosystem(TextureSimulator.EcosystemPreset.Planets);
        break;
      case "mitosis":
        _sim.LoadPresetEcosystem(TextureSimulator.EcosystemPreset.Mitosis);
        break;
      case "bodymind":
        _sim.LoadPresetEcosystem(TextureSimulator.EcosystemPreset.BodyMind);
        break;
      case "fluidy":
        _sim.LoadPresetEcosystem(TextureSimulator.EcosystemPreset.Fluidy);
        break;
      case "globules":
        _sim.LoadPresetEcosystem(TextureSimulator.EcosystemPreset.Globules);
        break;
      case "body mind":
        _sim.LoadPresetEcosystem(TextureSimulator.EcosystemPreset.BodyMind);
        break;
      default:
        Debug.LogError("No ecosystem with name " + name);
        break;
    }

    _sim.ResetPositions();
  }

  public void SetSpeciesCount(float count) {
    _sim.randomEcosystemSettings.speciesCount = Mathf.RoundToInt(count);
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

  public void SetDrag(float drag) {
    float diff = _sim.randomEcosystemSettings.maxDrag - _sim.randomEcosystemSettings.minDrag;
    _sim.randomEcosystemSettings.minDrag = Mathf.Clamp01(drag - diff * 0.5f);
    _sim.randomEcosystemSettings.maxDrag = Mathf.Clamp01(drag + diff * 0.5f);
  }

  public void SetParticleSize(float particleSize) {
    _sim.displayProperties.SetFloat("_Size", particleSize);
  }

  public void SetTrailSize(float trailSize) {
    _sim.displayProperties.SetFloat("_TrailLength", trailSize);
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
    _sim.LoadRandomEcosystem(name);
  }

  private void setSkyColor(Color c) {
    _skybox.SetColor("_TopColor", c * 1.1f);
    _skybox.SetColor("_MiddleColor", c);
    _skybox.SetColor("_BottomColor", c * 0.9f);
  }
}
