using Leap.Unity.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OverrideHandInfluenceWhenPrimaryHover : MonoBehaviour {

  public InteractionManager manager;

  public TextureSimulator particleSimulator;

  void Update() {
    //bool overrideHandInfluence = false;
    //foreach (var controller in manager.interactionControllers) {
    //  if (controller.isPrimaryHovering) {
    //    overrideHandInfluence = true;
    //    break;
    //  }
    //}

    //particleSimulator.SetOverrideDisableHandInfluence(overrideHandInfluence);
  }

}
