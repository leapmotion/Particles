using Leap.Unity;
using Leap.Unity.Query;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class NewUtils {

  #region Transform Utils

  /// <summary>
  /// Returns a list of transforms including this transform and ALL of its children,
  /// including the children of its children, and the children of their children, and
  /// so on.
  /// 
  /// THIS ALLOCATES GARBAGE. Use it for editor code only.
  /// </summary>
  public static List<Transform> GetSelfAndAllChildren(this Transform t,
                                                     bool includeInactiveObjects = false) {
    var allChildren = new List<Transform>();
    
    Stack<Transform> toVisit = Pool<Stack<Transform>>.Spawn();

    try {
      // Traverse the hierarchy of this object's transform to find all of its Colliders.
      toVisit.Push(t.transform);
      Transform curTransform;
      while (toVisit.Count > 0) {
        curTransform = toVisit.Pop();

        // Recursively search children and children's children
        foreach (var child in curTransform.GetChildren()) {
          // Ignore children with Rigidbodies of their own; its own Rigidbody
          // owns its own colliders and the colliders of its children
          if (includeInactiveObjects || child.gameObject.activeSelf) {
            toVisit.Push(child);
          }
        }

        // Since we'll visit every valid child, all we need to do is add the colliders
        // of every transform we visit.
        allChildren.Add(curTransform);
      }
    }
    finally {
      toVisit.Clear();
      Pool<Stack<Transform>>.Recycle(toVisit);
    }

    return allChildren;
  }

  #endregion

  /// <summary>
  /// Converts the quaternion into an axis and an angle and returns the vector
  /// axis * angle.
  /// </summary>
  public static Vector3 ToAngleAxisVector(this Quaternion q) {
    float angle;
    Vector3 axis;
    q.ToAngleAxis(out angle, out axis);
    return axis * angle;
  }

}
