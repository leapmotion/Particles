using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public struct SocialDescription {
  public float socialForce;
  public float socialRange;
}

[System.Serializable]
public struct SpeciesDescription {
  public int forceSteps;
  public float drag;
  public float collisionForce;
  public Color color;
}

[System.Serializable]
public struct ParticleDescription {
  public Vector3 position;
  public Vector3 velocity;
  public int species;
}

[System.Serializable]
public class EcosystemDescription : ISerializationCallbackReceiver {
  public string name;
  public bool isRandomDescription;
  public SocialDescription[,] socialData;
  public SpeciesDescription[] speciesData;

  [FormerlySerializedAs("toSpawn")]
  public List<ParticleDescription> particles;

  [SerializeField]
  private SocialDescription[] _serializedSocialData;

  public EcosystemDescription(bool isRandomDescription) {
    this.isRandomDescription = isRandomDescription;
  }

  public void OnBeforeSerialize() {
    if (speciesData == null) return;
    _serializedSocialData = new SocialDescription[speciesData.Length * speciesData.Length];
    for (int i = 0; i < speciesData.Length; i++) {
      for (int j = 0; j < speciesData.Length; j++) {
        _serializedSocialData[j * speciesData.Length + i] = socialData[i, j];
      }
    }
  }

  public void OnAfterDeserialize() {
    if (_serializedSocialData == null) return;
    socialData = new SocialDescription[speciesData.Length, speciesData.Length];
    for (int i = 0; i < speciesData.Length; i++) {
      for (int j = 0; j < speciesData.Length; j++) {
        socialData[i, j] = _serializedSocialData[j * speciesData.Length + i];
      }
    }
  }
}
