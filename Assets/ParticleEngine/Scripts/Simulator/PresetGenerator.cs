using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Query;
using Leap.Unity.Attributes;

public enum EcosystemPreset {
  RedMenace,
  Chase,
  Mitosis,
  BodyMind,
  Planets,
  Globules,
  Layers,
  Fluidy,
  BlackHole,
  Nova,
  EnergyConserving,
  Capillary,
  Comets,
  Worms,
  SolarSystem,
  StringTheory,
  OrbFlow,
  DeathStar,
  Tutorial_2_Attract,
  Tutorial_2_Repel,
  Tutorial_2_Chase,
  Tutorial_3_Attract_Line,
  Tutorial_3_Attract_Loop,
  Tutorial_100_Attract,
  Tutorial_100_Repel,
  Tutorial_1000_Chase,
  Tutorial_3000_3_Chase,
  Tutorial_3000_2_Ranges
}

public class PresetGenerator : MonoBehaviour {
  public const int MAX_PARTICLES = SimulationManager.MAX_PARTICLES;
  public const int MAX_SPECIES = SimulationManager.MAX_SPECIES;
  public const int MAX_FORCE_STEPS = SimulationManager.MAX_FORCE_STEPS;

  public const int SPECIES_CAP_FOR_PRESETS = 10;

  [Range(1, MAX_FORCE_STEPS)]
  [SerializeField]
  private int _maxForceSteps = 7;
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

  [MinMax(0, 1)]
  [SerializeField]
  private Vector2 _dragRange = new Vector2(0.05f, 0.3f);
  public float minDrag {
    get { return _dragRange.x; }
    set { _dragRange.x = value; }
  }

  public float maxDrag {
    get { return _dragRange.y; }
    set { _dragRange.y = value; }
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

  public EcosystemDescription GetPresetDescription(EcosystemPreset preset) {
    GeneratorManager manager = GetComponentInParent<GeneratorManager>();

    Color[] colors = new Color[MAX_SPECIES];
    Vector4[,] socialData = new Vector4[MAX_SPECIES, MAX_SPECIES];
    Vector4[] speciesData = new Vector4[MAX_SPECIES];

    Vector3[] particlePositions = new Vector3[MAX_PARTICLES].Fill(() => Random.insideUnitSphere * manager.spawnRadius);
    Vector3[] particleVelocities = new Vector3[MAX_PARTICLES];
    int[] particleSpecies = new int[MAX_PARTICLES].Fill(-1);

    int currentSimulationSpeciesCount = SPECIES_CAP_FOR_PRESETS;
    int particlesToSimulate = MAX_PARTICLES;

    //Default colors are greyscale 0 to 1
    for (int i = 0; i < SPECIES_CAP_FOR_PRESETS; i++) {
      float p = i / (SPECIES_CAP_FOR_PRESETS - 1.0f);
      colors[i] = new Color(p, p, p, 1);
    }

    //Default social interactions are zero force with max range
    for (int i = 0; i < MAX_SPECIES; i++) {
      for (int j = 0; j < MAX_SPECIES; j++) {
        socialData[i, j] = new Vector2(0, maxSocialRange);
      }
    }

    //Default species always have max drag, 0 extra social steps, and max collision force
    for (int i = 0; i < MAX_SPECIES; i++) {
      speciesData[i] = new Vector3(minDrag, 0, maxCollision);
    }

    //---------------------------------------------
    // Red Menace
    //---------------------------------------------
    if (preset == EcosystemPreset.BlackHole) {
      colors.Fill(Color.white);
      for (int i = 0; i < MAX_SPECIES; i++) {
        for (int j = 0; j < MAX_SPECIES; j++) {
          socialData[i, j] = new Vector4(maxSocialForce * 0.3f, maxSocialRange * 10);
        }
        speciesData[i] = new Vector4(0.11f, 0, maxCollision);
      }
    } else if (preset == EcosystemPreset.RedMenace) {
      colors[0] = new Color(1.0f, 0.0f, 0.0f);
      colors[1] = new Color(0.3f, 0.2f, 0.0f);
      colors[2] = new Color(0.3f, 0.3f, 0.0f);
      colors[3] = new Color(0.0f, 0.3f, 0.0f);
      colors[4] = new Color(0.0f, 0.0f, 0.3f);
      colors[5] = new Color(0.3f, 0.0f, 0.3f);
      colors[6] = new Color(0.3f, 0.3f, 0.3f);
      colors[7] = new Color(0.3f, 0.4f, 0.3f);
      colors[8] = new Color(0.3f, 0.4f, 0.3f);
      colors[9] = new Color(0.3f, 0.2f, 0.3f);

      int redSpecies = 0;

      float normalLove = maxSocialForce * 0.04f;
      float fearOfRed = maxSocialForce * -1.0f;
      float redLoveOfOthers = maxSocialForce * 2.0f;
      float redLoveOfSelf = maxSocialForce * 0.9f;

      float normalRange = maxSocialRange * 0.4f;
      float fearRange = maxSocialRange * 0.3f;
      float loveRange = maxSocialRange * 0.3f;
      float redSelfRange = maxSocialRange * 0.4f;

      for (int s = 0; s < SPECIES_CAP_FOR_PRESETS; s++) {
        speciesData[s] = new Vector3(Mathf.Lerp(minDrag, maxDrag, 0.1f),
                                     0,
                                     Mathf.Lerp(minCollision, maxCollision, 0.3f));

        for (int o = 0; o < SPECIES_CAP_FOR_PRESETS; o++) {
          socialData[s, o] = new Vector2(normalLove, normalRange);
        }

        //------------------------------------
        // everyone fears red except for red
        // and red loves everyone
        //------------------------------------
        socialData[s, redSpecies] = new Vector2(fearOfRed, fearRange * ((float)s / (float)SPECIES_CAP_FOR_PRESETS));

        socialData[redSpecies, redSpecies] = new Vector2(redLoveOfSelf, redSelfRange);

        socialData[redSpecies, s] = new Vector2(redLoveOfOthers, loveRange);
      }
    }
    //---------------------------------------------
    // Chase
    //---------------------------------------------
    else if (preset == EcosystemPreset.Chase) {
      for (int i = 0; i < SPECIES_CAP_FOR_PRESETS; i++) {
        speciesData[i] = new Vector3(minDrag, 0, minCollision);
        socialData[i, i] = new Vector2(maxSocialForce * 0.1f, maxSocialRange);
      }

      colors[0] = new Color(0.7f, 0.0f, 0.0f);
      colors[1] = new Color(0.7f, 0.3f, 0.0f);
      colors[2] = new Color(0.7f, 0.7f, 0.0f);
      colors[3] = new Color(0.0f, 0.7f, 0.0f);
      colors[4] = new Color(0.0f, 0.0f, 0.7f);
      colors[5] = new Color(0.4f, 0.0f, 0.7f);
      colors[6] = new Color(1.0f, 0.3f, 0.3f);
      colors[7] = new Color(1.0f, 0.6f, 0.3f);
      colors[8] = new Color(1.0f, 1.0f, 0.3f);
      colors[9] = new Color(0.3f, 1.0f, 0.3f);

      float chase = 0.9f * maxSocialForce;
      socialData[0, 1] = new Vector2(chase, maxSocialRange);
      socialData[1, 2] = new Vector2(chase, maxSocialRange);
      socialData[2, 3] = new Vector2(chase, maxSocialRange);
      socialData[3, 4] = new Vector2(chase, maxSocialRange);
      socialData[4, 5] = new Vector2(chase, maxSocialRange);
      socialData[5, 6] = new Vector2(chase, maxSocialRange);
      socialData[6, 7] = new Vector2(chase, maxSocialRange);
      socialData[7, 8] = new Vector2(chase, maxSocialRange);
      socialData[8, 9] = new Vector2(chase, maxSocialRange);
      socialData[9, 0] = new Vector2(chase, maxSocialRange);

      float flee = -0.6f * maxSocialForce;
      float range = 0.8f * maxSocialRange;
      socialData[0, 9] = new Vector2(flee, range);
      socialData[1, 0] = new Vector2(flee, range);
      socialData[2, 1] = new Vector2(flee, range);
      socialData[3, 2] = new Vector2(flee, range);
      socialData[4, 3] = new Vector2(flee, range);
      socialData[5, 4] = new Vector2(flee, range);
      socialData[6, 5] = new Vector2(flee, range);
      socialData[7, 6] = new Vector2(flee, range);
      socialData[8, 7] = new Vector2(flee, range);
      socialData[9, 8] = new Vector2(flee, range);
    }

  //---------------------------------------------
  // Mitosis
  //---------------------------------------------
  else if (preset == EcosystemPreset.Mitosis) {
      float drag = 0.0f;
      float collision = 0.0f;
      float range = 0.35f;
      float initRange = 0.2f;
      float start = 0.00015f;
      float shift = -0.000065f;
      float inc = 0.0001f;

      for (int i = 0; i < SPECIES_CAP_FOR_PRESETS; i++) {
        speciesData[i] = new Vector3(Mathf.Lerp(minDrag, maxDrag, drag),
                                       0,
                                       Mathf.Lerp(minCollision, maxCollision, collision));

        float force = start + (float)(i * shift);

        for (var j = 0; j < SPECIES_CAP_FOR_PRESETS; j++) {
          force += inc;
          socialData[i, j] = new Vector2(force, maxSocialRange * range);
        }
      }

      for (int p = 0; p < particlesToSimulate; p++) {
        particleVelocities[p] = Vector3.zero;
        particlePositions[p] = Random.insideUnitSphere * initRange;
      }

      colors[9] = new Color(0.9f, 0.9f, 0.9f);
      colors[8] = new Color(0.9f, 0.7f, 0.3f);
      colors[7] = new Color(0.9f, 0.4f, 0.2f);
      colors[6] = new Color(0.9f, 0.3f, 0.3f);
      colors[5] = new Color(0.6f, 0.3f, 0.6f);
      colors[4] = new Color(0.5f, 0.3f, 0.7f);
      colors[3] = new Color(0.2f, 0.2f, 0.3f);
      colors[2] = new Color(0.1f, 0.1f, 0.3f);
      colors[1] = new Color(0.0f, 0.0f, 0.3f);
      colors[0] = new Color(0.0f, 0.0f, 0.0f);
    }


    //---------------------------------------------
    // Planets
    //---------------------------------------------
    else if (preset == EcosystemPreset.Planets) {
      currentSimulationSpeciesCount = 9;

      for (int i = 0; i < SPECIES_CAP_FOR_PRESETS; i++) {
        speciesData[i] = new Vector3(Mathf.Lerp(minDrag, maxDrag, 0.2f),
                                     3,
                                     Mathf.Lerp(minCollision, maxCollision, 0.2f));

        for (var j = 0; j < SPECIES_CAP_FOR_PRESETS; j++) {
          socialData[i, j] = new Vector2(-maxSocialForce, maxSocialRange * 0.5f);
        }
      }

      float f = maxSocialForce * 0.6f;
      float r = maxSocialRange * 0.8f;

      socialData[0, 0] = new Vector2(f, r); socialData[0, 1] = new Vector2(f, r); socialData[0, 2] = new Vector2(f, r);
      socialData[1, 1] = new Vector2(f, r); socialData[1, 0] = new Vector2(f, r); socialData[1, 2] = new Vector2(f, r);
      socialData[2, 2] = new Vector2(f, r); socialData[2, 0] = new Vector2(f, r); socialData[2, 1] = new Vector2(f, r);

      socialData[3, 3] = new Vector2(f, r); socialData[3, 4] = new Vector2(f, r); socialData[3, 5] = new Vector2(f, r);
      socialData[4, 4] = new Vector2(f, r); socialData[4, 3] = new Vector2(f, r); socialData[4, 5] = new Vector2(f, r);
      socialData[5, 5] = new Vector2(f, r); socialData[5, 3] = new Vector2(f, r); socialData[5, 4] = new Vector2(f, r);

      socialData[6, 6] = new Vector2(f, r); socialData[6, 7] = new Vector2(f, r); socialData[6, 8] = new Vector2(f, r);
      socialData[7, 7] = new Vector2(f, r); socialData[7, 8] = new Vector2(f, r); socialData[7, 6] = new Vector2(f, r);
      socialData[8, 8] = new Vector2(f, r); socialData[8, 6] = new Vector2(f, r); socialData[8, 7] = new Vector2(f, r);

      colors[0] = new Color(0.9f, 0.0f, 0.0f);
      colors[1] = new Color(0.9f, 0.5f, 0.0f);
      colors[2] = new Color(0.4f, 0.2f, 0.1f);

      colors[3] = new Color(0.8f, 0.8f, 0.1f);
      colors[4] = new Color(0.1f, 0.8f, 0.1f);
      colors[5] = new Color(0.4f, 0.3f, 0.1f);

      colors[6] = new Color(0.0f, 0.0f, 0.9f);
      colors[7] = new Color(0.4f, 0.0f, 0.9f);
      colors[8] = new Color(0.2f, 0.1f, 0.5f);
    } else if (preset == EcosystemPreset.Tutorial_2_Attract) {
      currentSimulationSpeciesCount = 2;
      particlesToSimulate = 2;

      int steps = 0;
      float drag = 0.1f;
      float collision = 0.02f;
      float force = 0.0f;
      float range = 0.5f;
      float love = 0.0005f;
      float spread = 0.4f;

      colors[0] = new Color(1.0f, 0.0f, 0.0f);
      colors[1] = new Color(1.0f, 1.0f, 0.0f);

      for (int i = 0; i < currentSimulationSpeciesCount; i++) {
        speciesData[i] = new Vector3(drag, steps, collision);
      }

      socialData[0, 0] = new Vector2(force, range);
      socialData[0, 1] = new Vector2(love, range);
      socialData[1, 1] = new Vector2(force, range);
      socialData[1, 0] = new Vector2(love, range);

      for (int p = 0; p < particlesToSimulate; p++) {
        particlePositions[p] = new Vector3(-spread * 0.5f + (float)(p * spread), 0.0f, 0.0f);
        particleVelocities[p] = Vector3.zero;
        particleSpecies[p] = p;
      }
    } else if (preset == EcosystemPreset.Tutorial_2_Repel) {
      currentSimulationSpeciesCount = 2;
      particlesToSimulate = 2;

      int steps = 0;
      float drag = 0.1f;
      float collision = 0.02f;
      float force = 0.0f;
      float range = 0.7f;
      float hate = -0.0005f;
      float spread = 0.2f;

      colors[0] = new Color(1.0f, 0.0f, 0.0f);
      colors[1] = new Color(1.0f, 1.0f, 0.0f);

      for (int i = 0; i < currentSimulationSpeciesCount; i++) {
        speciesData[i] = new Vector3(drag, steps, collision);
      }

      socialData[0, 0] = new Vector2(force, range);
      socialData[0, 1] = new Vector2(hate, range);
      socialData[1, 1] = new Vector2(force, range);
      socialData[1, 0] = new Vector2(hate, range);

      for (int p = 0; p < particlesToSimulate; p++) {
        particlePositions[p] = new Vector3(-spread * 0.5f + (float)(p * spread), 0.0f, 0.0f);
        particleVelocities[p] = Vector3.zero;
        particleSpecies[p] = p;
      }
    } else if (preset == EcosystemPreset.Tutorial_2_Chase) {
      currentSimulationSpeciesCount = 2;
      particlesToSimulate = 2;

      int steps = 0;
      float drag = 0.1f;
      float collision = 0.02f;
      float range = 0.9f;
      float love = 0.0005f;
      float hate = -0.0005f;
      float spread = 0.2f;

      colors[0] = new Color(1.0f, 0.0f, 0.0f);
      colors[1] = new Color(1.0f, 1.0f, 0.0f);

      for (int i = 0; i < currentSimulationSpeciesCount; i++) {
        speciesData[i] = new Vector3(drag, steps, collision);
      }

      socialData[0, 0] = new Vector2(0.0f, range);
      socialData[0, 1] = new Vector2(love, range);
      socialData[1, 1] = new Vector2(0.0f, range);
      socialData[1, 0] = new Vector2(hate, range);

      for (int p = 0; p < particlesToSimulate; p++) {
        particlePositions[p] = new Vector3(-spread * 0.5f + (float)(p * spread), Random.value * 0.01f, 0.0f);
        particleVelocities[p] = Vector3.zero;
        particleSpecies[p] = p;
      }
    } else if (preset == EcosystemPreset.Tutorial_3_Attract_Line) {
      currentSimulationSpeciesCount = 3;
      particlesToSimulate = 3;

      int steps = 0;
      float drag = 0.1f;
      float collision = 0.02f;
      float epsilon = 0.0001f; //Alex: this is a bandaid for a side effect

      colors[0] = new Color(1.0f, 0.0f, 0.0f);
      colors[1] = new Color(1.0f, 1.0f, 0.0f);
      colors[2] = new Color(0.0f, 0.0f, 1.0f);

      for (int s = 0; s < currentSimulationSpeciesCount; s++) {
        speciesData[s] = new Vector3(drag, steps, collision);


        for (int o = 0; o < currentSimulationSpeciesCount; o++) {
          socialData[s, o] = new Vector2(0.0f, epsilon);
        }
      }

      float nextLove = 0.001f;
      float nextRange = 0.5f;

      socialData[0, 1] = new Vector2(nextLove, nextRange);
      socialData[1, 2] = new Vector2(nextLove, nextRange);
      socialData[2, 0] = new Vector2(0.0f, nextRange);

      particlePositions[0] = new Vector3(-0.2f, -0.17f, 0.0f);
      particlePositions[1] = new Vector3(0.2f, -0.17f, 0.0f);
      particlePositions[2] = new Vector3(0.0f, 0.20f, 0.0f);

      for (int p = 0; p < particlesToSimulate; p++) {
        particleSpecies[p] = p;
        particleVelocities[p] = Vector3.zero;
      }
    } else if (preset == EcosystemPreset.Tutorial_3_Attract_Loop) {
      currentSimulationSpeciesCount = 3;
      particlesToSimulate = 3;

      int steps = 0;
      float drag = 0.1f;
      float collision = 0.02f;
      float epsilon = 0.0001f; //Alex: this is a bandaid for a side effect

      colors[0] = new Color(1.0f, 0.0f, 0.0f);
      colors[1] = new Color(1.0f, 1.0f, 0.0f);
      colors[2] = new Color(0.0f, 0.0f, 1.0f);

      for (int s = 0; s < currentSimulationSpeciesCount; s++) {
        speciesData[s] = new Vector3(drag, steps, collision);


        for (int o = 0; o < currentSimulationSpeciesCount; o++) {
          socialData[s, o] = new Vector2(0.0f, epsilon);
        }
      }

      float nextLove = 0.001f;
      float nextRange = 0.5f;

      socialData[0, 1] = new Vector2(nextLove, nextRange);
      socialData[1, 2] = new Vector2(nextLove, nextRange);
      socialData[2, 0] = new Vector2(nextLove, nextRange);

      particlePositions[0] = new Vector3(-0.2f, -0.17f, 0.0f);
      particlePositions[1] = new Vector3(0.2f, -0.17f, 0.0f);
      particlePositions[2] = new Vector3(0.0f, 0.20f, 0.0f);

      for (int p = 0; p < particlesToSimulate; p++) {
        particleSpecies[p] = p;
        particleVelocities[p] = Vector3.zero;
      }
    } else if (preset == EcosystemPreset.Tutorial_100_Attract) {
      currentSimulationSpeciesCount = 1;
      particlesToSimulate = 100;

      int steps = 0;
      float drag = 0.1f;
      float collision = 0.02f;
      float force = 0.001f;
      float range = 1.0f;
      float startRange = 1.0f;

      colors[0] = new Color(0.6f, 0.5f, 0.4f);

      for (int s = 0; s < currentSimulationSpeciesCount; s++) {
        speciesData[s] = new Vector3(drag, steps, collision);

        for (int o = 0; o < currentSimulationSpeciesCount; o++) {
          socialData[s, o] = new Vector2(force, range);
        }
      }

      for (int p = 0; p < particlesToSimulate; p++) {
        float x = -startRange * 0.5f + Random.value * startRange;
        float y = -startRange * 0.5f + Random.value * startRange;
        float z = -startRange * 0.5f + Random.value * startRange;
        particlePositions[p] = new Vector3(x, y, z);
        particleSpecies[p] = 0;
        particleVelocities[p] = Vector3.zero;
      }
    } else if (preset == EcosystemPreset.Tutorial_100_Repel) {
      currentSimulationSpeciesCount = 1;
      particlesToSimulate = 100;

      int steps = 0;
      float drag = 0.1f;
      float collision = 0.02f;
      float force = -0.001f;
      float range = 1.0f;
      float startRange = 0.3f;

      colors[0] = new Color(0.6f, 0.5f, 0.4f);

      for (int s = 0; s < currentSimulationSpeciesCount; s++) {
        speciesData[s] = new Vector3(drag, steps, collision);

        for (int o = 0; o < currentSimulationSpeciesCount; o++) {
          socialData[s, o] = new Vector2(force, range);
        }
      }

      for (int p = 0; p < particlesToSimulate; p++) {
        float x = -startRange * 0.5f + Random.value * startRange;
        float y = -startRange * 0.5f + Random.value * startRange;
        float z = -startRange * 0.5f + Random.value * startRange;
        particlePositions[p] = new Vector3(x, y, z);
        particleSpecies[p] = 0;
        particleVelocities[p] = Vector3.zero;
      }
    } else if (preset == EcosystemPreset.Tutorial_1000_Chase) {
      currentSimulationSpeciesCount = 2;
      particlesToSimulate = 1000;

      int steps = 0;
      float drag = 0.1f;
      float collision = 0.02f;
      float love = 0.001f;
      float hate = -0.001f;
      float loveRange = 0.2f;
      float hateRange = 0.8f;
      float startRange = 0.8f;

      colors[0] = new Color(1.0f, 0.0f, 0.0f);
      colors[1] = new Color(1.0f, 1.0f, 0.0f);

      speciesData[0] = new Vector3(drag, steps, collision);
      speciesData[1] = new Vector3(drag, steps, collision);

      socialData[0, 0] = new Vector2(0.0f, 0.0f);
      socialData[1, 1] = new Vector2(0.0f, 0.0f);
      socialData[0, 1] = new Vector2(love, loveRange);
      socialData[1, 0] = new Vector2(hate, hateRange);

      for (int p = 0; p < particlesToSimulate; p++) {
        float x = -startRange * 0.5f + Random.value * startRange;
        float y = -startRange * 0.5f + Random.value * startRange;
        float z = -startRange * 0.5f + Random.value * startRange;
        particlePositions[p] = new Vector3(x, y, z);
        particleSpecies[p] = p % currentSimulationSpeciesCount;
        particleVelocities[p] = Vector3.zero;
      }
    }



        //----------------------------------------------------------------
        // This is a controlled test scenario which is the same as
        // Test3 in terms of species, but it has lots of particles
        //----------------------------------------------------------------
        else if (preset == EcosystemPreset.Tutorial_3000_3_Chase) {
      currentSimulationSpeciesCount = 3;
      particlesToSimulate = 3000;

      colors[0] = new Color(0.9f, 0.0f, 0.0f);
      colors[1] = new Color(0.9f, 0.9f, 0.0f);
      colors[2] = new Color(0.0f, 0.0f, 0.9f);

      for (int s = 0; s < currentSimulationSpeciesCount; s++) {
        speciesData[s] = new Vector3(0.1f, 1, 0.01f);
      }

      float Test4_selfForce = 0.002f;
      float Test4_selfRange = 0.5f;

      float Test4_loveForce = 0.002f;
      float Test4_loveRange = 0.5f;

      float Test4_hateForce = -0.004f;
      float Test4_hateRange = 0.5f;

      socialData[0, 0] = new Vector2(Test4_selfForce, Test4_selfRange);
      socialData[1, 1] = new Vector2(Test4_selfForce, Test4_selfRange);
      socialData[2, 2] = new Vector2(Test4_selfForce, Test4_selfRange);

      socialData[0, 1] = new Vector2(Test4_loveForce, Test4_loveRange);
      socialData[1, 2] = new Vector2(Test4_loveForce, Test4_loveRange);
      socialData[2, 0] = new Vector2(Test4_loveForce, Test4_loveRange);

      socialData[0, 2] = new Vector2(Test4_hateForce, Test4_hateRange);
      socialData[1, 0] = new Vector2(Test4_hateForce, Test4_hateRange);
      socialData[2, 1] = new Vector2(Test4_hateForce, Test4_hateRange);

      for (int p = 0; p < particlesToSimulate; p++) {
        float fraction = (float)p / (float)particlesToSimulate;
        if (fraction < (1.0f / currentSimulationSpeciesCount)) {
          particleSpecies[p] = 0;
        } else if (fraction < (2.0f / currentSimulationSpeciesCount)) {
          particleSpecies[p] = 1;
        } else {
          particleSpecies[p] = 2;
        }

        particleVelocities[p] = Vector3.zero;
      }
    }

        //--------------------------------------------------------
        // ranges
        //--------------------------------------------------------
        else if (preset == EcosystemPreset.Tutorial_3000_2_Ranges) {
      currentSimulationSpeciesCount = 2;

      colors[0] = new Color(1.0f, 0.4f, 0.3f);
      colors[1] = new Color(0.2f, 0.4f, 1.0f);

      speciesData[0] = new Vector3(0.01f, 0, 0.01f);
      speciesData[1] = new Vector3(0.01f, 0, 0.01f);


      // this is for testing variations...
      /*
          float forceMax = 0.001f;
          float rangeMax = 0.5f;

          int res = 7;
          float force_0_0 = -forceMax + ( (int)( Random.value * res ) / (float)(res-1) ) * forceMax * 2.0f;
          float force_0_1 = -forceMax + ( (int)( Random.value * res ) / (float)(res-1) ) * forceMax * 2.0f;
          float force_1_0 = -forceMax + ( (int)( Random.value * res ) / (float)(res-1) ) * forceMax * 2.0f;
          float force_1_1 = -forceMax + ( (int)( Random.value * res ) / (float)(res-1) ) * forceMax * 2.0f;

          float range_0_0 = ( (int)( Random.value * res ) / (float)(res-1) ) * rangeMax * 2.0f;
          float range_0_1 = ( (int)( Random.value * res ) / (float)(res-1) ) * rangeMax * 2.0f;
          float range_1_0 = ( (int)( Random.value * res ) / (float)(res-1) ) * rangeMax * 2.0f;
          float range_1_1 = ( (int)( Random.value * res ) / (float)(res-1) ) * rangeMax * 2.0f;

          Debug.Log( "force_0_0 = " + force_0_0 );
          Debug.Log( "force_0_1 = " + force_0_1 );
          Debug.Log( "force_1_0 = " + force_1_0 );
          Debug.Log( "force_1_1 = " + force_1_1 );

          Debug.Log( "range_0_0 = " + range_0_0 );
          Debug.Log( "range_0_1 = " + range_0_1 );
          Debug.Log( "range_1_0 = " + range_1_0 );
          Debug.Log( "range_1_1 = " + range_1_1 );
      */

      float force_0_0 = 0.001f;
      float force_1_1 = -0.002f;

      float range_0_0 = 0.5f;
      float range_1_1 = 0.5f;

      float force_0_1 = -0.0002f;
      float force_1_0 = 0.001f;

      float range_0_1 = 0.5f;
      float range_1_0 = 0.9f;

      socialData[0, 0] = new Vector2(force_0_0, range_0_0);
      socialData[1, 1] = new Vector2(force_1_1, range_1_1);
      socialData[0, 1] = new Vector2(force_0_1, range_0_1);
      socialData[1, 0] = new Vector2(force_1_0, range_1_0);

      particlesToSimulate = 4000;
      for (int p = 0; p < particlesToSimulate; p++) {
        particleSpecies[p] = p % 2;
        //particlePositions	[p] = Random.insideUnitSphere * 1.4f;
      }
    }



        //--------------------------------------------------------
        // String Theory
        //--------------------------------------------------------
        else if (preset == EcosystemPreset.StringTheory) {
      currentSimulationSpeciesCount = 2;

      colors[0] = new Color(0.9f, 0.7f, 0.5f);
      colors[1] = new Color(0.5f, 0.2f, 0.8f);

      speciesData[0] = new Vector3(0.01f, 0, 0.01f);
      speciesData[1] = new Vector3(0.01f, 0, 0.01f);

      float force_0_0 = -0.001f;
      float force_0_1 = 0.0005f;
      float force_1_0 = 0.0f;
      float force_1_1 = -0.001f;

      float range_0_0 = 0.75f;
      float range_0_1 = 0.75f;
      float range_1_0 = 1.0f;
      float range_1_1 = 0.75f;

      socialData[0, 0] = new Vector2(force_0_0, range_0_0);
      socialData[1, 1] = new Vector2(force_1_1, range_1_1);
      socialData[0, 1] = new Vector2(force_0_1, range_0_1);
      socialData[1, 0] = new Vector2(force_1_0, range_1_0);

      particlesToSimulate = 4000;
      for (int p = 0; p < particlesToSimulate; p++) {
        particleSpecies[p] = p % 2;
      }
    }


	//--------------------------------------------------------
	// Death Star
	//--------------------------------------------------------
	else if (preset == EcosystemPreset.DeathStar) 
	{
		int core 						= 0;
		int coreSkin 					= 1;
		int innerSphere 				= 2;
		int middleSphere 				= 3;
		int outerSphere 				= 4;
		currentSimulationSpeciesCount 	= 5;

		colors[ core			] = new Color( 0.4f, 0.0f, 0.8f );
		colors[ coreSkin		] = new Color( 0.2f, 0.3f, 0.5f );
		colors[ innerSphere		] = new Color( 0.9f, 0.9f, 0.0f );
		colors[ middleSphere	] = new Color( 0.9f, 0.4f, 0.0f );
		colors[ outerSphere		] = new Color( 0.9f, 0.0f, 0.0f );

		int   steps 		=  0;
		float drag 			=  0.1f;
		float collision 	=  0.02f;
		
		speciesData[ core			] = new Vector3( drag, steps, collision );
		speciesData[ coreSkin		] = new Vector3( drag, steps, collision ); 
		speciesData[ innerSphere	] = new Vector3( drag, steps, collision );
		speciesData[ middleSphere	] = new Vector3( drag, steps, collision );
		speciesData[ outerSphere	] = new Vector3( drag, steps, collision );
	
		//-------------------------------------------------------
		// core loves itself
		//-------------------------------------------------------
		socialData[ core, core ] = new Vector2(  0.08f, 2.0f );

		//-------------------------------------------------------
		// coreSkin loves core and hates itself
		//-------------------------------------------------------
		socialData[ coreSkin,	coreSkin	] = new Vector2( -0.02f,  0.1f );
		socialData[ coreSkin,	core		] = new Vector2(  0.005f, 2.0f );


		//----------------------------------------------------------------------------
		// spheres love coreSkin but hate core
		//----------------------------------------------------------------------------
		socialData[ innerSphere,	coreSkin		] = new Vector2(  0.02f,  0.6f );
		socialData[ middleSphere,	coreSkin		] = new Vector2(  0.02f,  0.7f );
		socialData[ outerSphere,	coreSkin		] = new Vector2(  0.02f,  0.8f );

		socialData[ innerSphere,	core			] = new Vector2( -0.04f,   0.8f );
		socialData[ middleSphere,	core			] = new Vector2( -0.04f,   0.9f );
		socialData[ outerSphere,	core			] = new Vector2( -0.04f,   1.0f );


		//----------------------------------------------
		// all spheres are repelled by thwie own kind
		//----------------------------------------------
		float repulsion = -0.01f;
		float range = 0.05f;

		socialData[ innerSphere,	innerSphere		] = new Vector2( repulsion, range );
		socialData[ middleSphere,	innerSphere		] = new Vector2( repulsion, range );
		socialData[ middleSphere,	middleSphere	] = new Vector2( repulsion, range );


		/*
		//----------------------------------------------
		// a bit of chasing to keep things interesting
		//----------------------------------------------
		float chaseForce =  0.0f;
		float chaseRange =  0.01f;
		float fleeForce  =  0.0f;
		float fleeRange  =  0.01f;

		socialData[ innerSphere,	middleSphere	] = new Vector2( chaseForce, 	chaseRange	);
		socialData[ middleSphere,	innerSphere		] = new Vector2( fleeForce, 	fleeRange 	);

		socialData[ middleSphere,	outerSphere		] = new Vector2( chaseForce, 	chaseRange 	);
		socialData[ outerSphere,	middleSphere	] = new Vector2( fleeForce, 	fleeRange 	);

		socialData[ outerSphere,	innerSphere		] = new Vector2( chaseForce, 	chaseRange 	);
		socialData[ innerSphere,	outerSphere		] = new Vector2( fleeForce, 	fleeRange 	);
		*/

		particlesToSimulate = 4000;
		for (int p = 0; p < particlesToSimulate; p++) 
		{
			if 		( p <  100 	) { particleSpecies[p] = core; 			}
			else if ( p < 1000 	) { particleSpecies[p] = coreSkin;		}
			else if ( p < 2000 	) { particleSpecies[p] = innerSphere;	}	
			else if ( p < 3000 	) { particleSpecies[p] = middleSphere;	}
			else if ( p < 4000 	) { particleSpecies[p] = outerSphere;	}	
		}
	}


    //--------------------------------------------------------
    // Orb Flow
    //--------------------------------------------------------
    else if (preset == EcosystemPreset.OrbFlow) {
      currentSimulationSpeciesCount = 2;

      colors[0] = new Color(0.0f, 0.5f, 1.0f);
      colors[1] = new Color(0.5f, 0.0f, 0.8f);

      speciesData[0] = new Vector3(0.01f, 0, 0.01f);
      speciesData[1] = new Vector3(0.01f, 0, 0.01f);

      float force_0_0 = 0.001f;
      float force_0_1 = -0.00033333f;
      float force_1_0 = 0.00066666f;
      float force_1_1 = -0.001f;

      float range_0_0 = 0.66666f;
      float range_0_1 = 0.83333f;
      float range_1_0 = 0.83333f;
      float range_1_1 = 0.16666f;


      socialData[0, 0] = new Vector2(force_0_0, range_0_0);
      socialData[1, 1] = new Vector2(force_1_1, range_1_1);
      socialData[0, 1] = new Vector2(force_0_1, range_0_1);
      socialData[1, 0] = new Vector2(force_1_0, range_1_0);

      particlesToSimulate = 4000;
      for (int p = 0; p < particlesToSimulate; p++) {
        particleSpecies[p] = p % 2;
      }
    }


        //--------------------------------------------------------
        // Orbit
        //--------------------------------------------------------
        else if (preset == EcosystemPreset.SolarSystem) {
      int sun = 0;
      int earth = 1;
      int moon = 2;
      int venus = 3;
      int mars = 4;
      currentSimulationSpeciesCount = 5;

      colors[sun] = new Color(1.0f, 1.0f, 0.3f);
      colors[earth] = new Color(0.2f, 0.5f, 0.9f);
      colors[moon] = new Color(0.4f, 0.7f, 0.3f);
      colors[venus] = new Color(0.6f, 0.4f, 0.7f);
      colors[mars] = new Color(0.8f, 0.4f, 0.4f);

      float startRadius = 1.0f;
      float drag = 0.01f;
      float steps = 0;
      float collision = 0.01f;
      float sunFear = -0.05f;
      float selfLove = 0.001f;
      float chaseLove = 0.0005f;
      float chaseFear = -0.0004f;
      float otherAvoid = -0.0004f;
      float otherRange = 0.3f;
      float sunFearRange = 1.0f;
      float selfLoveRange = 0.5f;
      float chaseRange = 1.0f;
      float sunSelfLove = 0.0005f;
      float sunSelfRange = 1.0f;
      float spin = 0.8f;

      for (int s = 0; s < currentSimulationSpeciesCount; s++) {
        speciesData[s] = new Vector3(drag, steps, collision);
      }

      socialData[sun, sun] = new Vector2(sunSelfLove, sunSelfRange);

      socialData[earth, earth] = new Vector2(selfLove, selfLoveRange);
      socialData[earth, sun] = new Vector2(sunFear, sunFearRange);
      socialData[earth, moon] = new Vector2(chaseLove, chaseRange);

      socialData[moon, moon] = new Vector2(selfLove, selfLoveRange);
      socialData[moon, sun] = new Vector2(sunFear, sunFearRange);
      socialData[moon, earth] = new Vector2(chaseFear, chaseRange);

      socialData[venus, venus] = new Vector2(selfLove, selfLoveRange);
      socialData[venus, sun] = new Vector2(sunFear, sunFearRange);
      socialData[venus, mars] = new Vector2(chaseLove, chaseRange);

      socialData[mars, mars] = new Vector2(selfLove, selfLoveRange);
      socialData[mars, sun] = new Vector2(sunFear, sunFearRange);
      socialData[mars, venus] = new Vector2(chaseFear, chaseRange);



      socialData[mars, earth] = new Vector2(otherAvoid, otherRange);
      socialData[mars, moon] = new Vector2(otherAvoid, otherRange);

      socialData[venus, earth] = new Vector2(otherAvoid, otherRange);
      socialData[venus, moon] = new Vector2(otherAvoid, otherRange);

      socialData[earth, mars] = new Vector2(otherAvoid, otherRange);
      socialData[earth, venus] = new Vector2(otherAvoid, otherRange);

      socialData[moon, mars] = new Vector2(otherAvoid, otherRange);
      socialData[moon, venus] = new Vector2(otherAvoid, otherRange);

      particlesToSimulate = 3000;

      for (int p = 0; p < 1000; p++) {
        particlePositions[p] = Random.insideUnitSphere * startRadius;
        particleSpecies[p] = sun;
        particleVelocities[p] = new Vector3(particlePositions[p].y * spin, particlePositions[p].x * -spin, 0.0f);
      }

      for (int p = 1000; p < 1500; p++) {
        particlePositions[p] = Random.insideUnitSphere * startRadius;
        particleSpecies[p] = earth;
        particleVelocities[p] = new Vector3(particlePositions[p].y * spin, particlePositions[p].x * -spin, 0.0f);
      }

      for (int p = 1500; p < 2000; p++) {
        particlePositions[p] = Random.insideUnitSphere * startRadius;
        particleSpecies[p] = moon;
        particleVelocities[p] = new Vector3(particlePositions[p].y * spin, particlePositions[p].x * -spin, 0.0f);
      }

      for (int p = 2000; p < 2500; p++) {
        particlePositions[p] = Random.insideUnitSphere * startRadius;
        particleSpecies[p] = venus;
        particleVelocities[p] = new Vector3(particlePositions[p].y * spin, particlePositions[p].x * -spin, 0.0f);
      }

      for (int p = 2500; p < 3000; p++) {
        particlePositions[p] = Random.insideUnitSphere * startRadius;
        particleSpecies[p] = mars;
        particleVelocities[p] = new Vector3(particlePositions[p].y * spin, particlePositions[p].x * -spin, 0.0f);
      }
    }






        //----------------------------------------------------------------
        // This is a controlled test scenario which is the same as
        // Test3 in terms of species, but it has lots of particles
        //----------------------------------------------------------------
        else if (preset == EcosystemPreset.Comets) {
      currentSimulationSpeciesCount = 3;
      particlesToSimulate = 3000;



      //----------------------------------------------------------------
      // This code is useful for finding new ecosystems...
      //----------------------------------------------------------------
      /*
      for (int s = 0; s < currentSimulationSpeciesCount; s++) 
      {
        colors[s] = new Color( Random.value, Random.value, Random.value );

        int steps = 0;
        if ( Random.value > 0.5f )
        {
          steps = (int)( Random.value * 10.0f );
        }

        float drag 		= Random.value * maxDrag;
        float collision = Random.value * maxCollision;

        speciesData[s] = new Vector3( drag, steps, collision );

        Debug.Log( "species " + s + ": drag = " + drag + "; steps = " + steps + "; collision = " + collision );

        for (int o = 0; o < currentSimulationSpeciesCount; o++) 
        {
          float force = -maxSocialForce + Random.value * maxSocialForce * 2.0f;
          float range = Random.value * 0.5f ;

          socialData[ s, o ] = new Vector2( force, range );	
          Debug.Log( "other species: " + o + ": force = " + force + "; range = " + range );
        }
      }	
      */


      colors[0] = new Color(0.8f, 0.8f, 0.2f);
      speciesData[0] = new Vector3(0.045f, 1, 0.003f);
      socialData[0, 0] = new Vector2(0.001f, 0.225f);
      socialData[0, 1] = new Vector2(-0.002f, 0.237f);
      socialData[0, 2] = new Vector2(-0.001f, 0.335f);

      colors[1] = new Color(0.6f, 0.2f, 0.0f);
      speciesData[1] = new Vector3(0.213f, 0, 0.008f);
      socialData[1, 0] = new Vector2(0.002f, 0.466f);
      socialData[1, 1] = new Vector2(-0.002f, 0.240f);
      socialData[1, 2] = new Vector2(-0.001f, 0.033f);

      colors[2] = new Color(0.3f, 0.0f, 0.6f);
      speciesData[2] = new Vector3(0.065f, 4, 0.000f);
      socialData[2, 0] = new Vector2(0.001f, 0.351f);
      socialData[2, 1] = new Vector2(0.002f, 0.274f);
      socialData[2, 2] = new Vector2(-0.001f, 0.272f);
    }
        //---------------------------------------------
        // Capillary
        //---------------------------------------------
        else if (preset == EcosystemPreset.Capillary) {
      currentSimulationSpeciesCount = 3;

      int blood = 0;
      int vesel = 1;
      int pulll = 2;

      colors[blood] = new Color(0.9f, 0.0f, 0.0f);
      colors[vesel] = new Color(0.5f, 0.4f, 0.4f);
      colors[pulll] = new Color(0.4f, 0.4f, 0.9f);

      int numBloodPartiles = 50;
      int numVeselPartiles = 100;
      int numPulllPartiles = 20;

      particlesToSimulate = numBloodPartiles + numVeselPartiles + numPulllPartiles;

      float bloodDrag = 0.01f;
      float veselDrag = 0.9f;
      float pulllDrag = 0.9f;

      float bloodCollision = 0.002f;
      float veselCollision = 0.0f;
      float veselSelfLove = 0.0f;
      float veselRange = 0.03f;
      float bloodSelfLove = 0.0001f;
      float bloodSelfRange = 0.05f;

      float bloodPullRange = 0.3f;

      float bloodFear = -0.002f;
      float bloodPullLove = 0.00003f;
      float bloodFearRange = 0.02f;
      //float pushForce 		=  0.02f;
      float capillaryWidth = 0.03f;
      float xRange = 1.6f;

      speciesData[blood] = new Vector3(bloodDrag, 0, bloodCollision);
      speciesData[vesel] = new Vector3(veselDrag, 0, veselCollision);
      speciesData[pulll] = new Vector3(pulllDrag, 0, 0.0f);

      socialData[blood, blood] = new Vector2(bloodSelfLove, bloodSelfRange);
      socialData[blood, vesel] = new Vector2(bloodFear, bloodFearRange);
      socialData[blood, pulll] = new Vector2(bloodPullLove, bloodPullRange);

      socialData[vesel, blood] = new Vector2(0.0f, 0.2f);
      socialData[vesel, vesel] = new Vector2(veselSelfLove, veselRange);
      socialData[vesel, pulll] = new Vector2(0.0f, 0.1f);

      socialData[pulll, blood] = new Vector2(-0.0001f, 0.2f);
      socialData[pulll, vesel] = new Vector2(-0.0001f, 0.2f);
      socialData[pulll, pulll] = new Vector2(0.0001f, 0.3f);

      //----------------------------------------------------
      // blood
      //----------------------------------------------------
      for (int p0 = 0; p0 < numBloodPartiles; p0++) {
        float f = (float)p0 / (float)numBloodPartiles;
        float x = -xRange * 0.5f + xRange * f;
        particlePositions[p0] = new Vector3(x, 0.0f, 0.0f);
        particleSpecies[p0] = blood;
        particleVelocities[p0] = Vector3.zero; ;
      }

      //----------------------------------------------------
      // capillary
      //----------------------------------------------------
      for (int p1 = numBloodPartiles; p1 < (numBloodPartiles + numVeselPartiles); p1++) {
        float f = (float)(p1 - numBloodPartiles) / (float)numVeselPartiles;

        float y = capillaryWidth;

        if (f >= 0.5f) {
          f -= 0.5f;
          y = -capillaryWidth;
        }

        float x = -xRange * 0.5f + xRange * f * 2.0f;

        particlePositions[p1] = new Vector3(x, y, 0.0f);
        particleVelocities[p1] = Vector3.zero;
        particleSpecies[p1] = vesel;
      }


      //----------------------------------------------------
      // pull
      //----------------------------------------------------
      for (int p2 = (numBloodPartiles + numVeselPartiles); p2 < particlesToSimulate; p2++) {
        float f = (float)(p2 - (numBloodPartiles + numVeselPartiles)) / (float)numPulllPartiles;

        float x = xRange * 0.54f;

        float y = -0.1f + xRange * f * 0.2f;

        particlePositions[p2] = new Vector3(x, y, 0.0f);
        particleVelocities[p2] = Vector3.zero;
        particleSpecies[p2] = pulll;
      }
    }



        //---------------------------------------------
        // Worms
        //---------------------------------------------
        else if (preset == EcosystemPreset.Worms) {
      currentSimulationSpeciesCount = 9;

      particlesToSimulate = 2000;

      colors[8] = new Color(0.9f, 0.9f, 0.9f);
      colors[7] = new Color(0.9f, 0.9f, 0.0f);
      colors[6] = new Color(0.8f, 0.3f, 0.0f);
      colors[5] = new Color(0.7f, 0.4f, 0.2f);
      colors[4] = new Color(0.5f, 0.3f, 0.2f);
      colors[3] = new Color(0.4f, 0.2f, 0.2f);
      colors[2] = new Color(0.3f, 0.2f, 0.2f);
      colors[1] = new Color(0.2f, 0.2f, 0.2f);
      colors[0] = new Color(0.1f, 0.1f, 0.1f);

      float drag = 0.1f;
      float steps = 0;
      float collision = 0.01f;

      for (int s = 0; s < currentSimulationSpeciesCount; s++) {
        speciesData[s] = new Vector3(drag, steps, collision);
      }

      float prevLove = 0.002f;
      float selfHate = -0.01f;
      float nextLove = 0.005f;
      float selfRange = 0.15f;
      float otherRange = 0.1f;



      for (int s = 0; s < currentSimulationSpeciesCount; s++) {
        for (int o = 0; o < currentSimulationSpeciesCount; o++) {
          socialData[s, o] = new Vector2(0.0f, otherRange);
        }

        int prev = s - 1;
        int next = s + 1;

        if (prev < 0) { prev = currentSimulationSpeciesCount - 1; }
        if (next > currentSimulationSpeciesCount - 1) { next = 0; }

        socialData[s, s] = new Vector2(selfHate, selfRange);
        socialData[s, prev] = new Vector2(prevLove, otherRange);
        socialData[s, next] = new Vector2(nextLove, otherRange);
      }


      float circleRadius = 1.0f;
      float r = 0.7f;
      for (int p = 0; p < particlesToSimulate; p++) {
        float x = -r * 0.5f + Random.value * r;
        float y = -r * 0.5f + Random.value * r;
        float z = -r * 0.5f + Random.value * r;

        particlePositions[p] = new Vector3(x, y, z);
        particleVelocities[p] = Vector3.zero;
        particleSpecies[p] = p % currentSimulationSpeciesCount;




        float fraction = (float)p / (float)particlesToSimulate;
        float radian = fraction * Mathf.PI * 2.0f;

        Vector3 right = Vector3.right * Mathf.Sin(radian);
        Vector3 up = Vector3.up * Mathf.Cos(radian);
        Vector3 front = Vector3.forward * 0.01f * Random.value;

        particlePositions[p] = circleRadius * right + circleRadius * up + front;
        particleVelocities[p] = Vector3.zero;
        particleSpecies[p] = p % currentSimulationSpeciesCount;
      }
    }



        //----------------------------------------------------------------
        // This is a controlled test scenario which is the same as
        // Test3 in terms of species, but it has lots of particles
        //----------------------------------------------------------------
        else if (preset == EcosystemPreset.Nova) {
      currentSimulationSpeciesCount = 3;

      particlesToSimulate = 4000;

      float circleRadius = 0.99f;
      float selfRange = 0.05f;
      float otherRange = 0.2f;
      float drag = 0.4f;
      float collision = 0.0f;

      colors[0] = new Color(0.6f, 0.6f, 0.3f);
      colors[1] = new Color(0.9f, 0.9f, 0.9f);
      colors[2] = new Color(0.4f, 0.2f, 0.7f);

      speciesData[0] = new Vector3(drag, 0, collision);
      speciesData[1] = new Vector3(drag, 0, collision);
      speciesData[2] = new Vector3(drag, 0, 0.1f);

      socialData[0, 0] = new Vector2(-0.0001f, selfRange);
      socialData[0, 1] = new Vector2(-0.0030f, otherRange);
      socialData[0, 2] = new Vector2(-0.0030f, otherRange);

      socialData[1, 1] = new Vector2(-0.0001f, selfRange);
      socialData[1, 0] = new Vector2(-0.0030f, otherRange);
      socialData[1, 2] = new Vector2(-0.0030f, otherRange);

      socialData[2, 2] = new Vector2(0.0000f, 0.4f);
      socialData[2, 0] = new Vector2(-0.0030f, otherRange);
      socialData[2, 1] = new Vector2(-0.0030f, otherRange);

      for (int p = 0; p < particlesToSimulate; p++) {
        float fraction = (float)p / (float)particlesToSimulate;
        float radian = fraction * Mathf.PI * 2.0f;

        Vector3 right = Vector3.right * Mathf.Sin(radian);
        Vector3 up = Vector3.up * Mathf.Cos(radian);
        Vector3 front = Vector3.forward * 0.01f * Random.value;

        particlePositions[p] = circleRadius * right + circleRadius * up + front;
        particleVelocities[p] = Vector3.zero;
        particleSpecies[p] = p % 3;
      }
    }


      //-----------------------------------------------------------------------------------------------------
      // This is a test to see what happens when opposing forces are specified between pairs of species
      //-----------------------------------------------------------------------------------------------------
      else if (preset == EcosystemPreset.EnergyConserving) {
      currentSimulationSpeciesCount = 6;

      particlesToSimulate = 3000;

      colors[0] = new Color(0.9f, 0.2f, 0.2f);
      colors[1] = new Color(0.9f, 0.5f, 0.2f);
      colors[2] = new Color(0.9f, 0.9f, 0.2f);
      colors[3] = new Color(0.2f, 0.9f, 0.2f);
      colors[4] = new Color(0.1f, 0.2f, 0.8f);
      colors[5] = new Color(0.3f, 0.2f, 0.8f);

      float drag = 0.1f;
      float steps = 0;
      float collision = 0.01f;
      float forceRange = 0.01f;
      float minRange = 0.1f;
      float maxRange = 0.9f;

      for (int s = 0; s < currentSimulationSpeciesCount; s++) {
        speciesData[s] = new Vector3(drag, steps, collision);

        for (int o = s; o < currentSimulationSpeciesCount; o++) {
          float force = -forceRange * 0.5f + forceRange * Random.value;
          float range = minRange + (maxRange - minRange) * Random.value;

          socialData[s, o] = new Vector2(force, range);
          socialData[o, s] = new Vector2(force, range);
        }
      }

      for (int p = 0; p < particlesToSimulate; p++) {
        particleVelocities[p] = Vector3.zero;
        particleSpecies[p] = p % currentSimulationSpeciesCount;
      }
    }



        //-----------------------------------------------------------------------------------
        // This is a test to see if we can simulate (somewhat and somehow) a bilayer lipid!
        //-----------------------------------------------------------------------------------
        else if (preset == EcosystemPreset.Layers) {
      currentSimulationSpeciesCount = 4;

      int s0 = 0;
      int s1 = 1;
      int s2 = 2;
      int s3 = 3;

      int rez = 30;

      particlesToSimulate = currentSimulationSpeciesCount * rez * rez;

      colors[s0] = new Color(0.3f, 0.4f, 0.6f);
      colors[s1] = new Color(0.3f, 0.2f, 0.1f);
      colors[s2] = new Color(0.7f, 0.6f, 0.5f);
      colors[s3] = new Color(0.5f, 0.4f, 0.3f);

      float drag = 0.1f;
      float collision = 0.01f;
      float epsilon = 0.0001f; //Alex: this is a bandaid for a side effect

      for (int s = 0; s < currentSimulationSpeciesCount; s++) {
        int steps = s * 2;
        speciesData[s] = new Vector3(drag, steps, collision);

        for (int o = 0; o < currentSimulationSpeciesCount; o++) {
          socialData[s, o] = new Vector2(0.0f, epsilon);
        }
      }

      float minForce = -0.002f;
      float maxForce = 0.002f;

      float minRange = 0.01f;
      float maxRange = 0.6f;


      float f_0_0 = minForce + (maxForce - minForce) * Random.value;
      float f_0_1 = minForce + (maxForce - minForce) * Random.value;
      float f_0_2 = minForce + (maxForce - minForce) * Random.value;
      float f_0_3 = minForce + (maxForce - minForce) * Random.value;

      float f_1_0 = minForce + (maxForce - minForce) * Random.value;
      float f_1_1 = minForce + (maxForce - minForce) * Random.value;
      float f_1_2 = minForce + (maxForce - minForce) * Random.value;
      float f_1_3 = minForce + (maxForce - minForce) * Random.value;

      float f_2_0 = minForce + (maxForce - minForce) * Random.value;
      float f_2_1 = minForce + (maxForce - minForce) * Random.value;
      float f_2_2 = minForce + (maxForce - minForce) * Random.value;
      float f_2_3 = minForce + (maxForce - minForce) * Random.value;

      float f_3_0 = minForce + (maxForce - minForce) * Random.value;
      float f_3_1 = minForce + (maxForce - minForce) * Random.value;
      float f_3_2 = minForce + (maxForce - minForce) * Random.value;
      float f_3_3 = minForce + (maxForce - minForce) * Random.value;



      float r_0_0 = minRange + (maxRange - minRange) * Random.value;
      float r_0_1 = minRange + (maxRange - minRange) * Random.value;
      float r_0_2 = minRange + (maxRange - minRange) * Random.value;
      float r_0_3 = minRange + (maxRange - minRange) * Random.value;

      float r_1_0 = minRange + (maxRange - minRange) * Random.value;
      float r_1_1 = minRange + (maxRange - minRange) * Random.value;
      float r_1_2 = minRange + (maxRange - minRange) * Random.value;
      float r_1_3 = minRange + (maxRange - minRange) * Random.value;

      float r_2_0 = minRange + (maxRange - minRange) * Random.value;
      float r_2_1 = minRange + (maxRange - minRange) * Random.value;
      float r_2_2 = minRange + (maxRange - minRange) * Random.value;
      float r_2_3 = minRange + (maxRange - minRange) * Random.value;

      float r_3_0 = minRange + (maxRange - minRange) * Random.value;
      float r_3_1 = minRange + (maxRange - minRange) * Random.value;
      float r_3_2 = minRange + (maxRange - minRange) * Random.value;
      float r_3_3 = minRange + (maxRange - minRange) * Random.value;

      /*
        Debug.Log("");
        Debug.Log("data  -----------------------------------------------------");

        Debug.Log("float f_0_0 = " + f_0_0 + "f; ");
        Debug.Log("float f_0_1 = " + f_0_1 + "f; ");
        Debug.Log("float f_0_2 = " + f_0_2 + "f; ");
        Debug.Log("float f_0_3 = " + f_0_3 + "f; ");

        Debug.Log("float f_1_0 = " + f_1_0 + "f; ");
        Debug.Log("float f_1_1 = " + f_1_1 + "f; ");
        Debug.Log("float f_1_2 = " + f_1_2 + "f; ");
        Debug.Log("float f_1_3 = " + f_1_3 + "f; ");

        Debug.Log("float f_2_0 = " + f_2_0 + "f; ");
        Debug.Log("float f_2_1 = " + f_2_1 + "f; ");
        Debug.Log("float f_2_2 = " + f_2_2 + "f; ");
        Debug.Log("float f_2_3 = " + f_2_3 + "f; ");

        Debug.Log("float f_3_0 = " + f_3_0 + "f; ");
        Debug.Log("float f_3_1 = " + f_3_1 + "f; ");
        Debug.Log("float f_3_2 = " + f_3_2 + "f; ");
        Debug.Log("float f_3_3 = " + f_3_3 + "f; ");


        Debug.Log("float r_0_0 = " + r_0_0 + "f; ");
        Debug.Log("float r_0_1 = " + r_0_1 + "f; ");
        Debug.Log("float r_0_2 = " + r_0_2 + "f; ");
        Debug.Log("float r_0_3 = " + r_0_3 + "f; ");

        Debug.Log("float r_1_0 = " + r_1_0 + "f; ");
        Debug.Log("float r_1_1 = " + r_1_1 + "f; ");
        Debug.Log("float r_1_2 = " + r_1_2 + "f; ");
        Debug.Log("float r_1_3 = " + r_1_3 + "f; ");

        Debug.Log("float r_2_0 = " + r_2_0 + "f; ");
        Debug.Log("float r_2_1 = " + r_2_1 + "f; ");
        Debug.Log("float r_2_2 = " + r_2_2 + "f; ");
        Debug.Log("float r_2_3 = " + r_2_3 + "f; ");

        Debug.Log("float r_3_0 = " + r_3_0 + "f; ");
        Debug.Log("float r_3_1 = " + r_3_1 + "f; ");
        Debug.Log("float r_3_2 = " + r_3_2 + "f; ");
        Debug.Log("float r_3_3 = " + r_3_3 + "f; ");


        float f_0_0 = -0.001361714f;
        float f_0_1 = -0.001863675f;
        float f_0_2 = -0.0006116494f;
        float f_0_3 = -0.0009556326f;
        float f_1_0 = -0.000519999f;
        float f_1_1 = 0.0006196692f;
        float f_1_2 = -0.0007936339f;
        float f_1_3 = -0.00107222f;
        float f_2_0 = -0.001001807f;
        float f_2_1 = 0.0007801288f;
        float f_2_2 = -0.001814131f;
        float f_2_3 = -0.0005873627f;
        float f_3_0 = 0.0005874083f;
        float f_3_1 = 0.0008533328f;
        float f_3_2 = 0.001345f;
        float f_3_3 = -0.0003365405f;


        float r_0_0 = 0.2570884f;
        float r_0_1 = 0.5648767f;
        float r_0_2 = 0.3039016f;
        float r_0_3 = 0.4649104f;
        float r_1_0 = 0.2592408f;
        float r_1_1 = 0.1084508f;
        float r_1_2 = 0.05279962f;
        float r_1_3 = 0.1394664f;
        float r_2_0 = 0.4481683f;
        float r_2_1 = 0.2992772f;
        float r_2_2 = 0.01796358f;
        float r_2_3 = 0.04451307f;
        float r_3_0 = 0.5427676f;
        float r_3_1 = 0.1953885f;
        float r_3_2 = 0.05868421f;
        float r_3_3 = 0.03309977f;
        */


      socialData[s0, s0] = new Vector2(f_0_0, r_0_0);
      socialData[s0, s1] = new Vector2(f_0_1, r_0_1);
      socialData[s0, s2] = new Vector2(f_0_2, r_0_2);
      socialData[s0, s3] = new Vector2(f_0_3, r_0_3);

      socialData[s1, s0] = new Vector2(f_1_0, r_1_0);
      socialData[s1, s1] = new Vector2(f_1_1, r_1_1);
      socialData[s1, s2] = new Vector2(f_1_2, r_1_2);
      socialData[s1, s3] = new Vector2(f_1_3, r_1_3);

      socialData[s2, s0] = new Vector2(f_2_0, r_2_0);
      socialData[s2, s1] = new Vector2(f_2_1, r_2_1);
      socialData[s2, s2] = new Vector2(f_2_2, r_2_2);
      socialData[s2, s3] = new Vector2(f_2_3, r_2_3);

      socialData[s3, s0] = new Vector2(f_3_0, r_3_0);
      socialData[s3, s1] = new Vector2(f_3_1, r_3_1);
      socialData[s3, s2] = new Vector2(f_3_2, r_3_2);
      socialData[s3, s3] = new Vector2(f_3_3, r_3_3);

      float width = 0.7f;
      float height = 0.7f;
      float depth = 0.3f;
      float jitter = 0.0001f;

      int p = 0;

      for (int i = 0; i < rez; i++) {
        float xFraction = (float)i / (float)rez;
        float x = -width * 0.5f + xFraction * width;

        for (int j = 0; j < currentSimulationSpeciesCount; j++) {
          float yFraction = (float)j / (float)currentSimulationSpeciesCount;
          float y = -depth * 0.5f + yFraction * depth;

          for (int k = 0; k < rez; k++) {
            float zFraction = (float)k / (float)rez;
            float z = -height * 0.5f + zFraction * height;

            particleSpecies[p] = j;

            particlePositions[p] = new Vector3
            (
              x + Random.value * jitter,
              y + Random.value * jitter,
              z + Random.value * jitter
            );
            p++;
          }
        }
      }
    }

      //------------------------------------------------
      // Bodymind!
      //------------------------------------------------
      else if (preset == EcosystemPreset.BodyMind) {
      currentSimulationSpeciesCount = 3;

      int blue = 0;
      int purple = 1;
      int black = 2;

      float blueDrag = 0.0f;
      float purpleDrag = 0.0f;
      float blackDrag = 0.0f;

      float blueCollision = 0.0f;
      float purpleCollision = 0.0f;
      float blackCollision = 0.0f;

      int blueSteps = 0;
      int purpleSteps = 0;
      int blackSteps = 0;

      float blueToBlueForce = 0.1f;
      float blueToBlueRange = 1.0f;

      float purpleToPurpleForce = 0.2f;
      float purpleToPurpleRange = 1.0f;

      float blueToPurpleForce = 1.0f;
      float blueToPurpleRange = 1.0f;

      float purpleToBlueForce = -1.0f;
      float purpleToBlueRange = 0.4f;

      float blackToBlueForce = 0.2f;
      float blackToBlueRange = 1.0f;

      float blackToPurpleForce = 0.2f;
      float blackToPurpleRange = 1.0f;

      colors[blue] = new Color(0.2f, 0.2f, 0.8f);
      colors[purple] = new Color(0.3f, 0.2f, 0.8f);
      colors[black] = new Color(0.1f, 0.0f, 0.4f);

      float bd = Mathf.Lerp(minDrag, maxDrag, blueDrag);
      float pd = Mathf.Lerp(minDrag, maxDrag, purpleDrag);
      float gd = Mathf.Lerp(minDrag, maxDrag, blackDrag);

      float bc = Mathf.Lerp(minCollision, maxCollision, blueCollision);
      float pc = Mathf.Lerp(minCollision, maxCollision, purpleCollision);
      float gc = Mathf.Lerp(minCollision, maxCollision, blackCollision);

      speciesData[blue] = new Vector3(bd, blueSteps, bc);
      speciesData[purple] = new Vector3(pd, purpleSteps, pc);
      speciesData[black] = new Vector3(gd, blackSteps, gc);

      socialData[blue, blue] = new Vector2(maxSocialForce * blueToBlueForce, maxSocialRange * blueToBlueRange);
      socialData[purple, purple] = new Vector2(maxSocialForce * purpleToPurpleForce, maxSocialRange * purpleToPurpleRange);
      socialData[blue, purple] = new Vector2(maxSocialForce * blueToPurpleForce, maxSocialRange * blueToPurpleRange);
      socialData[purple, blue] = new Vector2(maxSocialForce * purpleToBlueForce, maxSocialRange * purpleToBlueRange);
      socialData[black, blue] = new Vector2(maxSocialForce * blackToBlueForce, maxSocialRange * blackToBlueRange);
      socialData[black, purple] = new Vector2(maxSocialForce * blackToPurpleForce, maxSocialRange * blackToPurpleRange);
    }

        //------------------------------------------------
        // Globules
        //------------------------------------------------
        else if (preset == EcosystemPreset.Globules) {
      currentSimulationSpeciesCount = 3;

      for (int i = 0; i < SPECIES_CAP_FOR_PRESETS; i++) {
        speciesData[i] = new Vector3(Mathf.Lerp(minDrag, maxDrag, 0.2f),
                                     1,
                                     Mathf.Lerp(minCollision, maxCollision, 0.2f));

      }

      float globuleChaseForce = maxSocialForce * 0.2f;
      float globuleChaseRange = maxSocialRange * 0.8f;

      float globuleFleeForce = -maxSocialForce * 0.3f;
      float globuleFleeRange = maxSocialRange * 0.4f;

      float globuleAvoidForce = -maxSocialForce * 0.2f;
      float globuleAvoidRange = maxSocialRange * 0.1f;


      socialData[0, 1] = new Vector2(globuleChaseForce * 1.5f, globuleChaseRange);
      socialData[1, 2] = new Vector2(globuleChaseForce, globuleChaseRange);
      socialData[2, 0] = new Vector2(globuleChaseForce, globuleChaseRange);

      socialData[1, 0] = new Vector2(globuleFleeForce, globuleFleeRange);
      socialData[2, 1] = new Vector2(globuleFleeForce, globuleFleeRange);
      socialData[0, 2] = new Vector2(globuleFleeForce, globuleFleeRange);

      socialData[0, 0] = new Vector2(globuleAvoidForce, globuleAvoidRange);
      socialData[1, 1] = new Vector2(globuleAvoidForce, globuleAvoidRange);
      socialData[2, 2] = new Vector2(globuleAvoidForce, globuleAvoidRange);

      colors[0] = new Color(0.1f, 0.1f, 0.3f);
      colors[1] = new Color(0.3f, 0.2f, 0.5f);
      colors[2] = new Color(0.4f, 0.1f, 0.1f);
    }

        //------------------------------------------------
        // Fluidy
        //------------------------------------------------
        else if (preset == EcosystemPreset.Fluidy) {
      for (var i = 0; i < SPECIES_CAP_FOR_PRESETS; i++) {
        for (var j = 0; j < SPECIES_CAP_FOR_PRESETS; j++) {
          socialData[i, j] = new Vector2(0, 0);
        }

        socialData[i, i] = new Vector2(0.2f * maxSocialForce, maxSocialRange * 0.1f);
      }

      for (var i = 0; i < SPECIES_CAP_FOR_PRESETS; i++) {
        for (var j = i + 1; j < SPECIES_CAP_FOR_PRESETS; j++) {
          socialData[i, j] = new Vector2(0.15f * maxSocialForce, maxSocialRange);
          socialData[j, i] = new Vector2(-0.1f * maxSocialForce, maxSocialRange * 0.3f);
        }
      }

      for (int i = 0; i < MAX_PARTICLES; i++) {
        float percent = Mathf.InverseLerp(0, MAX_PARTICLES, i);
        float percent2 = percent * 12.123123f + Random.value;
        particlePositions[i] = new Vector3(Mathf.Lerp(-1, 1, percent2 - (int)percent2), Mathf.Lerp(-1, 1, percent), Random.Range(-0.01f, 0.01f));
        particleSpecies[i] = Mathf.FloorToInt(percent * currentSimulationSpeciesCount);
      }
    }


    EcosystemDescription description = new EcosystemDescription(isRandomDescription: false);
    description.name = preset.ToString();
    description.socialData = new SocialDescription[MAX_SPECIES, MAX_SPECIES];
    description.speciesData = new SpeciesDescription[MAX_SPECIES];
    description.toSpawn = new List<ParticleDescription>();

    for (int i = 0; i < MAX_SPECIES; i++) {
      for (int j = 0; j < MAX_SPECIES; j++) {
        float force = socialData[i, j].x;
        float range = socialData[i, j].y;

        if (range < SimulationManager.PARTICLE_DIAMETER) {
          range = SimulationManager.PARTICLE_DIAMETER;
          force = 0;
        }

        description.socialData[i, j] = new SocialDescription() {
          socialForce = force,
          socialRange = range
        };
      }

      description.speciesData[i] = new SpeciesDescription() {
        drag = speciesData[i].x,
        forceSteps = Mathf.RoundToInt(speciesData[i].y),
        collisionForce = speciesData[i].z,
        color = colors[i]
      };
    }

    for (int i = 0; i < particlesToSimulate; i++) {
      int species = particleSpecies[i];
      if (species < 0) {
        species = (i % currentSimulationSpeciesCount);
      }

      description.toSpawn.Add(new ParticleDescription() {
        position = particlePositions[i],
        velocity = particleVelocities[i],
        species = species
      });
    }

    return description;
  }
}
