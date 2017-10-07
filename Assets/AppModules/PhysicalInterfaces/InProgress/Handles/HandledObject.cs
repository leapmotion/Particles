using Leap.Unity.Query;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Leap.Unity.PhysicalInterfaces {

  //using HandleQuery = QueryWrapper<IHandle,
  //                                 SelectOp<SerializeableHandle, IHandle,
  //                                          QueryConversionExtensions
  //                                            .ListQueryOp<SerializeableHandle>>>;

  //public class HandledObject : MonoBehaviour,
  //                             IHandle {

  //  [SerializeField]
  //  private List<SerializeableHandle> _handles;
  //  public HandleQuery handlesQuery {
  //    get { return _handles.Query().Select(sH => sH.handle); }
  //  }

  //  void Start() {

  //  }

  //  #region IHandle

  //  public Pose pose {
  //    get {
  //      return this.transform.ToWorldPose();
  //    }
  //  }

  //  public bool isHeld {
  //    get {
  //      return handlesQuery.Any(h => h.isHeld);
  //    }
  //  }

  //  public Vector3 heldPosition {
  //    get {
  //      return handlesQuery.Select(h => h.heldPosition)
  //                         .Fold((a, b) => a + b)
  //             / handlesQuery.Count();
  //    }
  //  }

  //  public event Action OnPickedUp;
  //  public event Action OnMoved;
  //  public event Action OnPlaced;
  //  public event Action OnPlacedInContainer;
  //  public event Action<Vector3> OnThrown;

  //  #endregion

  //}

}
