using Leap.Unity;
using Leap.Unity.Animation;
using Leap.Unity.Attributes;
using Leap.Unity.GraphicalRenderer;
using Leap.Unity.Query;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SocialAttributeVisualization : MonoBehaviour {

  #region Inspector
  
  [Header("Simulator Bindings")]
  public SimulationManager simManager;
  public SimulatorSetters simulatorSetters;

  public enum VisualizationMode { SocialForces, SocialVision }
  [SerializeField, OnEditorChange("mode")]
  private VisualizationMode _mode = VisualizationMode.SocialForces;
  public VisualizationMode mode {
    get { return _mode; }
    set {
      _mode = value;

      refreshDisplay();
    }
  }

  [Tooltip("Index values receives from the modeController are converted to their "
         + "VisualizationMode enum value, and the visualization will set itself to that "
         + "mode.")]
  public RadioToggleGroup modeController;
  
  [Header("Visualization Bindings")]
  [Tooltip("The rect transform in which to build the visualization.")]
  public RectTransform rectTransform;

  [Tooltip("The text graphic to use to label the vertical axis.")]
  public LeapTextGraphic rowsLabelGraphic;

  [Tooltip("The text graphic to use to label the horizontal axis.")]
  public LeapTextGraphic colsLabelGraphic;

  [Tooltip("A prefab to insantiate as the base LeapMeshGraphic when creating new graphics "
         + "for attributes and labels.")]
  public LeapMeshGraphic meshGraphicPrefab;

  [Tooltip("The mesh to use for LeapMeshGraphics that label species. Species are further "
         + "identified by their runtime tint.")]
  public Mesh speciesLabelMesh;
  public MeshDisplayParams speciesLabelMeshParams;

  [Header("Social Forces")]
  public Mesh attractionMesh;
  public MeshDisplayParams attractionMeshParams;
  public Color attractionColor = Color.white;

  public Mesh repulsionMesh;
  public MeshDisplayParams repulsionMeshParams;
  public Color repulsionColor = Color.black;

  [Header("Social Vision")]
  public Mesh visionRangeMesh;
  public MeshDisplayParams visionRangeMeshParams;

  #endregion

  #region Unity Events

  void Reset() {
    if (simManager == null) {
      simManager = FindObjectOfType<SimulationManager>();
    }

    if (simulatorSetters == null) {
      simulatorSetters = FindObjectOfType<SimulatorSetters>();
    }

    if (rectTransform == null) {
      rectTransform = GetComponent<RectTransform>();
    }
  }

  void Start() {
    simManager.OnEcosystemEndedTransition += onEcosystemChanged;

    modeController.OnIndexToggled += onModeIndexToggled;
  }

  #endregion

  #region Simulation Data

  private EcosystemDescription _simDescription;
  private SocialDescription[,] _socialData;
  private float _effectiveMaxForce;
  private float _effectiveMaxRange;

  private int _numSpecies = 0;
  private int _numSocialAttributes { get { return _numSpecies * _numSpecies; } }

  private void onEcosystemChanged() {
    _simDescription    = simManager.currentDescription;
    _socialData = simManager.currentDescription.socialData;
    _effectiveMaxForce = getEffectiveMaxForce();
    _effectiveMaxRange = getEffectiveMaxRange();

    _numSpecies = (int)(simulatorSetters.GetSpeciesCount());

    refreshDisplay();
  }

  private float getEffectiveMaxForce() {
    if (simManager.currentDescription.isRandomDescription) {
      return simulatorSetters.GetMaxForce();
    }
    else {
      // It's a preset... have to scan all the forces to find the maximum value.
      float maximum = float.NegativeInfinity;
      for (int i = 0; i < _socialData.GetLength(0); i++) {
        for (int j = 0; j < _socialData.GetLength(1); j++) {
          float testForce = Mathf.Abs(_socialData[i, j].socialForce);
          if (testForce > maximum) {
            maximum = testForce;
          }
        }
      }

      return maximum;
    }
  }

  private float getEffectiveMaxRange() {
    if (simManager.currentDescription.isRandomDescription) {
      return simulatorSetters.GetMaxRange();
    }
    else {
      // It's a preset... have to scan all the forces to find the maximum value.
      float maximum = float.NegativeInfinity;
      for (int i = 0; i < _socialData.GetLength(0); i++) {
        for (int j = 0; j < _socialData.GetLength(1); j++) {
          float testForce = _socialData[i, j].socialRange;
          if (testForce > maximum) {
            maximum = testForce;
          }
        }
      }

      return maximum;
    }
  }

  /// <summary>
  /// Gets a normalized value from -1 to 1 indicating how strongly the observer species
  /// index is attracted to the observed species index. -1 indicates maximum repulsion,
  /// 1 indicates maximum attraction.
  /// </summary>
  private float getAttraction(int observingSpeciesIdx, int observedSpeciesIdx) {
    return _socialData[observingSpeciesIdx, observedSpeciesIdx].socialForce
           / _effectiveMaxForce;
  }

  /// <summary>
  /// Gets a normalized value from -1 to 1 indicating how wide the observer's vision 
  /// range is toward the observed species. 0 indicates minimum vision range, 1 indicates
  /// maximum vision range.
  /// </summary>
  private float getVision(int observingSpeciesIdx, int observedSpeciesIdx) {
    return _socialData[observingSpeciesIdx, observedSpeciesIdx].socialRange
           / _effectiveMaxRange;
  }

  #endregion

  #region Display

  private List<LeapMeshGraphic> rowSpeciesGraphics; // N
  private List<LeapMeshGraphic> colSpeciesGraphics; // N
  private List<LeapMeshGraphic> attributeGraphics;  // NxN

  private void onModeIndexToggled(int toggleIndex) {
    mode = (VisualizationMode)toggleIndex;
  }

  private void refreshDisplay() {
    refreshLayout();

    refreshRowColLabelText();
    refreshRowColSpeciesGraphics();

    refreshAttributeGraphics();
  }

  private void refreshRowColLabelText() {
    switch (_mode) {
      case VisualizationMode.SocialForces:
        rowsLabelGraphic.text = "Force Experiencer";
        colsLabelGraphic.text = "Observed Neighbor";
        break;
      case VisualizationMode.SocialVision:
        rowsLabelGraphic.text = "Observer";
        colsLabelGraphic.text = "Observed Neighbor";
        break;
    }
  }

  private void refreshRowColSpeciesGraphics() {
    ensureListExists(ref rowSpeciesGraphics);
    ensureListCount(rowSpeciesGraphics, _numSpecies, createMeshGraphic, deleteMeshGraphic);

    ensureListExists(ref colSpeciesGraphics);
    ensureListCount(colSpeciesGraphics, _numSpecies, createMeshGraphic, deleteMeshGraphic);

    for (int i = 0; i < _numSpecies; i++) {
      Color speciesColor = _simDescription.speciesData[i].color;
      
      rowSpeciesGraphics[i].SetMesh(speciesLabelMesh);
      rowSpeciesGraphics[i].RefreshMeshData();
      rowSpeciesGraphics[i].SetRuntimeTint(speciesColor);
      layoutRowSpeciesGraphic(rowSpeciesGraphics[i], i, speciesLabelMeshParams);

      colSpeciesGraphics[i].SetMesh(speciesLabelMesh);
      colSpeciesGraphics[i].RefreshMeshData();
      colSpeciesGraphics[i].SetRuntimeTint(speciesColor);
      layoutColSpeciesGraphic(colSpeciesGraphics[i], i, speciesLabelMeshParams);
    }
  }

  private void refreshAttributeGraphics() {
    ensureListExists(ref attributeGraphics);
    ensureListCount(attributeGraphics, _numSocialAttributes, createMeshGraphic,
                                                             deleteMeshGraphic);

    for (int k = 0; k < _numSocialAttributes; k++) {
      int row = k / _numSpecies;
      int col = k % _numSpecies;

      var graphic = attributeGraphics[k];
      switch (_mode) {
        case VisualizationMode.SocialForces:
          float attraction = getAttraction(row, col);

          MeshDisplayParams targetParams;
          if (attraction > 0f) {
            graphic.SetMesh(attractionMesh);
            targetParams = attractionMeshParams;
          }
          else {
            graphic.SetMesh(repulsionMesh);
            targetParams = repulsionMeshParams;
          }
          graphic.RefreshMeshData();

          // 0 = max repulsion. 1 = max attraction.
          // The easing curve applied to the value pushes values near 1 closer to 1 and
          // values near 0 closer to 0.
          float attractionRepulsionAmount = Ease.Quadratic.InOut(attraction.Map(-1, 1, 0, 1));

          graphic.SetRuntimeTint(Color.Lerp(repulsionColor, attractionColor, attractionRepulsionAmount));
          graphic.SetBlendShapeAmount(1f - Mathf.Abs(attractionRepulsionAmount.Map(0, 1, -1, 1)));

          layoutAttributeGraphic(graphic, row, col, targetParams);
          
          break;
        case VisualizationMode.SocialVision:
          float socialVisionRange = getVision(row, col);

          graphic.SetMesh(visionRangeMesh);
          graphic.RefreshMeshData();

          graphic.SetRuntimeTint(Color.Lerp(Color.black, Color.white, 0.8f)) ;
          graphic.SetBlendShapeAmount(socialVisionRange.Map(0, 1, 0, 1));

          layoutAttributeGraphic(graphic, row, col, visionRangeMeshParams);

          break;
      }
    }
  }

  private LeapMeshGraphic createMeshGraphic() {
    var meshGraphic = Instantiate(meshGraphicPrefab);
    var obj = meshGraphic.gameObject;
    if (!obj.activeSelf) { obj.SetActive(true); }

    obj.transform.parent = rectTransform;
    obj.transform.localPosition = new Vector3(obj.transform.localPosition.x,
                                              obj.transform.localPosition.y,
                                              0f);
    obj.transform.localScale = Vector3.one;
    obj.transform.localRotation = Quaternion.identity;
    initRectTransform(obj);

    return meshGraphic;
  }

  private void deleteMeshGraphic(LeapGraphic graphic) {
    Destroy(graphic.gameObject);
  }

  #endregion

  #region Layout

  private int _numRows;
  private float _rowWidth;

  private int _numCols;
  private float _colWidth;

  private void refreshLayout() {
    _numRows = _numSpecies + 1; // labels take up a row
    _rowWidth = rectTransform.rect.height / _numRows;

    _numCols = _numSpecies + 1; // labels take up a column
    _colWidth = rectTransform.rect.width / _numCols;
  }

  private void layoutRowSpeciesGraphic(LeapMeshGraphic rowSpeciesGraphic, int row,
                                       MeshDisplayParams? meshParams = null) {
    fitGraphicToCell(rowSpeciesGraphic, row + 1, 0, meshParams);
  }

  private void layoutColSpeciesGraphic(LeapMeshGraphic colSpeciesGraphic, int col,
                                       MeshDisplayParams? meshParams = null) {
    fitGraphicToCell(colSpeciesGraphic, 0, col + 1, meshParams);
  }

  private void layoutAttributeGraphic(LeapMeshGraphic attributeGraphic, int row, int col,
                                      MeshDisplayParams? meshParams = null) {
    fitGraphicToCell(attributeGraphic, row + 1, col + 1, meshParams);
  }

  private void fitGraphicToCell(LeapMeshGraphic meshGraphic, int row, int col,
                                MeshDisplayParams? meshParams) {
    // Invert row order (origin at top-left instead of Unity default bottom-left).
    row = _numRows - 1 - row;

    Vector2 cellMin = new Vector2(col * _colWidth / rectTransform.rect.width,
                                  row * _rowWidth / rectTransform.rect.height);
    Vector2 cellMax = new Vector2((col + 1) * _colWidth / rectTransform.rect.width,
                                  (row + 1) * _rowWidth / rectTransform.rect.height);

    var graphicRect = ensureGraphicHasRect(meshGraphic);
    graphicRect.anchorMin = cellMin;
    graphicRect.anchorMax = cellMax;
    graphicRect.anchoredPosition = Vector2.zero;
    graphicRect.sizeDelta = Vector2.zero;
    
    float boundsWidth = meshGraphic.mesh.bounds.size.CompMax();
    float cellWidth   = (cellMax - cellMin).CompMin();
    float targetScale = cellWidth / boundsWidth * 0.20f;

    Quaternion rotation = Quaternion.identity;
    if (meshParams.HasValue) {
      targetScale *= meshParams.Value.scale;
      rotation = Quaternion.Euler(meshParams.Value.rotation);
    }

    graphicRect.localScale = Vector3.one * targetScale;
    graphicRect.localRotation = rotation;
  }

  private RectTransform ensureGraphicHasRect(LeapGraphic graphic) {
    var rectTransform = graphic.GetComponent<RectTransform>();
    if (rectTransform == null) {
      initRectTransform(graphic.gameObject);
    }
    return rectTransform;
  }

  #endregion

  #region Utilities

  private void ensureListExists<T>(ref List<T> list) {
    if (list == null) {
      list = new List<T>();
    }
  }

  private void ensureListCount<T>(List<T> list, int count, Func<T> createT, Action<T> deleteT) {
    while (list.Count < count) {
      list.Add(createT());
    }

    while (list.Count > count) {
      T tempT = list[list.Count - 1];
      list.RemoveAt(list.Count - 1);
      deleteT(tempT);
    }
  }

  private RectTransform initRectTransform(GameObject obj) {
    var rectTransform = obj.GetComponent<RectTransform>();
    if (rectTransform == null) {
      rectTransform = obj.AddComponent<RectTransform>();
    }

    rectTransform.anchorMin = new Vector2(0f, 0f);
    rectTransform.anchorMax = new Vector2(0.1f, 0.1f);
    rectTransform.anchoredPosition = Vector2.zero;
    rectTransform.sizeDelta = Vector2.zero;
    return rectTransform;
  }

  [System.Serializable]
  public struct MeshDisplayParams {
    public Vector3 rotation;
    public float scale;

    public MeshDisplayParams(Vector3 eulerRotation, float scale) {
      this.rotation = eulerRotation;
      this.scale = scale;
    }
  }

  #endregion

}
