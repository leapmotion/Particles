using UnityEngine;
using Leap.Unity.Interaction;
using Leap.Unity.GraphicalRenderer;

[RequireComponent(typeof(InteractionButton))]
public class ButtonHighlight : MonoBehaviour {

  public LeapGraphic graphic;
  public Color baseColor;
  public Color primaryHoverColor;
  public Color pressColor;

  private LeapRuntimeTintData _tint;
  private InteractionButton _button;

  void Start() {
    _tint = graphic.GetFeatureData<LeapRuntimeTintData>();
    _button = GetComponent<InteractionButton>();
  }

  void Update() {
    if (_button.isPressed) {
      _tint.color = pressColor;
    } else if (_button.isPrimaryHovered) {
      _tint.color = primaryHoverColor;
    } else
      _tint.color = baseColor;
  }
}
