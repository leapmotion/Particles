using Leap.Unity;
using Leap.Unity.Attributes;
using Leap.Unity.GraphicalRenderer;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SocialAttributeVisualization : MonoBehaviour {

  #region Inspector
  
  [Header("Simulator Bindings")]
  public TextureSimulator simulator;
  public TextureSimulatorSetters simulatorSetters;

  public enum VisualizationMode { SocialForces }
  [SerializeField, OnEditorChange("mode")]
  private VisualizationMode _mode = VisualizationMode.SocialForces;
  public VisualizationMode mode {
    get { return _mode; }
    set {
      _mode = value;

      refreshDisplay();
    }
  }
  
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

  [Header("Social Forces")]
  public Mesh attractionMesh;
  public Mesh repulsionMesh;

  [Header("Social Vision")]
  public Mesh visionRangeMesh;

  #endregion

  #region Unity Events

  void Reset() {
    if (simulator == null) {
      simulator = FindObjectOfType<TextureSimulator>();
    }

    if (simulatorSetters == null) {
      simulatorSetters = FindObjectOfType<TextureSimulatorSetters>();
    }

    if (rectTransform == null) {
      rectTransform = GetComponent<RectTransform>();
    }
  }

  void Start() {
    simulator.OnEcosystemEndedTransition += onEcosystemChanged;
  }

  #endregion

  #region Simulation Data

  private TextureSimulator.SimulationDescription _simDescription;
  private TextureSimulator.SocialData[,] _socialData;
  
  private int _numSpecies = 0;
  private int _numSocialAttributes { get { return _numSpecies * _numSpecies; } }

  private void onEcosystemChanged() {
    _simDescription    = simulator.currentSimulationDescription;
    _socialData = simulator.currentSimulationDescription.socialData;

    _numSpecies = (int)(simulatorSetters.GetSpeciesCount());

    refreshDisplay();
  }

  /// <summary>
  /// Gets a normalized value from -1 to 1 indicating how strongly the observer species
  /// index is attracted to the observed species index. -1 indicates maximum repulsion,
  /// 1 indicated maximum attraction.
  /// </summary>
  private float getAttraction(int observingSpeciesIdx, int observedSpeciesIdx) {
    return _socialData[observingSpeciesIdx, observedSpeciesIdx].socialForce
           / simulatorSetters.GetMaxForce();
  }

  #endregion

  #region Display

  private List<LeapMeshGraphic> rowSpeciesGraphics; // N
  private List<LeapMeshGraphic> colSpeciesGraphics; // N
  private List<LeapMeshGraphic> attributeGraphics;  // NxN

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
      rowSpeciesGraphics[i].SetRuntimeTint(speciesColor);
      layoutRowSpeciesGraphic(rowSpeciesGraphics[i], i);

      colSpeciesGraphics[i].SetMesh(speciesLabelMesh);
      colSpeciesGraphics[i].SetRuntimeTint(speciesColor);
      layoutColSpeciesGraphic(colSpeciesGraphics[i], i);
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

          if (attraction > 0f) {
            graphic.SetMesh(attractionMesh);
          }
          else {
            graphic.SetMesh(repulsionMesh);
          }
          graphic.SetRuntimeTint(Color.Lerp(Color.black, Color.white, attraction.Map(-1, 1, 0, 1)));

          graphic.SetBlendShapeAmount(1f - Mathf.Abs(attraction));

          layoutAttributeGraphic(graphic, row, col);
          
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
    Destroy(graphic);
  }

  #endregion

  #region Layout

  int _numRows;
  float _rowWidth;

  int _numCols;
  float _colWidth;

  private void refreshLayout() {
    _numRows = _numSpecies + 1; // labels take up a row
    _rowWidth = rectTransform.rect.height / _numRows;

    _numCols = _numSpecies + 1; // labels take up a column
    _colWidth = rectTransform.rect.width / _numRows;
  }

  private void layoutRowSpeciesGraphic(LeapMeshGraphic rowSpeciesGraphic, int row) {
    fitGraphicToCell(rowSpeciesGraphic, row + 1, 0);
  }

  private void layoutColSpeciesGraphic(LeapMeshGraphic colSpeciesGraphic, int col) {
    fitGraphicToCell(colSpeciesGraphic, 0, col + 1);
  }

  private void layoutAttributeGraphic(LeapMeshGraphic attributeGraphic, int row, int col) {
    fitGraphicToCell(attributeGraphic, row + 1, col + 1);
  }

  private void fitGraphicToCell(LeapMeshGraphic meshGraphic, int row, int col) {
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

  #endregion

}
