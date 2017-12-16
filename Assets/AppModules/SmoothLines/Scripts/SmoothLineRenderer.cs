using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;

[ExecuteInEditMode]
public class SmoothLineRenderer : MonoBehaviour {
  private const int TEXTURE_RES = 16;

  public float radius;
  public List<Vector3> points;

  public Material maskMat;
  public Material fillmat;

  public Texture2D _circleTexture;
  public Texture2D _gradientTexture;

  private void Start() {
    initTextures();
  }

  private void OnPostRender() {
    maskMat.mainTexture = _circleTexture;
    maskMat.SetPass(0);

    drawTheThing();

    fillmat.mainTexture = _circleTexture;
    fillmat.SetPass(0);

    drawTheThing();
  }

  private void drawTheThing() {
    GL.Begin(GL.QUADS);

    foreach (var point in points) {
      Vector3 toCamera = transform.position - point;
      Vector3 right = Utils.Perpendicular(toCamera).normalized * radius;
      Vector3 up = Vector3.Cross(right, toCamera).normalized * radius;

      GL.TexCoord(new Vector2(0, 0));
      GL.Vertex(point + right + up);

      GL.TexCoord(new Vector2(0, 1));
      GL.Vertex(point + right - up);

      GL.TexCoord(new Vector2(1, 1));
      GL.Vertex(point - right - up);

      GL.TexCoord(new Vector2(1, 0));
      GL.Vertex(point - right + up);
    }

    //GL.End();

    //blitMat.mainTexture = _gradientTexture;
    //blitMat.SetPass(0);

    //GL.Begin(GL.QUADS);

    for (int i = 1; i < points.Count; i++) {
      var a = points[i - 1];
      var b = points[i];

      Vector3 up = Vector3.Cross(a - b, a - transform.position).normalized * radius;

      GL.TexCoord(new Vector2(0.5f, 0));
      GL.Vertex(a - up);

      GL.TexCoord(new Vector2(0.5f, 1));
      GL.Vertex(a + up);

      GL.TexCoord(new Vector2(0.5f, 1));
      GL.Vertex(b + up);

      GL.TexCoord(new Vector2(0.5f, 0));
      GL.Vertex(b - up);
    }

    GL.End();
  }

  private void initTextures() {
    //_circleTexture = new Texture2D(TEXTURE_RES, TEXTURE_RES, TextureFormat.Alpha8, mipmap: true, linear: true);
    //_gradientTexture = new Texture2D(TEXTURE_RES, TEXTURE_RES, TextureFormat.Alpha8, mipmap: true, linear: true);
    //_circleTexture.filterMode = FilterMode.Trilinear;
    //_gradientTexture.filterMode = FilterMode.Trilinear;
    //_circleTexture.wrapMode = TextureWrapMode.Clamp;
    //_gradientTexture.wrapMode = TextureWrapMode.Clamp;

    //for (int i = 0; i < TEXTURE_RES; i++) {
    //  for (int j = 0; j < TEXTURE_RES; j++) {
    //    float dx = 2.0f * (i - ((TEXTURE_RES - 1.0f) / 2.0f)) / (TEXTURE_RES - 1.0f);
    //    float dy = 2.0f * (j - ((TEXTURE_RES - 1.0f) / 2.0f)) / (TEXTURE_RES - 1.0f);

    //    float circleAlpha = 1.0f - Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
    //    float gradientAlpha = 1.0f - Mathf.Abs(dy);

    //    _circleTexture.SetPixel(i, j, new Color(1, 1, 1, circleAlpha));
    //    _gradientTexture.SetPixel(i, j, new Color(1, 1, 1, gradientAlpha));
    //  }
    //}

    //_circleTexture.Apply(updateMipmaps: true);
    //_gradientTexture.Apply(updateMipmaps: true);

    //foreach (var tex in new Texture2D[] { _circleTexture, _gradientTexture }) {
    //  for (int i = 0; i < tex.mipmapCount; i++) {
    //    var pixels = tex.GetPixels(i);

    //    int width = Mathf.RoundToInt(Mathf.Sqrt(pixels.Length));
    //    for (int dx = 0; dx < width; dx++) {
    //      pixels[dx * width + 0] = new Color(0, 0, 0, 0);
    //      pixels[dx * width + width - 1] = new Color(0, 0, 0, 0);
    //      pixels[0 * width + dx] = new Color(0, 0, 0, 0);
    //      pixels[(width - 1) * width + dx] = new Color(0, 0, 0, 0);
    //    }

    //    tex.SetPixels(pixels, i);
    //  }

    //  tex.Apply(updateMipmaps: false);
    //}
  }



}
