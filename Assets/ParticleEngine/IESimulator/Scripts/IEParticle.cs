using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;
using Leap.Unity.Interaction;

public class IEParticle : MonoBehaviour {
  public const int MAX_DELAY = 64;

#if UNITY_EDITOR
  new
#endif
  public Rigidbody rigidbody;
  public InteractionBehaviour interactionBehaviour;

  public int species;
  public Deque<Vector3> forceBuffer = new Deque<Vector3>(MAX_DELAY);

  private void Awake() {
    rigidbody = GetComponent<Rigidbody>();
    interactionBehaviour = GetComponent<InteractionBehaviour>();
  }
}
