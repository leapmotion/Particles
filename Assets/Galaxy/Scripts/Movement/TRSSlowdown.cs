using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;
using Leap.Unity.Animation;

public class TRSSlowdown : MonoBehaviour {

  public LeapRTS trs;
  public LeapTRS2 trs2;
  public GalaxySimulation sim;
  public float slowdownTime;
  public float speedupTime;

  private Tween _currTween;
  private float _prevSpeed;
  private bool _isSlow;

  void Update() {
    var trsSwitchA = (trs.enabled ? trs._switchA : trs2.switchA);
    var trsSwitchB = (trs.enabled ? trs._switchB : trs2.switchB);

    if (!trsSwitchA.grasped && !trsSwitchB.grasped && !_currTween.isValid) {
      _prevSpeed = sim.trsTimestep;
    }

    if (trsSwitchA.grasped || trsSwitchB.grasped) {
      if (_currTween.isValid) {
        _currTween.Stop();
      }

      _currTween = Tween.Single().Value(sim.timestep, 0, t => sim.trsTimestep = t).
                                  OverTime(slowdownTime).
                                  Play();
      _isSlow = true;
    } else if (_isSlow) {
      if (_currTween.isValid) {
        _currTween.Stop();
      }

      _currTween = Tween.Single().Value(0, _prevSpeed, t => sim.trsTimestep = t).
                                  OverTime(speedupTime).
                                  Smooth(SmoothType.SmoothStart).
                                  Play();
      _isSlow = false;
    }
  }



}
