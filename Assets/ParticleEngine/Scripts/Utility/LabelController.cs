using System;
using UnityEngine;
using Leap.Unity.Attributes;
using Leap.Unity.GraphicalRenderer;

public class LabelController : MonoBehaviour {

  [MinValue(0)]
  [SerializeField]
  private int _decimalPlaces;

  [SerializeField]
  private float _scale = 1;

  private string _originalString;

  private void Awake() {
    _originalString = GetComponent<LeapTextGraphic>().text;
    int index = _originalString.IndexOf('#');
    _originalString = _originalString.Replace("#", "");
    _originalString = _originalString.Insert(index, "#");
  }

  public void SetLabel(float value) {
    SetLabel(Math.Round(value * _scale, _decimalPlaces).ToString());
  }

  public void SetLabel(string value) {
    GetComponent<LeapTextGraphic>().text = _originalString.Replace("#", value);
  }
}
