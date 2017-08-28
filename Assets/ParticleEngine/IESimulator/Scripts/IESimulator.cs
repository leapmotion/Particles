using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IESimulator : MonoBehaviour {
  public const float PARTICLE_RADIUS = 0.01f;
  public const float PARTICLE_DIAMETER = (PARTICLE_RADIUS * 2);

  [SerializeField]
  private GameObject _particlePrefab;

  private List<IEParticle> _particles;
  private TextureSimulator.SimulationDescription _desc;

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

        if (distance < PARTICLE_DIAMETER) {
          float penetration = 1 - distance / PARTICLE_DIAMETER;
          float collisionScalar = (_desc.speciesData[particle.species].collisionForce + _desc.speciesData[other.species].collisionForce) * 0.5f;
          collisionForce -= toOther * penetration * collisionScalar;
        }

        if (distance < _desc.socialData[particle.species, other.species].socialRange) {
          socialForce += toOther * _desc.socialData[particle.species, other.species].socialForce;
          socialInteractions++;
        }
      }

      particle.rigidbody.velocity += collisionForce;

      if (socialInteractions > 0) {
        particle.forceBuffer.PushFront(socialForce / socialInteractions);
      } else {
        particle.forceBuffer.PushFront(Vector3.zero);
      }

      if (particle.forceBuffer.Count > _desc.speciesData[particle.species].forceSteps) {
        particle.forceBuffer.PopBack(out socialForce);
        particle.rigidbody.velocity += socialForce;
      }
    }
  }





}
