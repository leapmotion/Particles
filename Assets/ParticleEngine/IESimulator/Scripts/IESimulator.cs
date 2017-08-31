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
  private Material _materialTemplate;

  [SerializeField]
  private StreamingFolder _loadingFolder;

  [SerializeField]
  private float _scale = 1;
  private float _prevScale = 1;

  [MinValue(0)]
  [SerializeField]
  private float _scaleFactor = 0.01f;

  [SerializeField]
  private bool logicEnabled = true;

  private List<IEParticle> _particles = new List<IEParticle>();
  private TextureSimulator.SimulationDescription _desc;

  public void LoadDescription(TextureSimulator.SimulationDescription desc) {
    _desc = desc;

    foreach (var obj in _particles) {
      DestroyImmediate(obj.gameObject);
    }
    _particles.Clear();

    var materials = _desc.speciesData.Query().Select(t => {
      var mat = Instantiate(_materialTemplate);
      mat.color = t.color;
      return mat;
    }).ToArray();

    foreach (var obj in desc.toSpawn) {
      GameObject particle = Instantiate(_particlePrefab);
      particle.transform.SetParent(transform);
      particle.transform.localPosition = obj.position;
      particle.transform.localRotation = Quaternion.identity;
      particle.GetComponent<Renderer>().sharedMaterial = materials[obj.species];
      particle.GetComponent<Rigidbody>().velocity = obj.velocity;
      particle.GetComponent<IEParticle>().species = obj.species;
      particle.SetActive(true);

      _particles.Add(particle.GetComponent<IEParticle>());
    }

    ScaleBy(_scale);
    _prevScale = _scale;
  }

  private void Update() {
    if (Input.GetKeyDown(KeyCode.F6)) {
      var path = Directory.GetFiles(_loadingFolder.Path).Query().FirstOrDefault(t => t.EndsWith(".json"));
      var desc = JsonUtility.FromJson<TextureSimulator.SimulationDescription>(File.ReadAllText(path));
      LoadDescription(desc);
    }

    if (Input.GetKey(KeyCode.UpArrow)) {
      _scale *= (1 + _scaleFactor);
    }

    if (Input.GetKey(KeyCode.DownArrow)) {
      _scale /= (1 + _scaleFactor);
    }
  }

  private void ScaleBy(float ratio) {
    foreach (var particle in _particles) {
      particle.rigidbody.position *= ratio;
      particle.transform.position = particle.rigidbody.position;

      particle.rigidbody.velocity *= ratio;
      particle.transform.localScale *= ratio;
    }
  }

  private void FixedUpdate() {
    if (_scale != _prevScale) {
      ScaleBy(_scale / _prevScale);
      _prevScale = _scale;
    }

    if (!logicEnabled) {
      return;
    }

    foreach (var particle in _particles) {
      Vector3 collisionForce = Vector3.zero;
      Vector3 socialForce = Vector3.zero;
      int socialInteractions = 0;

      foreach (var other in _particles) {
        if (other == particle) continue;

        Vector3 toOther = other.rigidbody.position - particle.rigidbody.position;
        float distance = toOther.magnitude;
        toOther = distance < 0.0001 ? Vector3.zero : toOther / distance;

        if (distance < PARTICLE_DIAMETER * _scale) {
          float penetration = 1 - distance / (PARTICLE_DIAMETER * _scale);
          float collisionScalar = (_desc.speciesData[particle.species].collisionForce + _desc.speciesData[other.species].collisionForce) * 0.5f;
          collisionForce -= toOther * penetration * collisionScalar;
        }

        if (distance < _desc.socialData[particle.species, other.species].socialRange * _scale) {
          socialForce += toOther * _desc.socialData[particle.species, other.species].socialForce;
          socialInteractions++;
        }
      }

      particle.rigidbody.velocity += _scale * collisionForce / Time.fixedDeltaTime;

      if (socialInteractions > 0) {
        particle.forceBuffer.PushFront(socialForce / socialInteractions);
      } else {
        particle.forceBuffer.PushFront(Vector3.zero);
      }

      if (particle.forceBuffer.Count > _desc.speciesData[particle.species].forceSteps) {
        particle.forceBuffer.PopBack(out socialForce);
        particle.rigidbody.velocity += _scale * socialForce / Time.fixedDeltaTime;
      }

      particle.rigidbody.velocity *= (1.0f - _desc.speciesData[particle.species].drag);
    }
  }





}
