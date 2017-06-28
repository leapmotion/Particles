using Leap.Unity.GraphicalRenderer;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class TextGraphicSetter : MonoBehaviour {

  public LeapTextGraphic textGraphic;
  public string prefix;
  public string postfix;

  public abstract string GetTextValue();

  void Update() {
    string value = GetTextValue();

    if (textGraphic != null) {
      textGraphic.text = prefix + value + postfix;
    }
  }

}
