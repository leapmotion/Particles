using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public unsafe class BasicGalaxyTest : MonoBehaviour {


  [ContextMenu("Try it")]
  void tryit() {
    Debug.Log(NBodyC.GetOffsetOfVelocity());
    Debug.Log(Marshal.SizeOf(typeof(GalaxySimulation.BlackHole)));
  }




}
