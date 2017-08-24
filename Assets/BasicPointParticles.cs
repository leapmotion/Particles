using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;
using Leap.Unity.Query;

public class BasicPointParticles : MonoBehaviour {

  public bool render = true;
  public bool simulate = true;

  [Header("Settings")]
  public bool quads = true;
  public float size = 0.05f;

  [Header("References")]
  public RenderTexture pos0, pos1;
  public RenderTexture vel0, vel1;

  public Material displayMat;
  public Material simulateMat;

  public Transform[] targets;
  private Vector4[] targetPositions;

  private List<Mesh> _meshes = new List<Mesh>();

  private void Start() {
    simulateMat.SetTexture("_Positions", pos0);
    simulateMat.SetTexture("_Velocities", vel0);

    pos0.DiscardContents();
    vel0.DiscardContents();

    simulateMat.SetFloat("_Seed", Random.value);
    Graphics.Blit(null, pos0, simulateMat, 2);
    simulateMat.SetFloat("_Seed", Random.value);
    Graphics.Blit(null, vel0, simulateMat, 2);

    targetPositions = new Vector4[targets.Length];

    if (!displayMat.shader.isSupported) {
      FindObjectOfType<Renderer>().material.color = Color.red;
    }

    generateMeshes();
  }

  private void generateMeshes() {
    Mesh currMesh = null;
    List<Vector3> verts = new List<Vector3>();
    List<Vector2> uvs = new List<Vector2>();
    List<int> tris = new List<int>();

    for (int i = 0; i < pos0.width; i++) {
      for (int j = 0; j < pos0.height; j++) {
        if (verts.Count + 4 >= 60000) {
          currMesh.SetVertices(verts);
          currMesh.SetUVs(0, uvs);
          currMesh.SetTriangles(tris, 0);
          currMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 10000);
          currMesh.UploadMeshData(markNoLogerReadable: true);
          currMesh = null;
        }

        if (currMesh == null) {
          currMesh = new Mesh();
          _meshes.Add(currMesh);
          verts.Clear();
          uvs.Clear();
          tris.Clear();
        }


        Vector2 uv;
        uv.x = i / (float)pos0.width;
        uv.y = j / (float)pos0.height;

        if (quads) {
          tris.Add(verts.Count + 0);
          tris.Add(verts.Count + 2);
          tris.Add(verts.Count + 1);

          tris.Add(verts.Count + 0);
          tris.Add(verts.Count + 3);
          tris.Add(verts.Count + 2);

          uvs.Add(uv);
          uvs.Add(uv);
          uvs.Add(uv);
          uvs.Add(uv);

          verts.Add(new Vector3(size, size, 0));
          verts.Add(new Vector3(-size, size, 0));
          verts.Add(new Vector3(-size, -size, 0));
          verts.Add(new Vector3(size, -size, 0));
        }
      }
    }

    currMesh.SetVertices(verts);
    currMesh.SetUVs(0, uvs);
    currMesh.SetTriangles(tris, 0);
    currMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 10000);
    currMesh.UploadMeshData(markNoLogerReadable: true);
    currMesh = null;
  }

  private void FixedUpdate() {
    if (simulate) {
      targets.Query().Select(t => (Vector4)t.position).FillArray(targetPositions);
      simulateMat.SetVectorArray("_Targets", targetPositions);

      vel1.DiscardContents();
      Graphics.Blit(vel0, vel1, simulateMat, 0);
      simulateMat.SetTexture("_Velocities", vel1);
      Utils.Swap(ref vel0, ref vel1);

      pos1.DiscardContents();
      Graphics.Blit(pos0, pos1, simulateMat, 1);
      simulateMat.SetTexture("_Positions", pos1);
      Utils.Swap(ref pos0, ref pos1);
    }
  }

  public void SetSize(float per) {
    displayMat.SetFloat("_Size", per.Map(0, 1, 0, 20));
  }

  //private void Update() {
  //  if (render) {
  //    displayMat.mainTexture = pos0;
  //    foreach (var mesh in _meshes) {
  //      Graphics.DrawMesh(mesh, Matrix4x4.identity, displayMat, 0);
  //    }
  //  }
  //}

  void OnPostRender() {
    displayMat.mainTexture = pos0;
    displayMat.SetPass(0);
    Graphics.DrawProcedural(MeshTopology.Points, pos0.width * pos0.height);
  }
}
