using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class TheBigTest : MonoBehaviour {

  public Vector3 resultA;
  public Vector3 resultB;

  [ContextMenu("Try it")]
  void Update() {

    Vector3 force1 = Vector3.zero;
    Vector3 force2 = Vector3.zero;
    int count = 0;

    foreach (Transform child in transform) {
      count++;

      Vector3 toChild = child.localPosition;

      force1 += (toChild.normalized);
      force2 += toChild;
    }

    resultA = (force1 / count).normalized;
    resultB = (force2 / count).normalized;
  }

  void OnDrawGizmos() {
    Gizmos.color = Color.green;
    Gizmos.DrawLine(transform.position, transform.position + resultA * 3);
    Gizmos.color = Color.blue;
    Gizmos.DrawLine(transform.position, transform.position + resultB * 3);
  }



}
