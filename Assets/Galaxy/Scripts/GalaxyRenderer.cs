using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Leap.Unity.DevGui;
using UnityEngine.Serialization;

[RequireComponent(typeof(Camera))]
public class GalaxyRenderer : MonoBehaviour {
  private const string BOX_FILTER_KEYWORD = "BOX_FILTER";

  private const string BY_SPEED_KEYWORD = "BY_SPEED";
  private const string BY_DIRECTION_KEYWORD = "BY_DIRECTION";

  private const string START_TEX_PROPERTY = "_Stars";
  private const string GRADIENT_PROPERTY = "_Gradient";

  private const string GAMMA_PROPERTY = "_Gamma";
  private const string ADJACENT_PROPERTY = "_AdjacentFilter";
  private const string DIAGONAL_PROPERTY = "_DiagonalFilter";

  [Header("Black Holes"), DevCategory]
  [SerializeField, DevValue]
  private bool _renderBlackHoles = true;

  [SerializeField]
  private Mesh _blackHoleMesh;

  [SerializeField]
  private Material _blackHoleMat;

  [Header("Star Rendering"), DevCategory]
  [Range(0.05f, 2f)]
  [FormerlySerializedAs("scale")]
  [SerializeField, DevValue]
  private float _scale;

  [Range(0, 0.05f)]
  [FormerlySerializedAs("starSize")]
  [SerializeField, DevValue]
  private float _starSize;

  [Range(0, 1)]
  [FormerlySerializedAs("starBrightness")]
  [SerializeField, DevValue]
  private float _starBrightness;

  [SerializeField, DevValue]
  private RenderType _renderType;

  [SerializeField]
  private Material _pointMat;

  [SerializeField]
  private Material _quadMat;

  [SerializeField]
  private Material _lightMat;

  [Header("Star Coloring"), DevCategory]
  [SerializeField, DevValue]
  private ColorMode _colorMode = ColorMode.Solid;

  [SerializeField]
  private Color _starColor = Color.white;

  [SerializeField]
  private float _speedScalar = 1;

  [Header("Post Processing"), DevCategory]
  [SerializeField, DevValue]
  private PostProcessMode _postProcessMode;

  [SerializeField]
  private Material _postProcessMat;

  [SerializeField]
  private Gradient _heatGradient;

  [Range(0, 2)]
  [SerializeField, DevValue]
  private float _gammaValue = 0.3f;

  [SerializeField, DevValue]
  private bool _enableBoxFilter = true;

  [Range(0, 1)]
  [SerializeField, DevValue]
  private float _adjacentFilter = 0.75f;

  [Range(0, 1)]
  [SerializeField, DevValue]
  private float _diagonalFilter = 0.5f;

  private Camera _myCamera;
  private Texture _currPosition;
  private Texture _prevPosition;
  private Texture _lastPosition;

  public enum RenderType {
    Point,
    Quad,
    PointBright
  }

  public enum ColorMode {
    Solid,
    BySpeed,
    ByDirection
  }

  public enum PostProcessMode {
    None = 0,
    HeatMap = 1
  }

  private void OnValidate() {
    uploadGradientTexture();
  }

  private void OnEnable() {
    _myCamera = GetComponent<Camera>();
    Camera.onPostRender += drawCamera;

    uploadGradientTexture();
  }

  private void OnDisable() {
    Camera.onPostRender -= drawCamera;
  }

  public void UpdatePositions(Texture currPosition, Texture prevPosition, Texture lastPosition) {
    _currPosition = currPosition;
    _prevPosition = prevPosition;
    _lastPosition = lastPosition;
  }

  public void DrawBlackHole(Vector3 position) {
    if (_renderBlackHoles) {
      _blackHoleMat.SetColor("_Color", _starColor);

      Graphics.DrawMesh(_blackHoleMesh,
                        Matrix4x4.Scale(Vector3.one * _scale) * Matrix4x4.TRS(position, Quaternion.identity, Vector3.one * 0.01f),
                        _blackHoleMat,
                        0);
    }
  }

  private void OnRenderImage(RenderTexture source, RenderTexture destination) {
    RenderTexture tex = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear, 1);

    Graphics.SetRenderTarget(tex.colorBuffer, source.depthBuffer);
    GL.Clear(clearDepth: false, clearColor: true, backgroundColor: Color.black);

    drawStars();

    if (_enableBoxFilter) {
      _postProcessMat.EnableKeyword(BOX_FILTER_KEYWORD);
      _postProcessMat.SetFloat(ADJACENT_PROPERTY, _adjacentFilter);
      _postProcessMat.SetFloat(DIAGONAL_PROPERTY, _diagonalFilter);
    } else {
      _postProcessMat.DisableKeyword(BOX_FILTER_KEYWORD);
    }

    _postProcessMat.SetFloat(GAMMA_PROPERTY, _gammaValue);

    _postProcessMat.SetTexture(START_TEX_PROPERTY, tex);
    Graphics.Blit(source, destination, _postProcessMat, (int)_postProcessMode);

    RenderTexture.ReleaseTemporary(tex);
  }

  private void drawCamera(Camera camera) {
    if (_myCamera == camera) {
      return;
    }

    drawStars();
  }

  private void drawStars() {
    Material mat = null;

    switch (_renderType) {
      case RenderType.Point:
        mat = _pointMat;
        break;
      case RenderType.Quad:
        mat = _quadMat;
        break;
      case RenderType.PointBright:
        mat = _lightMat;
        break;
    }

    mat.DisableKeyword(BY_SPEED_KEYWORD);
    mat.DisableKeyword(BY_DIRECTION_KEYWORD);
    switch (_colorMode) {
      case ColorMode.BySpeed:
        mat.EnableKeyword(BY_SPEED_KEYWORD);
        break;
      case ColorMode.ByDirection:
        mat.EnableKeyword(BY_DIRECTION_KEYWORD);
        break;
    }

    mat.mainTexture = _currPosition;
    mat.SetTexture("_PrevPosition", _prevPosition);

    mat.SetFloat("_SpeedScalar", _speedScalar);
    mat.SetFloat("_Scale", _scale);
    mat.SetFloat("_Size", _starSize);
    mat.SetFloat("_Bright", _starBrightness);
    mat.SetPass(0);

    Graphics.DrawProcedural(MeshTopology.Points, _currPosition.width * _currPosition.height);
  }

  private void uploadGradientTexture() {
    Texture2D tex = new Texture2D(256, 1, TextureFormat.ARGB32, mipmap: false, linear: true);
    tex.filterMode = FilterMode.Bilinear;
    tex.wrapMode = TextureWrapMode.Clamp;

    for (int i = 0; i < tex.width; i++) {
      float t = i / (tex.width - 1.0f);
      tex.SetPixel(i, 0, _heatGradient.Evaluate(t));
    }
    tex.Apply();

    _postProcessMat.SetTexture(GRADIENT_PROPERTY, tex);
  }
}
