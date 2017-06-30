using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Interaction;

public class FollowRightHandPrimaryHover : MonoBehaviour {

  public InteractionHand intHand;

  void Update() {
    if (intHand.isPrimaryHovering) {
      this.transform.position = intHand.primaryHoveredObject.transform.position;
    }
  }

}
