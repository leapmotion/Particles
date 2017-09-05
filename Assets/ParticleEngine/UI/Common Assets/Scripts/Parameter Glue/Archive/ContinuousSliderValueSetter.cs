using Leap.Unity.GraphicalRenderer;
using Leap.Unity.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ContinuousSliderValueSetter : MonoBehaviour {
  
  public LeapTextGraphic currentValueTextGraphic;
  public string prefix;
  public string postfix;

  public abstract void SetWithSliderValue(float value);

  public void OnSlideEvent(float value) {
    SetWithSliderValue(value);

    if (currentValueTextGraphic != null) {
      currentValueTextGraphic.text = prefix + value.ToString() + postfix;
    }
  }

}
