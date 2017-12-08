using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;

public class ProceduralFloat {

  public float value {
    get {
      return 0;
    }
  }

  public static implicit operator float(ProceduralFloat pf) {
    return pf.value;
  }
}

public abstract class FloatProviderBase {
  public abstract float value { get; }
}

public class ConstantFloat : FloatProviderBase {

  [SerializeField]
  private float _value;

  public override float value {
    get {
      return _value;
    }
  }
}

public class RandomRangeFloat : FloatProviderBase {

  [SerializeField]
  private Vector2 _valueRange;

  public override float value {
    get {
      return Random.Range(_valueRange.x, _valueRange.y);
    }
  }
}

public class RandomCurveFloat : FloatProviderBase {

  [SerializeField]
  private AnimationCurve _valueCurve;

  public override float value {
    get {
      return _valueCurve.Evaluate(Random.value);
    }
  }
}

public class MultiplyFloat : FloatProviderBase {

  [SerializeField]
  private ProceduralFloat _propertyA;
  [SerializeField]
  private ProceduralFloat _propertyB;

  public override float value {
    get {
      return _propertyA * _propertyB;
    }
  }
}
