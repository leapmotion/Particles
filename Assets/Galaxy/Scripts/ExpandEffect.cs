using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExpandEffect : MonoBehaviour {

  public Material expandMat;
  public bool doit;

  private void OnRenderImage(RenderTexture source, RenderTexture destination) {
    FindObjectOfType<GalaxySimulation>().DrawStars(null);

    if (doit) {
      Graphics.Blit(source, destination, expandMat);
    } else {
      Graphics.Blit(source, destination);
    }
  }
}
