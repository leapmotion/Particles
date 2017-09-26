using System;
using UnityEngine;

[CreateAssetMenu]
public class RenderPreset : ScriptableObject {

  [Header("Star Coloring")]
  public BlitMode blitMode = BlitMode.Solid;
  public Color baseColor = Color.white;
  public bool enableStarGradient = false;
  public float preScalar = 1;
  public Gradient starRamp;
  public float postScalar = 1;

  [Header("Post Processing")]
  public PostProcessMode postProcessMode;
  public Gradient heatGradient;

  public enum BlitMode {
    Solid,
    BySpeed,
    ByDirection,
    ByAccel,
    ByStartingBlackHole
  }

  public enum PostProcessMode {
    None = 0,
    HeatMap = 1
  }

  [NonSerialized]
  public Texture2D starTex;
  [NonSerialized]
  public Texture2D heatTex;

  void OnValidate() {
    starTex = starRamp.ToTexture();
    heatTex = heatGradient.ToTexture();
  }
}

