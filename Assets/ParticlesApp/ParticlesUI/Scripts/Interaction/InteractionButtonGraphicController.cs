using Leap.Unity.GraphicalRenderer;
using Leap.Unity.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class InteractionButtonGraphicController : MonoBehaviour {

  public InteractionButton button;

  [Header("Colors")]
  public LeapGraphic buttonGraphic;
  public Color buttonGraphicColor;
  public Color buttonDepressedColor;
  public Color buttonDisabledColor;
  public Color buttonTint;

  void Reset() {
    if (button == null) {
      button = GetComponent<InteractionButton>();
    }
    if (buttonGraphic == null) {
      buttonGraphic = button.GetComponent<LeapGraphic>();
      if (buttonGraphic == null) {
        buttonGraphic = button.GetComponentInChildren<LeapGraphic>();
      }
    }

    buttonGraphicColor = new Color(1.00F, 1.00F, 1.00F);
    buttonDepressedColor = new Color(0.60F, 0.60F, 0.60F);
    buttonDisabledColor = new Color(0.80F, 0.80F, 0.80F);
    buttonTint = Color.white;
  }

  void Update() {
    if (button != null) {
      bool buttonEnabled = button.controlEnabled;

      if (buttonGraphic != null) {
        Color targetButtonColor = buttonGraphicColor;
        if (button.isPressed) targetButtonColor = buttonDepressedColor;
        if (!buttonEnabled) targetButtonColor = buttonDisabledColor;
        targetButtonColor = targetButtonColor.Multiply(buttonTint);
        if (!buttonEnabled) targetButtonColor = Color.Lerp(targetButtonColor, buttonDisabledColor, 0.5F);

        buttonGraphic.SetRuntimeTint(targetButtonColor);
      }
    }
  }

  public void SetButtonTint(string htmlString) {
    buttonTint = parseColorString(htmlString);
  }

  private Color parseColorString(string htmlString) {
    Color c = Color.white;
    if (!ColorUtility.TryParseHtmlString(htmlString, out c)) {
      Debug.LogError("Couldn't parse html string for color: " + htmlString, this);
    }
    return c;
  }

}