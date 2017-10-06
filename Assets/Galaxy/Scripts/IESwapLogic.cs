using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap;
using Leap.Unity;
using Leap.Unity.Query;

public class IESwapLogic : MonoBehaviour {

  public GameObject grabbablePrefab;
  public GalaxySimulation sim;
  public int gestureCounter = 0;

  private List<GameObject> spawned = new List<GameObject>();

  void Update() {
    if (Hands.Left != null && Hands.Right != null) {
      bool doesSatisfy = true;
      int upCount = 0;

      foreach (var finger in Hands.Left.Fingers.Query().Concat(Hands.Right.Fingers.Query())) {
        if (!finger.IsExtended) {
          continue;
        }

        switch (finger.Type) {
          case Finger.FingerType.TYPE_INDEX:
            upCount++;
            break;
          case Finger.FingerType.TYPE_MIDDLE:
            upCount++;
            break;
          case Finger.FingerType.TYPE_PINKY:
            doesSatisfy = false;
            break;
          case Finger.FingerType.TYPE_RING:
            doesSatisfy = false;
            break;
          case Finger.FingerType.TYPE_THUMB:
            doesSatisfy = false;
            break;
        }
      }

      if (doesSatisfy && upCount == 4) {
        gestureCounter++;
      } else {
        gestureCounter = Mathf.Max(0, gestureCounter - 1);
      }
    } else {
      gestureCounter = 0;
    }

    unsafe {
      if (gestureCounter > 30) {
        sim.simulate = !sim.simulate;

        if (sim.simulate) {
          //Destroy IE and put state back
        } else {
          //Grab state and create IE
          spawned.Clear();
          for (int i = 0; i < sim.mainState.count; i++) {
            var obj = Instantiate(grabbablePrefab);
            obj.transform.position = (*(sim.mainState.mainState + i)).position;
            obj.transform.rotation = Quaternion.identity;
            spawned.Add(obj);
          }
        }
      }
    }
  }
}
