using System.Collections;
using System.Collections.Generic;
using Leap.Unity.PhysicalInterfaces;
using UnityEngine;
using Leap.Unity.Attributes;

namespace Leap.Unity.Layout {

  public class UIPoseProvider : MonoBehaviour,
                                IPoseProvider {

    [SerializeField, ImplementsInterface(typeof(IHandle))]
    private MonoBehaviour _uiAnchorHandle;
    public IHandle uiAnchorHandle {
      get {
        return _uiAnchorHandle as IHandle;
      }
    }

    [SerializeField, ImplementsInterface(typeof(IWorldPositionProvider))]
    private MonoBehaviour _uiLookAnchor;
    public  IWorldPositionProvider uiLookAnchor {
      get {
        return _uiLookAnchor as IWorldPositionProvider;
      }
    }

    private float _cutoffThrowSpeed = 0.10f;

    public Pose GetTargetPose() {
      Vector3 layoutPos;
      if (uiAnchorHandle.movement.velocity.magnitude > _cutoffThrowSpeed) {
        layoutPos = uiAnchorHandle.pose.position;
      }
      else {
        layoutPos = LayoutUtils.LayoutThrownUIPosition(Camera.main.transform.ToWorldPose(),
                                                       uiAnchorHandle.pose.position,
                                                       uiAnchorHandle.movement.velocity);
      }

      Quaternion layoutRot = NewUtils.FaceTargetWithoutTwist(uiLookAnchor.GetTargetWorldPosition(),
                                                             Camera.main.transform.position,
                                                             flip180: true);

      return new Pose(layoutPos, layoutRot);
    }

  }

}
