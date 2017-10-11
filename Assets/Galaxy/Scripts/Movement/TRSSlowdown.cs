using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;
using Leap.Unity.Animation;

public class TRSSlowdown : MonoBehaviour, ITimestepMultiplier {

  public LeapRTS trs;
  public GalaxySimulation sim;
  public float slowdownTime;
  public float speedupTime;

  private Tween _tween;

  public float multiplier { get; set; }

  void Start() {
    multiplier = 1;
    _tween = Tween.Persistent().Value(1, 0, t => {
      multiplier = t;
    }).
                                OverTime(slowdownTime);
  }

  void OnEnable() {
    sim.TimestepMultipliers.Add(this);
  }

  void OnDisable() {
    sim.TimestepMultipliers.Remove(this);
  }

  void Update() {
    if (trs._switchA.grasped || trs._switchB.grasped) {
      _tween.OverTime(slowdownTime).
             Play(Direction.Forward);
    } else {
      _tween.OverTime(speedupTime).
             Play(Direction.Backward);
    }
  }



}
