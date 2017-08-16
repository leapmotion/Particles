using Leap.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Leap.Unity.Query;
using Leap.Unity.GraphicalRenderer;

public class ConvertAllLGPCToGPC : MonoBehaviour {

  [MenuItem("One-Time/Convert LGPC to GPC")]
  private static void Convert() {
    List<LeapGraphicPaletteController> lgpcs = new List<LeapGraphicPaletteController>();
    foreach (var lgpcArr in EditorSceneManager
                         .GetActiveScene()
                         .GetRootGameObjects()
                         .Query()
                         .Select(g => g.GetComponentsInChildren<LeapGraphicPaletteController>(true))) {
      foreach (var lgpc in lgpcArr) {
        lgpcs.Add(lgpc);
      }
    };

    foreach (var lgpc in lgpcs) {
      ConvertLGPC(lgpc);
    }

    Debug.Log("Converted!");
  }

  private static void ConvertLGPC(LeapGraphicPaletteController lgpc) {
    GameObject obj = lgpc.gameObject;

    LeapGraphic graphic = lgpc.graphic;
    ColorPalette palette = lgpc.palette;
    int idx = lgpc.colorIndex;

    Destroy(lgpc);

    var gpc = obj.AddComponent<GraphicPaletteController>();
    gpc.graphic = graphic;
    gpc.palette = palette;
    gpc.restingColorIdx = idx;
  }

}
