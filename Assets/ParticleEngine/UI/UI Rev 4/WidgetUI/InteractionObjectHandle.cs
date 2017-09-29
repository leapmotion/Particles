using Leap.Unity.Interaction;
using Leap.Unity.PhysicalInterfaces;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Leap.Unity.PhysicalInterfaces {

  public class InteractionObjectHandle : MonoBehaviour,
                                         IHandle {

    public InteractionBehaviour intObj;
    public AnchorableBehaviour anchObj;

    #region Unity Events

    void OnValidate() {
      if (intObj == null) intObj = GetComponent<InteractionBehaviour>();
      if (anchObj == null) anchObj = GetComponent<AnchorableBehaviour>();
    }

    void Start() {
      intObj.OnGraspBegin += OnPickedUp;
      intObj.OnGraspedMovement += (a, b, c, d, e) => { OnMoved(); }; // TODO: This sucks

      if (anchObj != null) {
        anchObj.OnPostTryAnchorOnGraspEnd += onGraspEnd;
      }
      else {
        intObj.OnGraspEnd += onGraspEnd;
      }
    }

    private void onGraspEnd() {
      float throwThreshold = 0.1f;
      if (intObj.rigidbody.velocity.sqrMagnitude > throwThreshold * throwThreshold) {
        OnThrown(intObj.rigidbody.velocity);
      }
      else if (anchObj != null && anchObj.preferredAnchor != null) {
        OnPlacedInContainer();
      }
      else {
        OnPlaced();
      }
    }

    #endregion

    #region IHandle

    public Pose pose {
      get { return intObj.transform.ToWorldPose(); }
    }

    public bool isHeld {
      get { return intObj.isGrasped; }
    }

    public Vector3 heldPosition {
      get { return intObj.graspingController.position; }
    }

    public event Action OnPickedUp;
    public event Action OnMoved;
    public event Action OnPlaced;
    public event Action OnPlacedInContainer;
    public event Action<Vector3> OnThrown;

    #endregion

  }

}
