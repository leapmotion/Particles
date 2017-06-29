using UnityEngine;
using System.Collections;

namespace Leap.Unity {

  public static class Vector3Utils {

    public static bool ContainsNaN(this Vector3 v) {
      return float.IsNaN(v.x)
          || float.IsNaN(v.y)
          || float.IsNaN(v.z);
    }

  }

}