using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;
using Leap.Unity.Query;
using Leap.Unity.Attributes;

public class IESimulator : MonoBehaviour {
  public const float PARTICLE_RADIUS = 0.01f;
  public const float PARTICLE_DIAMETER = (PARTICLE_RADIUS * 2);

  #region INSPECTOR
  [SerializeField]
  private GameObject _particlePrefab;

  [SerializeField]
  private Material _displayMat;
  #endregion

  private SimulationManager _manager;
  private List<IEParticle> _particles = new List<IEParticle>();

  private Vector3 _prevDisplayPosition;
  private Quaternion _prevDisplayRotation;
  private Vector3 _prevDisplayScale;

  #region PUBLIC API
  public EcosystemDescription currentDescription { get; private set; }

  public void RestartSimulation(EcosystemDescription description, ResetBehavior resetBehavior) {
    //TODO: remove this and implement everything else
    resetBehavior = ResetBehavior.ResetPositions;

    var block = new MaterialPropertyBlock();

    switch (resetBehavior) {
      case ResetBehavior.ResetPositions:
        foreach (var obj in _particles) {
          DestroyImmediate(obj.gameObject);
        }
        _particles.Clear();

        foreach (var obj in description.particles) {
          GameObject particle = Instantiate(_particlePrefab);
          particle.transform.SetParent(transform);
          particle.transform.localPosition = obj.position;
          particle.transform.localRotation = Quaternion.identity;
          particle.transform.localScale = Vector3.one * _manager.particleSize;

          particle.GetComponent<MeshFilter>().sharedMesh = _manager.particleMesh;

          particle.GetComponent<Renderer>().sharedMaterial = _displayMat;
          block.SetColor("_Color", description.speciesData[obj.species].color);
          particle.GetComponent<Renderer>().SetPropertyBlock(block);

          particle.GetComponent<Rigidbody>().velocity = obj.velocity;
          particle.GetComponent<IEParticle>().species = obj.species;

          particle.SetActive(true);
          _particles.Add(particle.GetComponent<IEParticle>());
        }

        TransformBy(_manager.displayAnchor.localToWorldMatrix);
        _prevDisplayPosition = _manager.displayAnchor.localPosition;
        _prevDisplayRotation = _manager.displayAnchor.localRotation;
        _prevDisplayScale = _manager.displayAnchor.localScale;

        _manager.NotifyMidTransition(SimulationMethod.InteractionEngine);
        break;
      default:
        throw new System.NotImplementedException();
    }

    currentDescription = description;
    _manager.NotifyEndedTransition(SimulationMethod.InteractionEngine);
  }
  #endregion

  #region UNITY MESSAGES

  private void Awake() {
    _manager = GetComponentInParent<SimulationManager>();
    _prevDisplayPosition = _manager.displayAnchor.localPosition;
    _prevDisplayRotation = _manager.displayAnchor.localRotation;
    _prevDisplayScale = _manager.displayAnchor.localScale;
  }

  private void FixedUpdate() {
    bool didDisplayMove = _prevDisplayPosition != _manager.displayAnchor.localPosition ||
                          _prevDisplayRotation != _manager.displayAnchor.localRotation ||
                          _prevDisplayScale != _manager.displayAnchor.localScale;
    if (didDisplayMove) {
      Matrix4x4 originalTransform = Matrix4x4.TRS(_prevDisplayPosition,
                                                  _prevDisplayRotation,
                                                  _prevDisplayScale);
      Matrix4x4 newTransform = Matrix4x4.TRS(_manager.displayAnchor.localPosition,
                                             _manager.displayAnchor.localRotation,
                                             _manager.displayAnchor.localScale);
      Matrix4x4 delta = newTransform * originalTransform.inverse;
      TransformBy(delta);

      _prevDisplayPosition = _manager.displayAnchor.localPosition;
      _prevDisplayRotation = _manager.displayAnchor.localRotation;
      _prevDisplayScale = _manager.displayAnchor.localScale;
    }

    float scale = _manager.displayAnchor.localScale.x;

    foreach (var particle in _particles) {
      Vector3 collisionForce = Vector3.zero;
      Vector3 socialForce = Vector3.zero;
      int socialInteractions = 0;

      foreach (var other in _particles) {
        if (other == particle) continue;

        Vector3 toOther = other.rigidbody.position - particle.rigidbody.position;
        float distance = toOther.magnitude;
        toOther = distance < 0.0001 ? Vector3.zero : toOther / distance;

        if (distance < PARTICLE_DIAMETER * scale) {
          float penetration = 1 - distance / (PARTICLE_DIAMETER * scale);
          float collisionScalar = (currentDescription.speciesData[particle.species].collisionForce + currentDescription.speciesData[other.species].collisionForce) * 0.5f;
          collisionForce -= toOther * penetration * collisionScalar;
        }

        if (distance < currentDescription.socialData[particle.species, other.species].socialRange * scale) {
          socialForce += toOther * currentDescription.socialData[particle.species, other.species].socialForce;
          socialInteractions++;
        }
      }

      particle.rigidbody.velocity += scale * collisionForce / Time.fixedDeltaTime;

      if (socialInteractions > 0) {
        particle.forceBuffer.PushFront(socialForce / socialInteractions);
      } else {
        particle.forceBuffer.PushFront(Vector3.zero);
      }

      if (particle.forceBuffer.Count > currentDescription.speciesData[particle.species].forceSteps) {
        particle.forceBuffer.PopBack(out socialForce);
        particle.rigidbody.velocity += scale * socialForce / Time.fixedDeltaTime;
      }

      particle.rigidbody.velocity *= (1.0f - currentDescription.speciesData[particle.species].drag);
    }
  }
  #endregion

  #region PRIVATE IMPLEMENTATION
  private void TransformBy(Matrix4x4 transform) {
    foreach (var particle in _particles) {
      particle.rigidbody.position = transform.MultiplyPoint3x4(particle.rigidbody.position);
      particle.transform.position = particle.rigidbody.position;

      particle.rigidbody.velocity = transform.MultiplyVector(particle.rigidbody.velocity);
      particle.transform.localScale *= transform.MultiplyVector(Vector3.forward).magnitude;
    }
  }
  #endregion
}
