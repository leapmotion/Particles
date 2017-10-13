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

    public bool flip180 = false;

    private float _cutoffThrowSpeed = PhysicalInterfaceUtils.MIN_THROW_SPEED;

    public Vector3 GetTargetPosition() {
      Vector3 layoutPos;

      if (uiAnchorHandle.movement.velocity.magnitude <= _cutoffThrowSpeed) {

        layoutPos = uiAnchorHandle.pose.position;

        DebugPing.Ping(layoutPos, Color.white);
      }
      else {
        layoutPos = LayoutUtils.LayoutThrownUIPosition(Camera.main.transform.ToWorldPose(),
                                                       uiAnchorHandle.pose.position,
                                                       uiAnchorHandle.movement.velocity);

        DebugPing.Ping(layoutPos, Color.red);
      }

      return layoutPos;
    }

    public Quaternion GetTargetRotation() {
      Quaternion layoutRot = NewUtils.FaceTargetWithoutTwist(uiLookAnchor.GetTargetWorldPosition(),
                                                             Camera.main.transform.position,
                                                             flip180: flip180);

      return layoutRot;
    }

  }

}
