using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public unsafe class BasicGalaxyTest : MonoBehaviour {


  private GalaxySimulation.UniverseState* state;

  private void OnEnable() {
    state = NBodyC.CreateGalaxy(1);
    state->time = 0;
    state->frames = 0;
  }

  private void OnDisable() {
    NBodyC.DestroyGalaxy(state);
  }

  private void Update() {

    Debug.Log(state->time + " : " + state->frames);
    if (Input.GetKeyDown(KeyCode.Space)) {
      NBodyC.StepGalaxy(state);
    }


  }




}
