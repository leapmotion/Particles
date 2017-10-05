using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Interaction;

public class DoubleBallManager : MonoBehaviour {

  public Transform anchor;
  public InteractionBehaviour ball;
  public GrabSwitch _switch;

  void Update() {
    _switch.grasped = false;

    if (ball.isGrasped) {
      _switch.Position = ball.transform.position;
      _switch.Rotation = ball.transform.rotation;
      _switch.grasped = true;
    } else {
      ball.transform.position = anchor.transform.position;
      ball.transform.rotation = anchor.transform.rotation;
    }
  }
}
