using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;
using Leap.Unity.Interaction;
using Leap.Unity.Attributes;

public class GalaxyIE : MonoBehaviour, IPropertyMultiplier {

  [SerializeField]
  private bool _canAct = true;
  public bool canAct {
    get { return _canAct; }
    set { _canAct = value; }
  }

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
    multiplier = 1;
    _sim.OnReset += onResetSim;
    _sim.OnStep += onStepSim;
    _sim.TimestepMultipliers.Add(this);
  }

  private void OnDisable() {
    _sim.OnReset -= onResetSim;
    _sim.OnStep -= onStepSim;
    _sim.TimestepMultipliers.Remove(this);
  }

  private void Update() {
    foreach (var spawned in _spawned) {
      float distToBlackHole = float.MaxValue;

      if (Hands.Left != null) {
        distToBlackHole = Mathf.Min(distToBlackHole, Vector3.Distance(Hands.Left.PalmPosition.ToVector3(),
                                                      spawned.transform.position));
      }

      if (Hands.Right != null) {
        distToBlackHole = Mathf.Min(distToBlackHole, Vector3.Distance(Hands.Right.PalmPosition.ToVector3(),
                                                      spawned.transform.position));
      }

      var renderer = spawned.GetComponentInChildren<Renderer>();

      float dstAlpha;
      if (canAct) {
        dstAlpha = 1 - Mathf.InverseLerp(_distRange.x, _distRange.y, distToBlackHole);
      } else {
        dstAlpha = 0;
      }

      renderer.material.color = renderer.material.color.WithAlpha(dstAlpha);
    }
  }

  public void OnGrasp(BlackHoleBehaviour graspedBehaviour) {
    _numGrasped++;
    _sim.simulate = false;

    int index = _spawned.IndexOf(graspedBehaviour);
    _sim.BeginDrag(_renderer.displayAnchor.worldToLocalMatrix * graspedBehaviour.transform.localToWorldMatrix, index);
  }

  public void OnRelease(BlackHoleBehaviour releasedBehaviour) {
    _numGrasped--;

    int index = _spawned.IndexOf(releasedBehaviour);
    _sim.EndDrag(index);

    if (_numGrasped == 0) {
      _sim.simulate = true;
    }
  }

  public void OnMove(BlackHoleBehaviour movedBehaviour) {
    unsafe {
      if (_numGrasped > 0) {
        for (int i = 0; i < _sim.mainState->numBlackHoles; i++) {
          var ie = _spawned[i].GetComponent<InteractionBehaviour>();
          if (!ie.isGrasped) {
            continue;
          }

          _sim.UpdateDrag(_renderer.displayAnchor.worldToLocalMatrix * _spawned[i].transform.localToWorldMatrix, i);
        }

        GalaxySimulation.BlackHole* ptr = _sim.mainState->blackHoles;
        for (int i = 0; i < _sim.mainState->numBlackHoles; i++, ptr++) {
          ptr->position = _renderer.displayAnchor.InverseTransformPoint(_spawned[i].transform.position);
          ptr->velocity = _renderer.displayAnchor.InverseTransformDirection(_spawned[i].transform.forward * (*ptr).velocity.magnitude);
        }
        _sim.ResetTrails();
      }
    }
  }

  private void onResetSim() {
    foreach (var spawned in _spawned) {
      DestroyImmediate(spawned.gameObject);
    }
    _spawned.Clear();

    unsafe {
      GalaxySimulation.BlackHole* ptr = _sim.mainState->blackHoles;
      for (int i = 0; i < _sim.mainState->numBlackHoles; i++, ptr++) {
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

  private unsafe void onStepSim() {
    if (_sim.mainState->numBlackHoles != _spawned.Count) {
      onResetSim();
    }

    if (canAct) {
      foreach (var item in _spawned) {
        item.GetComponent<InteractionBehaviour>().ignoreGrasping = false;
      }
    } else if (_numGrasped == 0) {
      foreach (var item in _spawned) {
        item.GetComponent<InteractionBehaviour>().ignoreGrasping = true;
      }
    }

    GalaxySimulation.BlackHole* ptr = _sim.mainState->blackHoles;
    for (int i = 0; i < _sim.mainState->numBlackHoles; i++, ptr++) {
      _spawned[i].transform.position = _renderer.displayAnchor.TransformPoint(ptr->position);
      _spawned[i].transform.rotation = _renderer.displayAnchor.rotation * Quaternion.LookRotation(ptr->velocity);
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
  }
}
