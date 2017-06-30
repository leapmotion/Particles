using Leap.Unity.GraphicalRenderer;
using Leap.Unity.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class InteractionSliderGraphicController : MonoBehaviour {

  public InteractionSlider slider;

  [Header("Slider Panel")]
  public LeapGraphic panelGraphic;
  public Color panelGraphicColor;
  public Color panelDepressedColor;
  public Color panelDisabledColor;
  public Color panelTint;

  [Header("Slider")]
  public LeapGraphic sliderGraphic;
  public Color sliderGraphicColor;
  public Color sliderDepressedColor;
  public Color sliderDisabledColor;
  public Color sliderTint;

  void Reset() {
    if (slider == null) {
      slider = GetComponent<InteractionSlider>();
    }
    if (sliderGraphic == null) {
      sliderGraphic = slider.GetComponent<LeapGraphic>();
      if (sliderGraphic == null) {
        sliderGraphic = slider.GetComponentInChildren<LeapGraphic>();
      }
    }
    if (panelGraphic == null && slider.transform.parent != null) {
      panelGraphic = slider.transform.parent.GetComponentInParent<LeapGraphic>();
    }

    panelGraphicColor = new Color(0.86F, 0.86F, 0.86F);
    panelDepressedColor = new Color(0.86F, 0.86F, 0.86F);
    panelDisabledColor = new Color(0.60F, 0.60F, 0.60F);
    panelTint = Color.white;

    sliderGraphicColor = new Color(1.00F, 1.00F, 1.00F);
    sliderDepressedColor = new Color(0.60F, 0.60F, 0.60F);
    sliderDisabledColor = new Color(0.80F, 0.80F, 0.80F);
    sliderTint = Color.white;
  }

  void Update() {
    if (slider != null) {
      bool sliderEnabled = slider.controlEnabled;
      
      if (panelGraphic != null) {
        Color targetPanelColor = panelGraphicColor;
        if (!sliderEnabled) targetPanelColor = panelDisabledColor;
        targetPanelColor = targetPanelColor.Multiply(panelTint);
        if (!sliderEnabled) targetPanelColor = Color.Lerp(targetPanelColor, panelDisabledColor, 0.5F);

        panelGraphic.SetRuntimeTint(targetPanelColor);
      }
      
      if (sliderGraphic != null) {
        Color targetSliderColor = sliderGraphicColor;
        if (!sliderEnabled) targetSliderColor = sliderDisabledColor;
        targetSliderColor = targetSliderColor.Multiply(sliderTint);
        if (!sliderEnabled) targetSliderColor = Color.Lerp(targetSliderColor, sliderDisabledColor, 0.5F);

        sliderGraphic.SetRuntimeTint(targetSliderColor);
      }
    }
  }

  public void SetPanelTint(string htmlString) {
    panelTint = parseColorString(htmlString);
  }

  public void SetSliderTint(string htmlString) {
    sliderTint = parseColorString(htmlString);
  }

  private Color parseColorString(string htmlString) {
    Color c = Color.white;
    if (!ColorUtility.TryParseHtmlString(htmlString, out c)) {
      Debug.LogError("Couldn't parse html string for color: " + htmlString, this);
    }
    return c;
  }

}

public static class ColorExtensions {

  public static Color Multiply(this Color A, Color B) {
    return new Color(A.r * B.r, A.g * B.g, A.b * B.b);
  }

}