using Leap.Unity;
using Leap.Unity.Attributes;
using Leap.Unity.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractionButtonScaleFeedback : MonoBehaviour {

  public InteractionButton button;

  [OnEditorChange("setPulsatorSettings")]
  [MinValue(0.0001F)]
  public float restScale = 1F;

  [OnEditorChange("setPulsatorSettings")]
  [MinValue(0.0001F)]
  public float activeScale = 0.95F;

  [OnEditorChange("setPulsatorSettings")]
  [MinValue(0.0001F)]
  public float peakScale = 0.9F;

  [OnEditorChange("setPulsatorSettings")]
  [MinValue(0.001F)]
  public float speed = 0.02F;

  private Pulsator _scalePulsator;

  void Reset() {
    button = GetComponent<InteractionButton>();
  }

  void OnEnable() {
    _scalePulsator = Pool<Pulsator>.Spawn();
    setPulsatorSettings();
    _scalePulsator.value = restScale;

    button.OnPress   += onPress;
    button.OnUnpress += onUnpress;
  }

  void Update() {
    button.transform.localScale = _scalePulsator.value * Vector3.one;
  }

  private void setPulsatorSettings() {
    if (Application.isPlaying) {
      _scalePulsator.rest = restScale;
      _scalePulsator.active = activeScale;
      _scalePulsator.peak = peakScale;
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
