using Leap.Unity.GraphicalRenderer;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraphicPaletteController : MonoBehaviour {

  public LeapGraphic graphic;

  public ColorPalette palette;
  public float colorChangeSpeed = 20F;

  public static ColorPalette s_lastPalette;

  private bool _paletteWasNull = true;
  private Color _targetColor;

  [Header("Palette Colors")]

  public int restingColorIdx;
  public Color restingColor { get { return palette[restingColorIdx]; } }

  // State tracking (optimization)
  private Color _lastFrameTargetColor;

  protected virtual void Reset() {
    graphic = GetComponent<LeapGraphic>();

    if (palette == null && s_lastPalette != null) {
      palette = s_lastPalette;
    }
  }

  protected virtual void OnValidate() {
    if (palette != null) {
      if (_paletteWasNull) {
        _paletteWasNull = false;
        s_lastPalette = palette;
      }
      validateColorIdx(ref restingColorIdx);
      setColor(restingColor);
    }
  }

  protected void validateColorIdx(ref int colorIdx) {
    colorIdx = Mathf.Max(0, Mathf.Min(palette.colors.Length - 1, colorIdx));
  }

  protected virtual void Update() {
    if (palette == null) return;
    if (graphic == null) return;

    if (!Application.isPlaying) {
      setColor(restingColorIdx);
      return;
    }

    _targetColor = updateTargetColor();

    if (_lastFrameTargetColor != _targetColor) {
      Color curColor = getColor();
      setColor(Color.Lerp(curColor, _targetColor, colorChangeSpeed * Time.deltaTime));
    }

    _lastFrameTargetColor = _targetColor;
  }

  protected virtual Color updateTargetColor() {
    return palette[restingColorIdx];
  }

  protected Color getColor() {
    var text = graphic as LeapTextGraphic;
    if (text != null) {
      return text.color;
    }
    else {
      return graphic.GetRuntimeTint();
    }
  }

  private void setColor(int colorIdx) {
    setColor(palette[colorIdx]);
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
