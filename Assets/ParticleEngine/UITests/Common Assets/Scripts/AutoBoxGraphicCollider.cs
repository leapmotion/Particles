using Leap.Unity.Attributes;
using Leap.Unity.GraphicalRenderer;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class AutoBoxGraphicCollider : MonoBehaviour {

  [MinValue(0F)]
  public float additionalDepth = 0.005F;

#if UNITY_EDITOR
  void Update() {
    if (Application.isPlaying) return;

    var boxGraphic = GetComponent<LeapBoxGraphic>();
    if (boxGraphic != null) {
      var someCollider = GetComponent<Collider>();
      if (someCollider != null && !(someCollider is BoxCollider)) {
        Debug.LogError("AutoBoxGraphicCollider only works with _box_ colliders.");
        return;
      }

      var boxCollider = GetComponent<BoxCollider>();
      if (boxCollider == null) {
        boxCollider = gameObject.AddComponent<BoxCollider>();
      }

      if (boxCollider != null) {
        boxCollider.size = new Vector3(boxGraphic.size.x, boxGraphic.size.y, boxGraphic.size.z + additionalDepth);
        boxCollider.center = new Vector3(0F, 0F, -boxGraphic.size.z / 2F + additionalDepth / 2F);
      }
    }
  }
#endif

}
