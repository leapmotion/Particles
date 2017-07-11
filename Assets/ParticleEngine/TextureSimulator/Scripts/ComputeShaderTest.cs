using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Query;

public class ComputeShaderTest : MonoBehaviour {

  public ComputeShader _shader;
  
  public struct Cluster {
    public Vector3 v;
    public float r;
    public uint count;
    public uint start;
    public uint end;
  }

  private void Start() {
    RenderTexture particles = createTexture(1, randomWrite: false);
    RenderTexture clustered = createTexture(1, randomWrite: true);

    ComputeBuffer clusters = new ComputeBuffer(2, sizeof(float) * 4 + sizeof(uint) * 3);
    ComputeBuffer assignments = new ComputeBuffer(4096, sizeof(uint));

    int sort = _shader.FindKernel("SortParticlesIntoClusters");

    foreach(var kernel in new int[] { sort }) {
      _shader.SetTexture(kernel, "_Particles", particles);
      _shader.SetTexture(kernel, "_ClusteredParticles", clustered);
      _shader.SetBuffer(kernel, "_Clusters", clusters);
      _shader.SetBuffer(kernel, "_ClusterAssignments", assignments);
    }

    uint[] ii = new uint[4096];
    for(int i=0; i<4096; i++) {
      ii[i] = Random.value > 0.5f ? 0u : 1u;
    }
    assignments.SetData(ii);

    _shader.Dispatch(sort, 4096 / 64, 1, 1);

    Cluster[] cld = new Cluster[2];
    clusters.GetData(cld);

    FindObjectOfType<TextMesh>().text = cld[0].end + " : " + cld[1].end;
  }

  private RenderTexture createTexture(int height = 1, bool randomWrite = false) {
    RenderTexture tex = new RenderTexture(4096, height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
    tex.wrapMode = TextureWrapMode.Clamp;
    tex.filterMode = FilterMode.Point;
    tex.enableRandomWrite = randomWrite;

    RenderTexture.active = tex;
    GL.Clear(clearDepth: false, clearColor: true, backgroundColor: Color.blue);
    RenderTexture.active = null;
    return tex;
  }

}
