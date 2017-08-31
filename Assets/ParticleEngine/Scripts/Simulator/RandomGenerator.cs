using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Query;

public class RandomGenerator : MonoBehaviour {
  public const int MAX_PARTICLES = SimulationManager.MAX_PARTICLES;
  public const int MAX_SPECIES = SimulationManager.MAX_SPECIES;
  public const int MAX_FORCE_STEPS = SimulationManager.MAX_FORCE_STEPS;

  public EcosystemDescription GetRandomEcosystemDescription() {
    Random.InitState(Time.realtimeSinceStartup.GetHashCode());

    var gen = GetComponent<NameGenerator>();
    string name;
    if (gen == null) {
      name = Random.Range(0, 1000).ToString();
    } else {
      name = gen.GenerateName();
    }
    Debug.Log(name);

    return GetRandomEcosystemDescription(name);
  }

  public EcosystemDescription GetRandomEcosystemDescription(string seed) {
    var manager = GetComponentInParent<GeneratorManager>();

    EcosystemDescription desc = new EcosystemDescription(isRandomDescription: true) {
      name = seed,
      socialData = new SocialDescription[MAX_SPECIES, MAX_SPECIES],
      speciesData = new SpeciesDescription[MAX_SPECIES],
      particles = new List<ParticleDescription>()
    };

    //We first generate a bunch of 'meta seeds' which will be used for seeds for
    //each of the following steps.  We do this so that even if the length of the steps
    //change, it will not have an effect on the results of the following steps.
    Random.InitState(seed.GetHashCode());
    List<int> metaSeeds = new List<int>().FillEach(10, () => Random.Range(int.MinValue, int.MaxValue));
    int currMetaSeed = 0;

    Random.InitState(metaSeeds[currMetaSeed++]);
    for (int s = 0; s < MAX_SPECIES; s++) {
      for (int o = 0; o < MAX_SPECIES; o++) {
        desc.socialData[s, o] = new SocialDescription() {
          socialForce = Random.Range(-manager.maxSocialForce, manager.maxSocialForce),
          socialRange = Random.value * manager.maxSocialRange
        };
      }
    }

    Random.InitState(metaSeeds[currMetaSeed++]);
    for (int i = 0; i < MAX_SPECIES; i++) {
      desc.speciesData[i] = new SpeciesDescription() {
        drag = Mathf.Clamp01(Random.Range(manager.dragCenter - manager.dragSpread, manager.dragCenter + manager.dragSpread)),
        forceSteps = Mathf.FloorToInt(Random.Range(0.0f, manager.maxForceSteps)),
        collisionForce = Random.Range(manager.minCollision, manager.maxCollision)
      };
    }

    Random.InitState(metaSeeds[currMetaSeed++]);
    for (int i = 0; i < manager.particleCount; i++) {
      desc.particles.Add(new ParticleDescription() {
        position = Random.insideUnitSphere * manager.spawnRadius,
        velocity = Vector3.zero,
        species = i % manager.speciesCount
      });
    }

    Random.InitState(metaSeeds[currMetaSeed++]);
    var colors = manager.GetRandomColors();
    for (int i = 0; i < MAX_SPECIES; i++) {
      desc.speciesData[i].color = colors[i];
    }

    return desc;
  }
}
