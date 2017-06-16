using System.Collections.Generic;
using UnityEngine;

public class TextureSimulator : MonoBehaviour {

  public Mesh mesh;
  public RenderTexture positions;
  public RenderTexture velocities;
  public Material mat;
  public Material displayMat;

  private List<Mesh> _meshes = new List<Mesh>();

  void Start() {
    Graphics.Blit(positions, positions, mat, 4);

    var verts = mesh.vertices;
    for (int i = 0; i < verts.Length; i++) {
      verts[i] = verts[i] * 0.02f;
    }

    var tris = mesh.triangles;

    List<Vector3> pos = new List<Vector3>();
    List<int> tri = new List<int>();
    List<Vector2> uv = new List<Vector2>();

    Mesh _mesh = null;
    for (int i = 0; i < positions.width; i++) {
      for (int j = 0; j < positions.height; j++) {
        if (pos.Count + verts.Length > 60000) {
          _mesh.SetVertices(pos);
          _mesh.SetTriangles(tri, 0);
          _mesh.SetUVs(0, uv);
          _mesh.RecalculateNormals();
          _mesh = null;

          pos.Clear();
          tri.Clear();
          uv.Clear();
        }

        if (_mesh == null) {
          tris = mesh.triangles;
          _mesh = new Mesh();
          _mesh.hideFlags = HideFlags.HideAndDontSave;
          _meshes.Add(_mesh);
        }

        pos.AddRange(verts);
        tri.AddRange(tris);

        for (int k = 0; k < verts.Length; k++) {
          uv.Add(new Vector2((i + 0.5f) / (float)positions.width, (j + 0.5f) / (float)positions.height));
        }

        for (int k = 0; k < tris.Length; k++) {
          tris[k] += verts.Length;
        }
      }
    }

    _mesh.hideFlags = HideFlags.HideAndDontSave;
    _mesh.SetVertices(pos);
    _mesh.SetTriangles(tri, 0);
    _mesh.SetUVs(0, uv);
    _mesh.RecalculateNormals();

    Debug.Log("Supports RT: " + SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat));

    {
      Color[] colors = new Color[10];
      colors[0] = new Color(1.0f, 0.0f, 0.0f);
      colors[1] = new Color(0.3f, 0.2f, 0.0f);
      colors[2] = new Color(0.3f, 0.3f, 0.0f);
      colors[3] = new Color(0.0f, 0.3f, 0.0f);
      colors[4] = new Color(0.0f, 0.0f, 0.3f);
      colors[5] = new Color(0.3f, 0.0f, 0.3f);
      colors[6] = new Color(0.3f, 0.3f, 0.3f);
      colors[7] = new Color(0.3f, 0.4f, 0.3f);
      colors[8] = new Color(0.3f, 0.4f, 0.3f);
      colors[9] = new Color(0.3f, 0.2f, 0.3f);

      displayMat.SetColorArray("_Colors", colors);
    }

    {
      const int MAX_SPECIES = 10;
      const float MAX_SOCIAL_FORCE = 0.003f;
      const float MAX_SOCIAL_RANGE = 0.5f;

      int redSpecies = 0;

      float normalLove = MAX_SOCIAL_FORCE * 0.04f;
      float fearOfRed = MAX_SOCIAL_FORCE * -1.0f;
      float redLoveOfOthers = MAX_SOCIAL_FORCE * 2.0f;
      float redLoveOfSelf = MAX_SOCIAL_FORCE * 0.9f;

      float normalRange = MAX_SOCIAL_RANGE * 0.4f;
      float fearRange = MAX_SOCIAL_RANGE * 0.3f;
      float loveRange = MAX_SOCIAL_RANGE * 0.3f;
      float redSelfRange = MAX_SOCIAL_RANGE * 0.4f;

      Vector4[] _socialData = new Vector4[MAX_SPECIES * MAX_SPECIES];

      for (int s = 0; s < MAX_SPECIES; s++) {
        for (int o = 0; o < MAX_SPECIES; o++) {
          _socialData[s * 10 + o] = new Vector2(normalLove, normalRange);
        }

        _socialData[s * 10 + redSpecies] = new Vector2(fearOfRed, fearRange * ((float)(s + 1) / (float)MAX_SPECIES));
        _socialData[redSpecies * 10 + redSpecies] = new Vector2(redLoveOfSelf, redSelfRange);
        _socialData[redSpecies * 10 + s] = new Vector2(redLoveOfOthers, loveRange);
      }

      for (var t = 0; t < MAX_SPECIES; t++) {
        for (var f = 0; f < MAX_SPECIES; f++) {
          _socialData[t * 10 + f] = new Vector2(-MAX_SOCIAL_FORCE * 0.1f, MAX_SOCIAL_RANGE);
        }
      }

      for (var t = 2; t < MAX_SPECIES; t++) {
        for (var f = 0; f < MAX_SPECIES; f++) {
          _socialData[t * 10 + f] = new Vector2(f * MAX_SOCIAL_FORCE * 2.0f / MAX_SPECIES - t * MAX_SOCIAL_FORCE * 0.7f / MAX_SPECIES,
                                                MAX_SOCIAL_RANGE * 0.5f);
        }
      }

      mat.SetVectorArray("_SocialData", _socialData);
    }
  }

  int count = 1;

  public void SetCount(float value) {
    count = Mathf.RoundToInt(value * 10);
  }

  public void ResetTheSim() {
    Graphics.Blit(positions, positions, mat, 4);
  }

  void Update() {
    if (Input.GetKeyDown(KeyCode.R)) {
      const int MAX_SPECIES = 10;
      const float MAX_SOCIAL_FORCE = 0.003f;
      const float MAX_SOCIAL_RANGE = 0.5f;

      Color[] colors = new Color[MAX_SPECIES];
      for (int i = 0; i < colors.Length; i++) {
        colors[i] = Color.HSVToRGB(Random.value, Random.Range(0.5f, 1), Random.Range(0.3f, 1));
      }
      displayMat.SetColorArray("_Colors", colors);

      Vector4[] _socialData = new Vector4[MAX_SPECIES * MAX_SPECIES];

      for (int s = 0; s < MAX_SPECIES; s++) {
        for (int o = 0; o < MAX_SPECIES; o++) {
          _socialData[s * 10 + o] = new Vector2(Random.Range(-MAX_SOCIAL_FORCE, MAX_SOCIAL_FORCE), Random.value * MAX_SOCIAL_RANGE);
        }
      }

      mat.SetVectorArray("_SocialData", _socialData);
    }

    for (int i = 0; i < count; i++) {
      Graphics.Blit(velocities, positions, mat, 0);
      Graphics.Blit(positions, velocities, mat, 1);
      Graphics.Blit(positions, velocities, mat, 2);
      Graphics.Blit(positions, velocities, mat, 3);
    }

    foreach (var mesh in _meshes) {
      Graphics.DrawMesh(mesh, Matrix4x4.identity, displayMat, 0);
    }
  }

}