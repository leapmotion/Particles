using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;
using Leap.Unity.DevGui;

public class SolarSystemIE : MonoBehaviour, IPropertyMultiplier {

  public bool canAct = true;
  public LeapProvider provider;
  public SolarSystemSimulator sim;
  public Transform spawnAnchor;
  public CometIEBehaviour handlePrefab;

  [DevValue]
  public bool requireGraspToRotate = false;

  public float multiplier { get; set; }
  private List<CometIEBehaviour> _spawned = new List<CometIEBehaviour>();

  private void OnEnable() {
    multiplier = 1;
    SolarSystemSimulator.speedMultiplier.Add(this);
    SolarSystemSimulator.OnCreateSystem += onCreateSystem;
    SolarSystemSimulator.OnDestroySystem += onDestroySystem;
    SolarSystemSimulator.OnUpdateSystem += onUpdateSystem;
  }

  private void OnDisable() {
    SolarSystemSimulator.speedMultiplier.Remove(this);
    SolarSystemSimulator.OnCreateSystem -= onCreateSystem;
    SolarSystemSimulator.OnDestroySystem -= onDestroySystem;
    SolarSystemSimulator.OnUpdateSystem -= onUpdateSystem;
  }

  private void Update() {

    //Update sim multiplier
    //the individual comet behaviors decide the multiplier collectively
    multiplier = 1;
    for (int i = 0; i < sim.currState.comets.Count; i++) {
      multiplier = Mathf.Min(multiplier, _spawned[i].GetMultiplier(sim.currState.comets[i]));
    }

    if (!canAct) {
      multiplier = 1;
    }

    //Update all handles with the new multiplier
    foreach (var spawned in _spawned) {
      spawned.OnMultiplierChange(multiplier);
    }

    //Allow each behavior to individually modify the state if needed
    for (int i = 0; i < sim.currState.comets.Count; i++) {
      var comet = sim.currState.comets[i];
      if (canAct && _spawned[i].GetModifiedState(ref comet)) {

        SolarSystemSimulator.CometState prev, curr;
        comet.Generate2States(sim.prevState.simTime,
                              sim.currState.simTime,
                              sim.simulationTime,
                              out prev,
                              out curr);

        sim.currState.comets[i] = curr;
        sim.prevState.comets[i] = prev;

        sim.RestartPaths();
      }
    }
  }

  private void onCreateSystem() {
    foreach (var comet in sim.currState.comets) {
      var spawned = Instantiate(handlePrefab);
      _spawned.Add(spawned);

      spawned.transform.SetParent(spawnAnchor, worldPositionStays: true);
      spawned.gameObject.SetActive(true);
    }
  }

  private void onDestroySystem() {
    foreach (var spawned in _spawned) {
      DestroyImmediate(spawned.gameObject);
    }
    _spawned.Clear();
  }

  private void onUpdateSystem() {
    for (int i = 0; i < sim.currState.comets.Count; i++) {
      var currComet = sim.currState.comets[i];
      var prevComet = sim.prevState.comets[i];

      float interp = Mathf.InverseLerp(sim.prevState.simTime,
                                       sim.currState.simTime,
                                       sim.simulationTime);

      var interpComet = SolarSystemSimulator.CometState.Lerp(prevComet,
                                                             currComet,
                                                             interp);

      _spawned[i].UpdateState(interpComet);
    }
  }
}
