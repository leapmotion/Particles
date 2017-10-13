using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Layout {

  public static class LayoutUtils {

    public static Vector3 LayoutThrownUIPosition(Pose userHeadPose,
                                                 Vector3 thrownUIInitPosition,
                                                 Vector3 thrownUIInitVelocity) {
      List<Vector3> otherUIPositions = Pool<List<Vector3>>.Spawn();
      List<float> otherUIRadii = Pool<List<float>>.Spawn();
      try {
        return LayoutThrownUIPosition(userHeadPose,
                                      thrownUIInitPosition,
                                      thrownUIInitVelocity,
                                      0.1f, otherUIPositions, otherUIRadii);
      }
      finally {
        otherUIPositions.Clear();
        Pool<List<Vector3>>.Recycle(otherUIPositions);

        otherUIRadii.Clear();
        Pool<List<float>>.Recycle(otherUIRadii);
      }
    }

    public static Vector3 LayoutThrownUIPosition(Pose userHeadPose,
                                                 Vector3 thrownUIInitPosition,
                                                 Vector3 thrownUIInitVelocity,
                                                 float thrownUIRadius,
                                                 List<Vector3> otherUIPositions,
                                                 List<float> otherUIRadii) {
      // Push velocity away from the camera if necessary.
      Vector3 towardsCamera = (userHeadPose.position - thrownUIInitPosition).normalized;
      float towardsCameraness = Mathf.Clamp01(Vector3.Dot(towardsCamera, thrownUIInitVelocity.normalized));
      thrownUIInitVelocity = thrownUIInitVelocity + Vector3.Lerp(Vector3.zero, -towardsCamera * 2.00F, towardsCameraness);

      // Calculate velocity direction on the XZ plane.
      Vector3 groundPlaneVelocity = Vector3.ProjectOnPlane(thrownUIInitVelocity, Vector3.up);
      float groundPlaneDirectedness = groundPlaneVelocity.magnitude.Map(0.003F, 0.40F, 0F, 1F);
      Vector3 groundPlaneDirection = groundPlaneVelocity.normalized;

      // Calculate camera "forward" direction on the XZ plane.
      Vector3 cameraGroundPlaneForward = Vector3.ProjectOnPlane(userHeadPose.rotation * Vector3.forward, Vector3.up);
      float cameraGroundPlaneDirectedness = cameraGroundPlaneForward.magnitude.Map(0.001F, 0.01F, 0F, 1F);
      Vector3 alternateCameraDirection = (userHeadPose.rotation * Vector3.forward).y > 0F ? userHeadPose.rotation * Vector3.down : userHeadPose.rotation * Vector3.up;
      cameraGroundPlaneForward = Vector3.Slerp(alternateCameraDirection, cameraGroundPlaneForward, cameraGroundPlaneDirectedness);
      cameraGroundPlaneForward = cameraGroundPlaneForward.normalized;

      // Calculate a placement direction based on the camera and throw directions on the XZ plane.
      Vector3 placementDirection = Vector3.Slerp(cameraGroundPlaneForward, groundPlaneDirection, groundPlaneDirectedness);

      // Calculate a placement position along the placement direction between min and max placement distances.
      float minPlacementDistance = 0.25F;
      float maxPlacementDistance = 0.51F;
      Vector3 placementPosition = userHeadPose.position + placementDirection * Mathf.Lerp(minPlacementDistance, maxPlacementDistance,
                                                                                    (groundPlaneDirectedness * thrownUIInitVelocity.magnitude)
                                                                                    .Map(0F, 1.50F, 0F, 1F));

      // Don't move far if the initial velocity is small.
      float overallDirectedness = thrownUIInitVelocity.magnitude.Map(0.00F, 3.00F, 0F, 1F);
      placementPosition = Vector3.Lerp(thrownUIInitPosition, placementPosition, overallDirectedness * overallDirectedness);

      // Enforce placement height.
      float placementHeightFromCamera = -0.30F;
      placementPosition.y = userHeadPose.position.y + placementHeightFromCamera;

      // Enforce minimum placement away from user.
      Vector2 cameraXZ = new Vector2(userHeadPose.position.x, userHeadPose.position.z);
      Vector2 stationXZ = new Vector2(placementPosition.x, placementPosition.z);
      float placementDist = Vector2.Distance(cameraXZ, stationXZ);
      if (placementDist < minPlacementDistance) {
        float distanceLeft = (minPlacementDistance - placementDist) + thrownUIRadius;
        Vector2 xzDisplacement = (stationXZ - cameraXZ).normalized * distanceLeft;
        placementPosition += new Vector3(xzDisplacement[0], 0F, xzDisplacement[1]);
      }

      return placementPosition;
    }

  }

}
