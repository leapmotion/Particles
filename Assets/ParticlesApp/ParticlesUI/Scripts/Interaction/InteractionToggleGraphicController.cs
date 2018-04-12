using Leap.Unity.GraphicalRenderer;
using Leap.Unity.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class InteractionToggleGraphicController : MonoBehaviour {

  public InteractionToggle toggle;

  [Header("Colors")]
  public LeapGraphic toggleGraphic;
  public Color untoggledColor;
  public Color depressedColor;
  public Color toggledColor;
  public Color disabledColor;
  public Color toggleTint;

  void Reset() {
    if (toggle == null) {
      toggle = GetComponent<InteractionToggle>();
    }
    if (toggleGraphic == null) {
      toggleGraphic = toggle.GetComponent<LeapGraphic>();
      if (toggleGraphic == null) {
        toggleGraphic = toggle.GetComponentInChildren<LeapGraphic>();
      }
    }

    untoggledColor = new Color(1.00F, 1.00F, 1.00F);
    depressedColor = new Color(0.60F, 0.60F, 0.60F);
    toggledColor = new Color(0.75F, 0.75F, 0.75F);
    disabledColor = new Color(0.80F, 0.80F, 0.80F);
    toggleTint = Color.white;
  }

  void Update() {
    if (toggle != null) {
      bool toggleEnabled = toggle.controlEnabled;

      if (toggleGraphic != null) {
        Color targetToggleColor = toggle.isToggled ? toggledColor : untoggledColor;
        if (toggle.isDepressed) targetToggleColor = depressedColor;
        if (!toggleEnabled) targetToggleColor = disabledColor;
        targetToggleColor = targetToggleColor.Multiply(toggleTint);
        if (!toggleEnabled) targetToggleColor = Color.Lerp(targetToggleColor, disabledColor, 0.5F);

        toggleGraphic.SetRuntimeTint(targetToggleColor);
      }
    }
  }

  public void SetToggleTint(string htmlString) {
    toggleTint = parseColorString(htmlString);
  }

  private Color parseColorString(string htmlString) {
    Color c = Color.white;
    if (!ColorUtility.TryParseHtmlString(htmlString, out c)) {
      Debug.LogError("Couldn't parse html string for color: " + htmlString, this);
    }
    return c;
  }

}