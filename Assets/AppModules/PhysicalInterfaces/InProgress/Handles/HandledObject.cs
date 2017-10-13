using Leap.Unity;
using Leap.Unity.Query;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Leap.Unity.Attributes;

namespace Leap.Unity.PhysicalInterfaces {

  public class HandledObject : MonoBehaviour {

    #region Inspector
    
    // Rendered via Custom Editor.
    /// <summary>
    /// All handles owned by this HandledObject. Manipulating these handles will
    /// manipulate the HandlesObject in some way -- by default, by moving it.
    /// </summary>
    private List<IHandle> _attachedHandles = new List<IHandle>();

    /// <summary>
    /// All handles owned by this HandledObject. Read-only.
    /// </summary>
    public ReadonlyList<IHandle> attachedHandles {
      get { return _attachedHandles; }
    }

    #endregion

    #region Unity Events

    protected virtual void Reset() {
      RefreshHandles();
    }

    protected virtual void OnValidate() {
      RefreshHandles();
    }

    protected virtual void OnEnable() {
      if (!_handlesInitialized) {
        RefreshHandles();
      }

      initializeHandledObject();

      registerHandleCallbacks();
    }

    protected virtual void OnDisable() {
      unregisterHandleCallbacks();
    }

    protected virtual void Update() {
      updateHandledObject();
    }

    #endregion

    #region Attached Handles

    private bool _handlesInitialized = false;

    public void RefreshHandles() {
      NewUtils.FindOwnedChildComponents(this, _attachedHandles,
                                        includeInactiveObjects: true);
      _handlesInitialized = true;
    }

    #endregion

    #region Handle Events

    private void registerHandleCallbacks() {
      foreach (var handle in _attachedHandles) {
        handle.OnMovedHandle             += onHandleMoved;
        handle.OnPickedUpHandle          += onHandlePickedUp;
        handle.OnPlacedHandle            += onHandlePlaced;
        handle.OnPlacedHandleInContainer += onHandlePlacedInContainer;
        handle.OnThrownHandle            += onHandleThrown;
      }
    }

    private void unregisterHandleCallbacks() {
      foreach (var handle in _attachedHandles) {
        handle.OnMovedHandle             -= onHandleMoved;
        handle.OnPickedUpHandle          -= onHandlePickedUp;
        handle.OnPlacedHandle            -= onHandlePlaced;
        handle.OnPlacedHandleInContainer -= onHandlePlacedInContainer;
        handle.OnThrownHandle            -= onHandleThrown;
      }
    }

    private void onHandleMoved(IHandle handle) {

    }

    private void onHandlePickedUp(IHandle handle) {
      _idleHandles.Remove(handle);

      _heldHandles.Add(handle);
    }

    private void onHandlePlaced(IHandle handle) {
      _heldHandles.Remove(handle);

      _idleHandles.Add(handle);
    }

    private void onHandlePlacedInContainer(IHandle handle) {
      _heldHandles.Remove(handle);

      _idleHandles.Add(handle);
    }

    private void onHandleThrown(IHandle handle, Vector3 throwVector) {
      _heldHandles.Remove(handle);

      _idleHandles.Add(handle);
    }

    #endregion

    #region Handled Object

    public Pose pose {
      get {
        return this.transform.ToWorldPose();
      }
    }

    /// <summary> Handles that are currently not held. </summary>
    private HashSet<IHandle> _idleHandles = new HashSet<IHandle>();
    /// <summary> Handles that are currently not held. (Read only.) </summary>
    protected ReadonlyHashSet<IHandle> idleHandles {
      get { return _idleHandles; }
    }

    /// <summary> Handles that are currently held. </summary>
    private HashSet<IHandle> _heldHandles = new HashSet<IHandle>();
    /// <summary> Handles that are currently held. (Read only.) </summary>
    protected ReadonlyHashSet<IHandle> heldHandles {
      get { return _heldHandles; }
    }

    public bool isHeld {
      get { return _heldHandles.Count > 0; }
    }

    private void initializeHandledObject() {
      _idleHandles.Clear();
      _heldHandles.Clear();

      foreach (var handle in _attachedHandles) {
        if (handle.isHeld) {
          _heldHandles.Add(handle);
        }
        else {
          _idleHandles.Add(handle);
        }
      }
    }

    private void updateHandledObject() {
      updatePreKabschState();

      Matrix4x4 kabschResult;
      solveHandleKabsch(out kabschResult);

      updatePostKabschState();

      updateHandledObjectPose(kabschResult);
    }

    #endregion

    #region Virtual Functions

    protected virtual void updateHandledObjectPose(Matrix4x4 kabschResult) {

      // Move this object based on the deltaPose, but preserve the handles' poses if
      // they happen to be child transforms of this object.

      var origPoses = Pool<List<Pose>>.Spawn();
      origPoses.Clear();
      try {
        foreach (var handle in _attachedHandles) {
          origPoses.Add(handle.pose);
        }

        //Debug.Log("Delta pose from handles: " + deltaPoseFromHandles);
        //Debug.Log("OK, my target pose will be " + this.pose.Then(deltaPoseFromHandles));
        //this.transform.SetWorldPose(this.pose.Then(deltaPoseFromHandles));
        this.transform.position = kabschResult.GetVector3() + this.transform.position;
        this.transform.rotation = kabschResult.GetQuaternion() * this.transform.rotation;

        int origPosesIdx = 0;
        foreach (var handle in _attachedHandles) {
          handle.SetPose(origPoses[origPosesIdx++]);
        }
      }
      finally {
        origPoses.Clear();
        Pool<List<Pose>>.Recycle(origPoses);
      }
    }

    #endregion

    #region Kabsch Movement

    private Interaction.KabschSolver _kabsch = new Interaction.KabschSolver();

    private Dictionary<IHandle, Pose> _origHandlePoses = new Dictionary<IHandle, Pose>();

    private void updatePreKabschState() {
      // Ensure there's a reference pose for all currently held handles.
      foreach (var handle in _attachedHandles) {
        if (!_origHandlePoses.ContainsKey(handle)) {
          _origHandlePoses[handle] = handle.pose;
        }
      }

      // Ensure there's NO reference pose for non-held handles.
      var removeHandlesFromKabsch = Pool<List<IHandle>>.Spawn();
      removeHandlesFromKabsch.Clear();
      try {
        foreach (var handlePosePair in _origHandlePoses) {
          if (!_attachedHandles.Contains(handlePosePair.Key)) {
            removeHandlesFromKabsch.Add(handlePosePair.Key);
          }
        }
        foreach (var handle in removeHandlesFromKabsch) {
          _origHandlePoses.Remove(handle);
        }
      }
      finally {
        removeHandlesFromKabsch.Clear();
        Pool<List<IHandle>>.Recycle(removeHandlesFromKabsch);
      }
    }

    private void solveHandleKabsch(out Matrix4x4 kabschResult) {
      if (_origHandlePoses.Count == 0) {
        kabschResult = Matrix4x4.identity;
        return;
      }

      List<Vector3> origPoints = Pool<List<Vector3>>.Spawn();
      origPoints.Clear();
      List<Vector3> curPoints = Pool<List<Vector3>>.Spawn();
      curPoints.Clear();

      // DELETE ME
      Pose aRefPose = new Pose(Vector3.one * 100000, Quaternion.identity);
      Pose aCurPose = new Pose(Vector3.one * 100000, Quaternion.identity);

      try {
        Vector3 objectPos = this.pose.position;

        foreach (var handlePosePair in _origHandlePoses) {
          Pose origPose = handlePosePair.Value;
          origPoints.Add(origPose.position - objectPos);
          origPoints.Add(origPose.position + origPose.rotation * Vector3.up * 0.01f - objectPos);
          origPoints.Add(origPose.position + origPose.rotation * Vector3.right * 0.01f - objectPos);
          aRefPose = origPose;

          Pose curPose = handlePosePair.Key.pose;
          curPoints.Add(curPose.position - objectPos);
          curPoints.Add(curPose.position + curPose.rotation * Vector3.up * 0.01f - objectPos);
          curPoints.Add(curPose.position + curPose.rotation * Vector3.right * 0.01f - objectPos);
          aCurPose = curPose;
        }

        kabschResult = _kabsch.SolveKabsch(origPoints, curPoints);

        //solvedPoseFromCurrentPose = solvedMatrix.GetPose();
      }
      finally {
        origPoints.Clear();
        Pool<List<Vector3>>.Recycle(origPoints);

        curPoints.Clear();
        Pool<List<Vector3>>.Recycle(curPoints);
      }
    }

    private void updatePostKabschState() {
      foreach (var handle in _attachedHandles) {
        if (_origHandlePoses.ContainsKey(handle)) {
          _origHandlePoses[handle] = handle.pose;
        }
      }
    }

    #endregion

    #region Access Helpers

    /// <summary>
    /// isHeld property support for PlayMaker.
    /// </summary>
    public bool GetIsHeld() { return isHeld; }

    #endregion

  }

}
