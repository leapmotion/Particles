using UnityEngine;
using Leap.Unity;
using Leap.Unity.DevGui;
using Leap.Unity.Attributes;
using UnityEngine.Serialization;

[RequireComponent(typeof(Camera))]
public class GalaxyRenderer : MonoBehaviour {
  private const string BOX_FILTER_KEYWORD = "BOX_FILTER";
  private const string STAR_RAMP_KEYWORD = "USE_RAMP";

  private const string BY_SPEED_KEYWORD = "BY_SPEED";
  private const string BY_DIRECTION_KEYWORD = "BY_DIRECTION";
  private const string BY_ACCEL_KEYWORD = "BY_ACCEL";
  private const string BY_BLACK_HOLE_KEYWORD = "BY_BLACK_HOLE";

  private const string START_TEX_PROPERTY = "_Stars";
  private const string GRADIENT_PROPERTY = "_Gradient";

  private const string GAMMA_PROPERTY = "_Gamma";
  private const string ADJACENT_PROPERTY = "_AdjacentFilter";
  private const string DIAGONAL_PROPERTY = "_DiagonalFilter";

  [SerializeField]
  private GalaxySimulation _sim;

  [SerializeField]
  private Transform _displayAnchor;

  [DevCategory("General Settings")]
  [Range(0.01f, 2f)]
  [SerializeField, DevValue]
  private float _scale = 1;

  [SerializeField, DevValue]
  private float _forwardOffset = 0;

  [Header("Black Hole Rendering"), DevCategory]
  [SerializeField, DevValue("Render")]
  private bool _renderBlackHoles = true;

  [Range(0, 1)]
  [SerializeField, DevValue("Size")]
  private float _blackHoleSize = 0.05f;

  [SerializeField]
  private Mesh _blackHoleMesh;

  [SerializeField]
  private Material _blackHoleMat;

  [Header("Star Rendering"), DevCategory]
  [SerializeField, DevValue]
  private bool _renderStars = true;

  [Range(0, 0.05f)]
  [FormerlySerializedAs("starSize")]
  [SerializeField, DevValue]
  private float _starSize;

  [Range(0, 1)]
  [FormerlySerializedAs("starBrightness")]
  [SerializeField, DevValue]
  private float _starBrightness;

  [Disable]
  [SerializeField]
  private RenderType _renderType;

  [SerializeField]
  private Material _pointMat;

  [SerializeField]
  private Material _quadMat;

  [SerializeField]
  private Material _lightMat;


  [Header("Render Presets")]
  [SerializeField]
  private RenderPreset preset;

  [SerializeField]
  private Material _postProcessMat;

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

  public float scale {
    get {
      return _scale;
    }
    set {
      _scale = value;
    }
  }

  public enum RenderType {
    Point,
    Quad,
    PointBright
  }

  public void SetPreset(RenderPreset preset) {
    this.preset = preset;
    uploadGradientTextures();
  }

  private void OnValidate() {
    uploadGradientTextures();
  }

  private void OnEnable() {
    _myCamera = GetComponent<Camera>();
    Camera.onPostRender += drawCamera;

    uploadGradientTextures();
  }

  private void OnDisable() {
    Camera.onPostRender -= drawCamera;
  }

  private void LateUpdate() {
    //_displayAnchor.localScale = _scale * Vector3.one;
    //_displayAnchor.localPosition = Vector3.forward * _forwardOffset * _scale;
  }

  public void UpdatePositions(Texture currPosition, Texture prevPosition, Texture lastPosition) {
    _currPosition = currPosition;
    _prevPosition = prevPosition;
    _lastPosition = lastPosition;
  }

  public void DrawBlackHole(Vector3 position) {
    if (_renderBlackHoles) {
      _blackHoleMat.SetColor("_Color", preset.baseColor);

      Graphics.DrawMesh(_blackHoleMesh,
                        _displayAnchor.localToWorldMatrix * Matrix4x4.TRS(position, Quaternion.identity, Vector3.one * _blackHoleSize),
                        _blackHoleMat,
                        0);
    }
  }

  private void OnRenderImage(RenderTexture source, RenderTexture destination) {
    if (!_renderStars) {
      Graphics.Blit(source, destination);
      return;
    }

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
    Graphics.Blit(source, destination, _postProcessMat, (int)preset.postProcessMode);

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
    mat.DisableKeyword(BY_ACCEL_KEYWORD);
    mat.DisableKeyword(BY_BLACK_HOLE_KEYWORD);
    switch (preset.blitMode) {
      case RenderPreset.BlitMode.BySpeed:
        mat.EnableKeyword(BY_SPEED_KEYWORD);
        break;
      case RenderPreset.BlitMode.ByDirection:
        mat.EnableKeyword(BY_DIRECTION_KEYWORD);
        break;
      case RenderPreset.BlitMode.ByAccel:
        mat.EnableKeyword(BY_ACCEL_KEYWORD);
        break;
      case RenderPreset.BlitMode.ByStartingBlackHole:
        mat.EnableKeyword(BY_BLACK_HOLE_KEYWORD);
        break;
    }

    if (preset.enableStarGradient) {
      mat.EnableKeyword(STAR_RAMP_KEYWORD);
    } else {
      mat.DisableKeyword(STAR_RAMP_KEYWORD);
    }

    mat.mainTexture = _currPosition;
    mat.SetTexture("_PrevPosition", _prevPosition);
    mat.SetTexture("_LastPosition", _lastPosition);

    switch (preset.blitMode) {
      case RenderPreset.BlitMode.BySpeed:
        mat.SetFloat("_PreScalar", preset.preScalar / _sim.timestep);
        break;
      case RenderPreset.BlitMode.ByAccel:
        mat.SetFloat("_PreScalar", preset.preScalar / _sim.timestep / _sim.timestep);
        break;
      default:
        mat.SetFloat("_PreScalar", preset.preScalar);
        break;
    }

    mat.SetFloat("_PostScalar", preset.postScalar);

    mat.SetMatrix("_ToWorldMat", _displayAnchor.localToWorldMatrix);
    mat.SetFloat("_Scale", _displayAnchor.lossyScale.x);
    mat.SetFloat("_Size", _starSize);
    mat.SetFloat("_Bright", _starBrightness);

#if UNITY_EDITOR
    uploadGradientTextures();
#endif

    if (_renderStars) {
      mat.SetPass(0);
      Graphics.DrawProcedural(MeshTopology.Points, _currPosition.width * _currPosition.height);
    }
  }

  private void uploadGradientTextures() {
    _postProcessMat.SetTexture(GRADIENT_PROPERTY, preset.heatTex);

    var starTex = preset.starTex;
    _pointMat.SetTexture("_Ramp", starTex);
    _quadMat.SetTexture("_Ramp", starTex);
    _lightMat.SetTexture("_Ramp", starTex);
  }
}
