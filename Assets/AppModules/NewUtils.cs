﻿using Leap.Unity.Query;
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

  #region Unity Objects

  /// <summary>
  /// Usage is the same as FindObjectOfType, but this method will also return objects
  /// that are inactive.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  /// <returns></returns>
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

}
