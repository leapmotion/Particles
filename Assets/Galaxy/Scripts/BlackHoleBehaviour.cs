using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BlackHoleBehaviour : MonoBehaviour {

  public UnityEvent OnMove;

  public float movementThreshold;

  public float rotationThreshold;

  [NonSerialized]
  public Vector3 prevPos;

  [NonSerialized]
  public Quaternion prevRot;

  [SerializeField]
  private GalaxyRenderer _renderer;

  public Vector3 deltaPos {
    get {
      return transform.position - prevPos;
    }
  }

  public Quaternion deltaRot {
    get {
      return transform.rotation * Quaternion.Inverse(prevRot);
    }
  }

  private Matrix4x4 _prevDeltaTransform;
  public Matrix4x4 deltaTransform;

  void Update() {
    if (Vector3.Distance(transform.position, prevPos) > movementThreshold ||
        Quaternion.Angle(prevRot, transform.rotation) > rotationThreshold) {
      OnMove.Invoke();

      prevPos = transform.position;
      prevRot = transform.rotation;

      Matrix4x4 tr = _renderer.displayAnchor.worldToLocalMatrix * transform.localToWorldMatrix;
      deltaTransform = tr * _prevDeltaTransform.inverse;
      _prevDeltaTransform = tr;
    }
  }
}
