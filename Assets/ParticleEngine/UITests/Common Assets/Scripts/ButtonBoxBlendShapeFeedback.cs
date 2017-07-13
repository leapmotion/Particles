using Leap.Unity;
using Leap.Unity.Attributes;
using Leap.Unity.GraphicalRenderer;
using Leap.Unity.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonBoxBlendShapeFeedback : MonoBehaviour {

  public InteractionButton button;
  public LeapBoxGraphic box;

  [OnEditorChange("setPulsatorSettings")]
  [MinValue(0.0001F)]
  public float restScale = 1F;

  [OnEditorChange("setPulsatorSettings")]
  [MinValue(0.0001F)]
  public float activeScale = 0.98F;

  [OnEditorChange("setPulsatorSettings")]
  [MinValue(0.0001F)]
  public float peakScale = 0.96F;

  [OnEditorChange("setPulsatorSettings")]
  [MinValue(0.001F)]
  public float speed = 20F;

  private Pulsator _scalePulsator;
  private Vector3 _initBoxSize;

  void Reset() {
    button = GetComponent<InteractionButton>();
    box = GetComponent<LeapBoxGraphic>();
  }

  void Awake() {
    box = GetComponent<LeapBoxGraphic>();
    _initBoxSize = box.size;
  }

  void OnEnable() {
    _scalePulsator = Pool<Pulsator>.Spawn();
    setPulsatorSettings();
    _scalePulsator.value = restScale;

    button.OnPress   += onPress;
    button.OnUnpress += onUnpress;
  }

  void Update() {
    try {
      box.SetBlendShapeAmount(_scalePulsator.value);
    }
    catch (System.Exception) { }
    //button.transform.localScale = _scalePulsator.value * Vector3.one;
  }

  private void setPulsatorSettings() {
    if (Application.isPlaying) {
      _scalePulsator.rest = restScale;
      _scalePulsator.active = activeScale;
      _scalePulsator.pulse = peakScale;
      _scalePulsator.speed = speed;
    }
  }

  void OnDisable() {
    Pool<Pulsator>.Recycle(_scalePulsator);

    button.OnPress   -= onPress;
    button.OnUnpress -= onUnpress;
  }

  private void onPress() {
    _scalePulsator.Pulse();
  }

  private void onUnpress() {
    _scalePulsator.Relax();
  }

}
