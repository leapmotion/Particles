using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Query;

public class NoWay : MonoBehaviour {

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

  }

  int count = 0;

  public void SetCount(float value) {
    count = Mathf.RoundToInt(value * 10);
  }

  public void ResetTheSim() {
    Graphics.Blit(positions, positions, mat, 4);
  }

  void Update() {
    for(int i=0; i< count; i++) {
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