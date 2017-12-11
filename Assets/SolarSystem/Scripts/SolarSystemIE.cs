using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;

public class SolarSystemIE : MonoBehaviour, IPropertyMultiplier {

  public LeapProvider provider;
  public SolarSystemSimulator sim;
  public Transform spawnAnchor;
  public CometIEBehaviour handlePrefab;

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

    //Update all handles with the new multiplier
    foreach (var spawned in _spawned) {
      spawned.OnMultiplierChange(multiplier);
    }

    //Allow each behavior to individually modify the state if needed
    for (int i = 0; i < sim.currState.comets.Count; i++) {
      var comet = sim.currState.comets[i];
      if (_spawned[i].GetModifiedState(ref comet)) {
        sim.currState.comets[i] = comet;
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
      _spawned[i].UpdateState(sim.currState.comets[i]);
    }
  }
}
