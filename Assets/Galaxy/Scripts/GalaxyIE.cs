using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;
using Leap.Unity.Interaction;
using Leap.Unity.Attributes;

public class GalaxyIE : MonoBehaviour, ITimestepMultiplier {

  [SerializeField]
  private GalaxySimulation _sim;

  [SerializeField]
  private GalaxyRenderer _renderer;

  [SerializeField]
  private BlackHoleBehaviour _iePrefab;

  [MinMax(0, 1)]
  [SerializeField]
  private Vector2 _distRange = new Vector2(0.05f, 0.2f);

  [SerializeField]
  private AnimationCurve _distCurve = DefaultCurve.SigmoidUp;

  public float multiplier { get; set; }

  private List<BlackHoleBehaviour> _spawned = new List<BlackHoleBehaviour>();
  private int _numGrasped = 0;


  private void OnEnable() {
    _sim.OnReset += onResetSim;
    _sim.OnStep += onStepSim;
  }

  private void OnDisable() {
    _sim.OnReset -= onResetSim;
    _sim.OnStep -= onStepSim;
  }

  public void OnGrasp() {
    _numGrasped++;
    _sim.simulate = false;
  }

  public void OnRelease() {
    _numGrasped--;

    if (_numGrasped == 0) {
      _sim.simulate = true;
    }
  }

  public void OnMove() {
    if (_numGrasped > 0) {
      unsafe {
        GalaxySimulation.BlackHoleMainState* ptr = _sim.mainState.mainState;
        for (int i = 0; i < _sim.mainState.count; i++, ptr++) {
          (*ptr).position = _spawned[i].transform.position;
          (*ptr).velocity = _spawned[i].deltaRot * (*ptr).velocity;
        }
      }
      _sim.ResetTrails();
    }
  }

  private void onResetSim() {
    foreach (var spawned in _spawned) {
      DestroyImmediate(spawned.gameObject);
    }
    _spawned.Clear();

    unsafe {
      GalaxySimulation.BlackHoleMainState* ptr = _sim.mainState.mainState;
      for (int i = 0; i < _sim.mainState.count; i++, ptr++) {
        var obj = Instantiate(_iePrefab);
        _spawned.Add(obj);

        obj.transform.SetParent(_renderer.displayAnchor);
        obj.transform.position = (*ptr).position;
        obj.transform.localScale = Vector3.one;
        obj.transform.rotation = Quaternion.identity;
        obj.gameObject.SetActive(true);
      }
    }
  }

  private void onStepSim() {
    if (_sim.mainState.count != _spawned.Count) {
      onResetSim();
    }

    unsafe {
      GalaxySimulation.BlackHoleMainState* ptr = _sim.mainState.mainState;
      for (int i = 0; i < _sim.mainState.count; i++, ptr++) {
        _spawned[i].transform.position = (*ptr).position;
      }
    }

    float minDist = float.MaxValue;
    foreach (var spawned in _spawned) {
      spawned.transform.localScale = Vector3.one / spawned.transform.parent.lossyScale.x;

      if (Hands.Left != null) {
        minDist = Mathf.Min(minDist, Vector3.Distance(Hands.Left.PalmPosition.ToVector3(),
                                                      spawned.transform.position));
      }

      if (Hands.Right != null) {
        minDist = Mathf.Min(minDist, Vector3.Distance(Hands.Right.PalmPosition.ToVector3(),
                                                      spawned.transform.position));
      }
    }

    float percent = Mathf.InverseLerp(_distRange.x, _distRange.y, minDist);
    float curvedPercent = _distCurve.Evaluate(percent);

    multiplier = curvedPercent;
    foreach (var spawned in _spawned) {
      var renderer = spawned.GetComponentInChildren<Renderer>();
      renderer.material.color = renderer.material.color.WithAlpha(1 - curvedPercent);
    }
  }
}
