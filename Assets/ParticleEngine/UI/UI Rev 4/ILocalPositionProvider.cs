using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ILocalPositionProvider {

  Vector3 GetLocalPosition(Transform transform);

}
