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
  public float colorChangeSpeed = 10F;

  [Header("State Colors")]
  public int restingColorIdx;
  public int primaryHoverColorIdx;
  public int pressedColorIdx;

  public static ColorPalette s_lastPalette;

  private bool _paletteWasNull = true;
  private Color _targetColor;
  private Pulsator _pressPulsator;

  public Color restingColor { get { return palette[restingColorIdx]; } }
  public Color primaryHoverColor { get { return palette[primaryHoverColorIdx]; } }
  public Color pressedColor { get { return palette[pressedColorIdx]; } }

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
      }
    }
  }

  void OnEnable() {
    button.OnPress   += onPress;
    button.OnUnpress += onUnpress;
  }

  void OnDisable() {
    button.OnPress   -= onPress;
    button.OnUnpress -= onUnpress;
  }

  private void onPress() { _pressPulsator.Pulse(); }
  private void onUnpress() { _pressPulsator.Relax(); }

  void Start() {
    _pressPulsator = Pulsator.Spawn().SetValues(0F, 1F, 1.2F).SetSpeed(20F);
  }

  void OnDestroy() {
    if (_pressPulsator != null) {
      Pulsator.Recycle(_pressPulsator);
    }
  }

  void Update() {
    if (palette == null) return;
    if (graphic == null) return;

    if (!Application.isPlaying) {
      setColor(restingColor);
      return;
    }

    _targetColor = restingColor;
    if (!_pressPulsator.isResting) {
      if (_pressPulsator.value < 1.0F) {
        _targetColor = Color.Lerp(restingColor, pressedColor, _pressPulsator.value);
      }
      else {
        _targetColor = Color.Lerp(pressedColor, restingColor, _pressPulsator.value - 1F);
      }
    }
    else if (button.isPrimaryHovered) {
      _targetColor = palette[primaryHoverColorIdx];
    }

    Color curColor = getColor();
    setColor(Color.Lerp(curColor, _targetColor, colorChangeSpeed * Time.deltaTime));
  }

  private Color getColor() {
    var text = graphic as LeapTextGraphic;
    if (text != null) {
      return text.color;
    }
    else {
      return graphic.GetRuntimeTint();
    }
  }

  private void setColor(Color color) {
    var text = graphic as LeapTextGraphic;
    if (text != null) {
      text.color = color;
    }
    else {
      graphic.SetRuntimeTint(color);
    }
  }

}
