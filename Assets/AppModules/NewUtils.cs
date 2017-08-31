using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class NewUtils {

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
}
