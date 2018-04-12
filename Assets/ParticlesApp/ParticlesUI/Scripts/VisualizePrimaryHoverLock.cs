using Leap.Unity.Attributes;
using Leap.Unity.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisualizePrimaryHoverLock : MonoBehaviour {

  public InteractionController leftHand;
  public InteractionController rightHand;

  public Material lHandMaterial;
  public Material rHandMaterial;

  [EditTimeOnly]
  public string colorProperty = "_Outline";
  private int _colorPropertyId;

  public Color unlockedColor;
  public Color lockedColor;

  void Start() {
    _colorPropertyId = Shader.PropertyToID(colorProperty);
  }

  void Update() {
    if (leftHand.primaryHoverLocked) {
      lHandMaterial.SetColor(_colorPropertyId, lockedColor);
    }
    else {
      lHandMaterial.SetColor(_colorPropertyId, unlockedColor);
    }

    if (rightHand.primaryHoverLocked) {
      rHandMaterial.SetColor(_colorPropertyId, lockedColor);
    }
    else {
      rHandMaterial.SetColor(_colorPropertyId, unlockedColor);
    }
  }

}
