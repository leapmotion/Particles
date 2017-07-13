using Leap.Unity;
using Leap.Unity.GraphicalRenderer;
using Leap.Unity.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class LeapGraphicButtonPaletteController : MonoBehaviour {

  public LeapGraphic graphic;
  public InteractionButton button;

  public ColorPalette palette;
  public float colorChangeSpeed = 0.4F;

  [Header("State Colors")]
  public int restingColorIdx;
  public int pressedColorIdx;

  public static ColorPalette s_lastPalette;

  private bool _paletteWasNull = true;
  private Pulsator _pressPulsator;
  private Color _curColor;

  void Reset() {
    graphic = GetComponent<LeapGraphic>();
    button = GetComponent<InteractionButton>();

    if (palette == null && s_lastPalette != null) {
      palette = s_lastPalette;
    }
  }

  void OnValidate() {
    if (palette != null) {
      if (_paletteWasNull) {
        _paletteWasNull = false;
        s_lastPalette = palette;
      }

      if (palette.colors.Length != 0) {
        restingColorIdx = Mathf.Max(0, Mathf.Min(palette.colors.Length - 1, restingColorIdx));
        pressedColorIdx = Mathf.Max(0, Mathf.Min(palette.colors.Length - 1, pressedColorIdx));

        refreshColor();
      }
    }
  }

  void OnEnable() {
    if (Application.isPlaying) {
      if (_pressPulsator == null) {
        _pressPulsator = Pool<Pulsator>.Spawn();
      }
      Debug.Log(_pressPulsator);
      initPulsatorSettings();
      Debug.Log("Hello");

      _curColor = palette[restingColorIdx];

      button.OnPress += onPress;
      button.OnUnpress += onUnpress;
    }
  }

  private void initPulsatorSettings() {
    _pressPulsator.speed = colorChangeSpeed;
  }

  void OnDisable() {
    if (_pressPulsator != null) {
      Pool<Pulsator>.Recycle(_pressPulsator);
    }

    button.OnPress   -= onPress;
    button.OnUnpress -= onUnpress;
  }

  private void onPress() {
    _pressPulsator.Pulse();
  }

  private void onUnpress() {
    _pressPulsator.Relax();
  }

  void Update() {
    if (palette != null) {
      if (!Application.isPlaying) {
        _curColor = palette[restingColorIdx];
      }
      else {
        _curColor = Color.Lerp(palette[restingColorIdx], palette[pressedColorIdx], _pressPulsator.value);
      }
    }

    refreshColor();
  }

  private void refreshColor() {
    if (palette == null) return;
    if (graphic == null) return;

    Color color = _curColor;

    var text = graphic as LeapTextGraphic;
    if (text != null) {
      text.color = color;
    }
    else {
      graphic.SetRuntimeTint(color);
    }
  }

}
