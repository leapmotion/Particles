using System;
using System.Collections;
using UnityEngine;
using Leap;
using Leap.Unity;

public class CometLauncher : MonoBehaviour {

  public bool canAct = true;
  public SolarSystemSimulator sim;
  public float velocityScale = 0.1f;

  private Vector3 _spawnPosLeft;
  private Vector3 _spawnPosRight;
  private bool _isLeftPinching, _isRightPinching;

  private void Start() {
    StartCoroutine(pinchSpawn(() => Hands.Right));
    StartCoroutine(pinchSpawn(() => Hands.Left));
  }

  IEnumerator pinchSpawn(Func<Hand> getHand) {
    while (true) {

      //Wait for hand to pinch and be non-null
      while (true) {
        var hand = getHand();

        if (hand != null && hand.PinchStrength > 0.3f) {
          break;
        }

        yield return null;
      }

      sim.simBlockers++;

      int index = sim.currState.comets.Count;
      sim.currState.comets.Add(new SolarSystemSimulator.CometState() {
        position = getHand().GetPinchPosition(),
        velocity = Vector3.zero
      });
      sim.RestartPaths();

      //Wait for hand to dissapear or release the pinch
      while (true) {
        var hand = getHand();

        if (hand == null || hand.PinchStrength < 0.2f) {
          break;
        }

        var comet = sim.currState.comets[index];
        comet.velocity = (comet.position - hand.GetPinchPosition()) * velocityScale;
        sim.currState.comets[index] = comet;
        sim.RestartPaths();

        yield return null;
      }

      sim.simBlockers--;
    }
  }
}
