using Leap.Unity.Attributes;
using Leap.Unity.GraphicalRenderer;
using Leap.Unity.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetParticleMaterialFloatWithSliderValue : ContinuousSliderValueSetter {

  public TextureSimulator particleSimulator;
  
  [OnEditorChange("ReloadPropertyID")]
  public string floatPropertyName = "_Size";
  private int propertyID = -1;

  private void ReloadPropertyID() {
    propertyID = Shader.PropertyToID(floatPropertyName);
  }

  public override void SetWithSliderValue(float value) {
    if (propertyID == -1) {
      ReloadPropertyID();
    }

    particleSimulator.particleMat.SetFloat(propertyID, value);
  }

}
