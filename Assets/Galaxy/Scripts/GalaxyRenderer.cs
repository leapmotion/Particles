using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Leap.Unity.DevGui;
using UnityEngine.Serialization;

[RequireComponent(typeof(Camera))]
public class GalaxyRenderer : MonoBehaviour {
  private const int PASS_NONE = 0;

  [Header("Black Hole Rendering"), DevCategory]
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
  private Color _solidColor = Color.white;

  [Header("Post Processing"), DevCategory]
  [SerializeField, DevValue]
  private PostProcessMode _postProcessMode;

  [SerializeField]
  private Material _postProcessMat;

  [SerializeField, DevValue]
  private bool _enableBoxFilter = true;

  [Range(0, 1)]
  [SerializeField, DevValue]
  private float _adjacentFilter = 0.75f;

  [Range(0, 1)]
  [SerializeField, DevValue]
  private float _diagonalFilter = 0.5f;

  private Camera _myCamera;
  private Texture _position;

  public enum RenderType {
    Point,
    Quad,
    PointBright
  }

  public enum ColorMode {
    Solid
  }

  public enum PostProcessMode {
    None
  }

  private void OnEnable() {
    _myCamera = GetComponent<Camera>();
    Camera.onPostRender += drawCamera;
  }

  private void OnDisable() {
    Camera.onPostRender -= drawCamera;
  }

  public void UpdatePositions(Texture position) {
    _position = position;
  }

  public void DrawBlackHole(Vector3 position) {
    if (_renderBlackHoles) {
      Graphics.DrawMesh(_blackHoleMesh, Matrix4x4.Scale(Vector3.one * _scale) * Matrix4x4.TRS(position, Quaternion.identity, Vector3.one * 0.01f), _blackHoleMat, 0);
    }
  }

  private void OnRenderImage(RenderTexture source, RenderTexture destination) {
    RenderTexture tex = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear, 1, RenderTextureMemoryless.Color);

    Graphics.SetRenderTarget(tex.colorBuffer, source.depthBuffer);

    drawStars();

    _postProcessMat.SetTexture("_Stars", tex);
    Graphics.Blit(source, destination, _postProcessMat);

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

    mat.mainTexture = _position;
    mat.SetFloat("_Scale", _scale);
    mat.SetFloat("_Size", _starSize);
    mat.SetFloat("_Bright", _starBrightness);
    mat.SetPass(0);

    Graphics.DrawProcedural(MeshTopology.Points, _position.width * _position.height);
  }
}
