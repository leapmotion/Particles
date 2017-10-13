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

    void Reset() {
      if (intObj == null) intObj = GetComponent<InteractionBehaviour>();
      if (anchObj == null) anchObj = GetComponent<AnchorableBehaviour>();
    }

    void OnValidate() {
      if (intObj == null) intObj = GetComponent<InteractionBehaviour>();
      if (anchObj == null) anchObj = GetComponent<AnchorableBehaviour>();
    }

    void Start() {
      intObj.OnGraspBegin += fireOnPickedUp;

      intObj.OnGraspedMovement += (a, b, c, d, e) => { fireOnMoved(); }; // TODO: This sucks

      if (anchObj != null) {
        anchObj.OnPostTryAnchorOnGraspEnd += onGraspEnd;
        anchObj.OnAttachedToAnchor += onAttachedToAnchor;
        anchObj.OnDetachedFromAnchor += onDetachedFromAnchor;
      }
      else {
        intObj.OnGraspEnd += onGraspEnd;
      }
    }

    private void onGraspEnd() {
      float throwThreshold = 0.02f;
      if (anchObj != null && anchObj.preferredAnchor != null) {
        OnPlacedInContainer();
        OnPlacedHandleInContainer(this);
      }
      else if (intObj.rigidbody.velocity.sqrMagnitude > throwThreshold * throwThreshold) {
        OnThrown(intObj.rigidbody.velocity);
        OnThrownHandle(this, intObj.rigidbody.velocity);
      }
      else {
        OnPlaced();
        OnPlacedHandle(this);
      }
    }

    private void onAttachedToAnchor(AnchorableBehaviour anchobj, Anchor anchor) {
      OnPickedUp();
      OnPickedUpHandle(this);
    }

    private void onDetachedFromAnchor(AnchorableBehaviour anchobj, Anchor anchor) {
      if (!intObj.isGrasped) {
        OnPlaced();
      }
    }

    private void fireOnPickedUp() {
      if (anchObj.isAttached) {
        OnPlaced();
      }

      OnPickedUp();
      OnPickedUpHandle(this);
    }

    private void fireOnMoved() {
      OnMoved();
      OnMovedHandle(this);
    }

    #endregion

    #region IHandle

    public Pose pose {
      get { return intObj.transform.ToWorldPose(); }
    }

    public void SetPose(Pose pose) {
      intObj.transform.SetWorldPose(pose);
    }

    public Movement movement {
      get { return intObj.worldMovement; }
    }

    public Pose deltaPose {
      get { return intObj.worldDeltaPose; }
    }

    public bool isHeld {
      get { return intObj.isGrasped || anchObj.isAttached; }
    }

    public Vector3 heldPosition {
      get { return intObj.isGrasped ? intObj.graspingController.position
                                    : anchObj.anchor.transform.position; }
    }

    public event Action OnPickedUp;
    public event Action OnMoved;
    public event Action OnPlaced;
    public event Action OnPlacedInContainer;
    public event Action<Vector3> OnThrown;

    public event Action<IHandle> OnPickedUpHandle;
    public event Action<IHandle> OnMovedHandle;
    public event Action<IHandle> OnPlacedHandle;
    public event Action<IHandle> OnPlacedHandleInContainer;
    public event Action<IHandle, Vector3> OnThrownHandle;

    #endregion

  }

}
