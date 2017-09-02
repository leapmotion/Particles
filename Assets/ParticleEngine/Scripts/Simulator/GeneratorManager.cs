using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Query;
using Leap.Unity.Attributes;


public class GeneratorManager : MonoBehaviour {
  public const int MAX_PARTICLES = SimulationManager.MAX_PARTICLES;
  public const int MAX_SPECIES = SimulationManager.MAX_SPECIES;
  public const int MAX_FORCE_STEPS = SimulationManager.MAX_FORCE_STEPS;

  [Range(0, 2)]
  [SerializeField]
  private float _spawnRadius;
  public float spawnRadius {
    get { return _spawnRadius; }
    set { _spawnRadius = value; }
  }

  [Range(1, MAX_PARTICLES)]
  [SerializeField]
  private int _particleCount = MAX_PARTICLES;
  public int particleCount {
    get { return _particleCount; }
    set { _particleCount = value; }
  }

  [Range(1, MAX_SPECIES)]
  [SerializeField]
  private int _speciesCount = 10;
  public int speciesCount {
    get { return _speciesCount; }
    set { _speciesCount = value; }
  }

  [Range(1, MAX_FORCE_STEPS)]
  [SerializeField]
  private int _maxForceSteps = MAX_FORCE_STEPS;
  public int maxForceSteps {
    get { return _maxForceSteps; }
    set { _maxForceSteps = value; }
  }

  [Range(0, 0.01f)]
  [SerializeField]
  private float _maxSocialForce = 0.003f;
  public float maxSocialForce {
    get { return _maxSocialForce; }
    set { _maxSocialForce = value; }
  }

  [Range(0, 1f)]
  [SerializeField]
  private float _maxSocialRange = 0.5f;
  public float maxSocialRange {
    get { return _maxSocialRange; }
    set { _maxSocialRange = value; }
  }

  [Range(0, 1)]
  [SerializeField]
  private float _dragCenter = 0.175f;
  public float dragCenter {
    get { return _dragCenter; }
    set { _dragCenter = value; }
  }

  [Range(0, 0.5f)]
  [SerializeField]
  private float _dragSpread = 0.125f;
  public float dragSpread {
    get { return _dragSpread; }
    set { _dragSpread = value; }
  }

  [MinMax(0, 0.05f)]
  [SerializeField]
  private Vector2 _collisionForceRange = new Vector2(0.002f, 0.009f);
  public float minCollision {
    get { return _collisionForceRange.x; }
    set { _collisionForceRange.x = value; }
  }

  public float maxCollision {
    get { return _collisionForceRange.y; }
    set { _collisionForceRange.y = value; }
  }

  [MinMax(0, 1)]
  [SerializeField]
  private Vector2 _hueRange = new Vector2(0, 1);
  public float minHue {
    get { return _hueRange.x; }
    set { _hueRange.x = value; }
  }

  public float maxHue {
    get { return _hueRange.y; }
    set { _hueRange.y = value; }
  }

  [MinMax(0, 1)]
  [SerializeField]
  private Vector2 _valueRange = new Vector2(0, 1);
  public float minValue {
    get { return _valueRange.x; }
    set { _valueRange.x = value; }
  }

  public float maxValue {
    get { return _valueRange.y; }
    set { _valueRange.y = value; }
  }

  [MinMax(0, 1)]
  [SerializeField]
  private Vector2 _saturationRange = new Vector2(0, 1);
  public float minSaturation {
    get { return _saturationRange.x; }
    set { _saturationRange.x = value; }
  }

  public float maxSaturation {
    get { return _saturationRange.y; }
    set { _saturationRange.y = value; }
  }

  [Range(0, 1)]
  [SerializeField]
  private float _randomColorThreshold = 0.15f;
  public float randomColorThreshold {
    get { return _randomColorThreshold; }
    set { _randomColorThreshold = value; }
  }

  public Color[] GetRandomColors() {
    List<Color> colors = new List<Color>();
    for (int i = 0; i < MAX_SPECIES; i++) {
      Color newColor;
      int maxTries = 1000;
      while (true) {
        float h = Random.Range(minHue, maxHue);
        float s = Random.Range(minSaturation, maxSaturation);
        float v = Random.Range(minValue, maxValue);

        bool alreadyExists = false;
        foreach (var color in colors) {
          float existingH, existingS, existingV;
          Color.RGBToHSV(color, out existingH, out existingS, out existingV);

          if (Mathf.Abs(h - existingH) < randomColorThreshold &&
              Mathf.Abs(s - existingS) < randomColorThreshold) {
            alreadyExists = true;
            break;
          }
        }

        maxTries--;
        if (!alreadyExists || maxTries < 0) {
          newColor = Color.HSVToRGB(h, s, v);
          break;
        }
      }

      colors.Add(newColor);
    }
    return colors.ToArray();
  }

  public EcosystemDescription GetPresetEcosystem(EcosystemPreset preset) {
    var presetGenerator = GetComponentInChildren<PresetGenerator>();
    var description = presetGenerator.GetPresetDescription(preset);
    copyDescriptionToSliders(description);
    return description;
  }

  public EcosystemDescription GetRandomEcosystem() {
    var randomGenerator = GetComponentInChildren<RandomGenerator>();
    return randomGenerator.GetRandomEcosystemDescription();
  }

  public EcosystemDescription GetRandomEcosystem(string name) {
    var randomGenerator = GetComponentInChildren<RandomGenerator>();
    return randomGenerator.GetRandomEcosystemDescription(name);
  }

  private void copyDescriptionToSliders(EcosystemDescription desc) {
    float maxForce = 0;
    float maxRange = 0;
    float maxSteps = 1;
    float maxDrag = 0;
    for (int i = 0; i < desc.speciesData.Length; i++) {
      for (int j = 0; j < desc.speciesData.Length; j++) {
        maxForce = Mathf.Max(maxForce, desc.socialData[i, j].socialForce);
        maxRange = Mathf.Max(maxRange, desc.socialData[i, j].socialRange);
      }
      maxSteps = Mathf.Max(maxSteps, desc.speciesData[i].forceSteps + 1);
      maxDrag = Mathf.Max(maxDrag, desc.speciesData[i].drag);
    }

    maxSocialForce = maxForce;
    maxSocialRange = maxRange;
    maxForceSteps = Mathf.RoundToInt(maxSteps);
    dragCenter = maxDrag;
    particleCount = desc.toSpawn.Count;
    speciesCount = desc.toSpawn.Query().CountUnique(t => t.species);
  }

  public void ApplySliderValues(ref EcosystemDescription currentDescription, out ResetBehavior requiredReset) {
    requiredReset = ResetBehavior.None;
    if (particleCount != currentDescription.toSpawn.Count) {
      requiredReset = ResetBehavior.SmoothTransition;
    }
    if (speciesCount != currentDescription.toSpawn.Query().CountUnique(t => t.species)) {
      requiredReset = ResetBehavior.SmoothTransition;
    }

    if (currentDescription.isRandomDescription) {
      currentDescription = GetRandomEcosystem(currentDescription.name);
    } else {
      var preset = (EcosystemPreset)System.Enum.Parse(typeof(EcosystemPreset), currentDescription.name);
      currentDescription = GetPresetEcosystem(preset);

      float maxForce = float.Epsilon;
      float maxRange = float.Epsilon;
      int maxSteps = 0;
      float maxDrag = float.Epsilon;
      for (int i = 0; i < currentDescription.speciesData.Length; i++) {
        for (int j = 0; j < currentDescription.speciesData.Length; j++) {
          maxForce = Mathf.Max(maxForce, currentDescription.socialData[i, j].socialForce);
          maxRange = Mathf.Max(maxRange, currentDescription.socialData[i, j].socialRange);
        }
        maxSteps = Mathf.Max(maxSteps, currentDescription.speciesData[i].forceSteps);
        maxDrag = Mathf.Max(maxDrag, currentDescription.speciesData[i].drag);
      }

      float forceFactor = maxSocialForce / maxForce;
      float rangeFactor = maxSocialRange / maxRange;
      float dragFactor = dragCenter / maxDrag;

      for (int i = 0; i < currentDescription.speciesData.Length; i++) {
        for (int j = 0; j < currentDescription.speciesData.Length; j++) {
          currentDescription.socialData[i, j].socialForce *= forceFactor;
          currentDescription.socialData[i, j].socialRange *= rangeFactor;
        }

        float percent;
        if (maxSteps == 0) {
          percent = 1;
        } else {
          percent = Mathf.InverseLerp(0, maxSteps, currentDescription.speciesData[i].forceSteps);
        }

        int result = Mathf.FloorToInt(Mathf.Lerp(0, maxForceSteps - 1, percent));
        currentDescription.speciesData[i].forceSteps = result;
        currentDescription.speciesData[i].drag *= dragFactor;
      }
    }
  }
}
