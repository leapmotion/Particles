using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;
using Leap.Unity.Query;
using Leap.Unity.Attributes;

public class IESimulator : MonoBehaviour {
  public const float PARTICLE_RADIUS = 0.01f;
  public const float PARTICLE_DIAMETER = (PARTICLE_RADIUS * 2);

  [SerializeField]
  private GameObject _particlePrefab;

  [SerializeField]
  private StreamingFolder _loadingFolder;

  private List<IEParticle> _particles = new List<IEParticle>();
  private TextureSimulator.SimulationDescription _desc;
  private float _currScale = 1;

  public void LoadDescription(TextureSimulator.SimulationDescription desc) {
    _desc = desc;

    foreach (var obj in _particles) {
      DestroyImmediate(obj.gameObject);
    }
    _particles.Clear();

    foreach (var obj in desc.toSpawn) {
      GameObject particle = Instantiate(_particlePrefab);
      particle.transform.SetParent(transform);
      particle.transform.localPosition = obj.position;
      particle.transform.localRotation = Quaternion.identity;
      particle.GetComponent<Rigidbody>().velocity = obj.velocity;
      particle.GetComponent<IEParticle>().species = obj.species;
      particle.SetActive(true);

      _particles.Add(particle.GetComponent<IEParticle>());
    }

    ScaleBy(_currScale);
  }

  private void Update() {
    if (Input.GetKeyDown(KeyCode.F6)) {
      var path = Directory.GetFiles(_loadingFolder.Path).Query().FirstOrDefault(t => t.EndsWith(".json"));
      var desc = JsonUtility.FromJson<TextureSimulator.SimulationDescription>(File.ReadAllText(path));
      LoadDescription(desc);
    }
  }

  private void ScaleBy(float ratio) {
    foreach (var particle in _particles) {
      particle.transform.position *= ratio;
      particle.rigidbody.velocity *= ratio;
      particle.transform.localScale *= ratio;
    }
  }

  private void FixedUpdate() {
    foreach (var particle in _particles) {
      Vector3 collisionForce = Vector3.zero;
      Vector3 socialForce = Vector3.zero;
      int socialInteractions = 0;

      foreach (var other in _particles) {
        if (other == particle) continue;

        Vector3 toOther = other.rigidbody.position - particle.rigidbody.position;
        float distance = toOther.magnitude;
        toOther = distance < 0.0001 ? Vector3.zero : toOther / distance;

        if (distance < PARTICLE_DIAMETER * _currScale) {
          float penetration = 1 - distance / (PARTICLE_DIAMETER * _currScale);
          float collisionScalar = (_desc.speciesData[particle.species].collisionForce + _desc.speciesData[other.species].collisionForce) * 0.5f;
          collisionForce -= toOther * penetration * collisionScalar * _currScale;
        }

        if (distance < _desc.socialData[particle.species, other.species].socialRange * _currScale) {
          socialForce += toOther * _desc.socialData[particle.species, other.species].socialForce * _currScale;
          socialInteractions++;
        }
      }

      particle.rigidbody.velocity += collisionForce / Time.fixedDeltaTime;

      if (socialInteractions > 0) {
        particle.forceBuffer.PushFront(socialForce / socialInteractions);
      } else {
        particle.forceBuffer.PushFront(Vector3.zero);
      }

      if (particle.forceBuffer.Count > _desc.speciesData[particle.species].forceSteps) {
        particle.forceBuffer.PopBack(out socialForce);
        particle.rigidbody.velocity += socialForce / Time.fixedDeltaTime;
      }

      particle.rigidbody.velocity *= (1.0f - _desc.speciesData[particle.species].drag);
    }
  }





}
