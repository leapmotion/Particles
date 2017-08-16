using Leap.Unity.Attributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class PaletteSwitcherController : MonoBehaviour {

  public Transform findPaletteControllersWithin;

  public ColorPalette[] palettes;

  [SerializeField, OnEditorChange("curPaletteIdx")]
  private int _curPaletteIdx = 0;
  public int curPaletteIdx {
    get { return _curPaletteIdx; }
    set {
      value = Mathf.Clamp(value, 0, palettes.Length - 1);
      _curPaletteIdx = value;
      setPalettes();
    }
  }

  public ColorPalette curPalette {
    get { return palettes[curPaletteIdx]; }
  }

  private List<GraphicPaletteController> _paletteControllers = new List<GraphicPaletteController>();

  void Start() {
    _paletteControllers.Clear();
    findPaletteControllersWithin.GetComponentsInChildren(true, _paletteControllers);
  }

  private void setPalettes() {
    foreach (var paletteController in _paletteControllers) {
      paletteController.palette = curPalette;
    }
  }

}
