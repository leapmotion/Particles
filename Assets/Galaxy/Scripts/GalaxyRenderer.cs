using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Leap.Unity.DevGui;

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
  [SerializeField, DevValue]
  private float scale;

  [Range(0, 0.05f)]
  [SerializeField, DevValue]
  private float starSize;

  [Range(0, 1)]
  [SerializeField, DevValue]
  private float starBrightness;

  [SerializeField, DevValue]
  private RenderType renderType;

  [SerializeField]
  private Material pointMat;

  [SerializeField]
  private Material quadMat;

  [SerializeField]
  private Material lightMat;

  [Header("Post Processing"), DevCategory]
  private Material postProcessMat;

  [SerializeField, DevValue]
  private ColorMode colorMode = ColorMode.None;

  [SerializeField, DevValue]
  private bool enableBoxFilter = true;

  [Range(0, 1)]
  [SerializeField, DevValue]
  private float adjacentFilter = 0.75f;

  [Range(0, 1)]
  [SerializeField, DevValue]
  private float diagonalFilter = 0.5f;

  private Camera _myCamera;
  private Texture _position;

  public enum RenderType {
    Point,
    Quad,
    PointBright
  }

  public enum ColorMode {
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
      Graphics.DrawMesh(_blackHoleMesh, Matrix4x4.Scale(Vector3.one * scale) * Matrix4x4.TRS(position, Quaternion.identity, Vector3.one * 0.01f), _blackHoleMat, 0);
    }
  }

  private void OnRenderImage(RenderTexture source, RenderTexture destination) {
    RenderTexture tex = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear, 1, RenderTextureMemoryless.Color);

    Graphics.SetRenderTarget(tex.colorBuffer, source.depthBuffer);

    drawStars();

    postProcessMat.SetTexture("_Stars", tex);
    Graphics.Blit(source, destination, postProcessMat);

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

    switch (renderType) {
      case RenderType.Point:
        mat = pointMat;
        break;
      case RenderType.Quad:
        mat = quadMat;
        break;
      case RenderType.PointBright:
        mat = lightMat;
        break;
    }

    mat.mainTexture = _position;
    mat.SetFloat("_Scale", scale);
    mat.SetFloat("_Size", starSize);
    mat.SetFloat("_Bright", starBrightness);
    mat.SetPass(0);

    Graphics.DrawProcedural(MeshTopology.Points, _position.width * _position.height);
  }
}
