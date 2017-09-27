using Leap.Unity;
using Leap.Unity.Query;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class NewUtils {

  #region Math

  /// <summary>
  /// Returns the largest component of the input vector.
  /// </summary>
  public static float CompMax(this Vector2 v) {
    return Mathf.Max(v.x, v.y);
  }

  /// <summary>
  /// Returns the largest component of the input vector.
  /// </summary>
  public static float CompMax(this Vector3 v) {
    return Mathf.Max(Mathf.Max(v.x, v.y), v.z);
  }

  /// <summary>
  /// Returns the largest component of the input vector.
  /// </summary>
  public static float CompMax(this Vector4 v) {
    return Mathf.Max(Mathf.Max(Mathf.Max(v.x, v.y), v.z), v.w);
  }

  /// <summary>
  /// Returns the smallest component of the input vector.
  /// </summary>
  public static float CompMin(this Vector2 v) {
    return Mathf.Min(v.x, v.y);
  }

  /// <summary>
  /// Returns the smallest component of the input vector.
  /// </summary>
  public static float CompMin(this Vector3 v) {
    return Mathf.Min(Mathf.Min(v.x, v.y), v.z);
  }

  /// <summary>
  /// Returns the smallest component of the input vector.
  /// </summary>
  public static float CompMin(this Vector4 v) {
    return Mathf.Min(Mathf.Min(Mathf.Min(v.x, v.y), v.z), v.w);
  }

  #endregion

  #region Unity Object Utils

  /// <summary>
  /// Usage is the same as FindObjectOfType, but this method will also return objects
  /// that are inactive.
  /// 
  /// Use this method to search for singleton-pattern objects even if they are disabled,
  /// but be warned that it's not cheap to call!
  /// </summary>
  public static T FindObjectInHierarchy<T>() where T : UnityEngine.Object {
    T obj = Resources.FindObjectsOfTypeAll<T>().Query().FirstOrDefault();
    if (obj == null) return null;

    #if UNITY_EDITOR
    // Exclude prefabs.
    var prefabType = UnityEditor.PrefabUtility.GetPrefabType(obj);
    if (prefabType == UnityEditor.PrefabType.ModelPrefab
        || prefabType == UnityEditor.PrefabType.Prefab) {
      return null;
    }
    #endif

    return obj;
  }

  #endregion

  #region Math Utils

  /// <summary>
  /// Extrapolates using time values for positions a and b at extrapolatedTime.
  /// </summary>
  public static Vector3 TimedExtrapolate(Vector3 a, float aTime,
                                         Vector3 b, float bTime,
                                         float extrapolatedTime) {
    return Vector3.LerpUnclamped(a, b, extrapolatedTime.MapUnclamped(aTime, bTime, 0f, 1f));
  }

  /// <summary>
  /// Extrapolates using time values for rotations a and b at extrapolatedTime.
  /// </summary>
  public static Quaternion TimedExtrapolate(Quaternion a, float aTime,
                                            Quaternion b, float bTime,
                                            float extrapolatedTime) {
    return Quaternion.SlerpUnclamped(a, b, extrapolatedTime.MapUnclamped(aTime, bTime, 0f, 1f));
  }

  #endregion

  #region List Utils

  public static void EnsureListExists<T>(ref List<T> list) {
    if (list == null) {
      list = new List<T>();
    }
  }

  public static void EnsureListCount<T>(this List<T> list, int count, Func<T> createT, Action<T> deleteT) {
    while (list.Count < count) {
      list.Add(createT());
    }

    while (list.Count > count) {
      T tempT = list[list.Count - 1];
      list.RemoveAt(list.Count - 1);
      deleteT(tempT);
    }
  }

  public static void EnsureListCount<T>(this List<T> list, int count) {
    if (list.Count == count) return;

    while (list.Count < count) {
      list.Add(default(T));
    }

    while (list.Count > count) {
      T tempT = list[list.Count - 1];
      list.RemoveAt(list.Count - 1);
    }
  }

  #endregion

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

  #region Value Mapping Utils

  /// <summary>
  /// Returns a vector between resultMin and resultMax based on the input value's position
  /// between valueMin and valueMax.
  /// The input value is clamped between valueMin and valueMax.
  /// </summary>
  public static Vector3 Map(float input, float valueMin, float valueMax, Vector3 resultMin, Vector3 resultMax) {
    return Vector3.Lerp(resultMin, resultMax, Mathf.InverseLerp(valueMin, valueMax, input));
  }

  #endregion

}
