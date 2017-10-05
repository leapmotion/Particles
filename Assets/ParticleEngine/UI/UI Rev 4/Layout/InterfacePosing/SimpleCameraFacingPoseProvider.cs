using System.Collections;
using System.Collections.Generic;
using Leap.Unity.PhysicalInterfaces;
using UnityEngine;

namespace Leap.Unity.Layout {

  public class SimpleCameraFacingPoseProvider : MonoBehaviour,
                                                IPoseProvider {

    public bool flipPose = false;

    public Pose GetTargetPose() {
      var cameraPose = Camera.main.transform.ToWorldPose();

      return new Pose(this.transform.position,
                      PoseUtil.FaceTargetWithoutTwist(this.transform.position,
                                                      cameraPose.position,
                                                      flipPose));
    }

  }

}
