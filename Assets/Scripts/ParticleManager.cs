using System.Runtime.InteropServices;
using UnityEngine;

public class ParticleManager : MonoBehaviour {
  public const int MAX_PARTICLES = 100000;
  public const string SIMULATION_KERNEL_NAME = "Simulate_Basic";

  [SerializeField]
  private Mesh _mesh;

  [SerializeField]
  private Shader _displayShader;

  [SerializeField]
  private ComputeShader _simulationShader;


  private Material _displayMaterial;
  private ComputeBuffer _particleBuffer;

  private int _simulationKernelIndex;

  [StructLayout(LayoutKind.Sequential)]
  public struct Particle {
    public Vector3 position;
    public Vector3 velocity;
  }

  void OnEnable() {
    _particleBuffer = new ComputeBuffer(MAX_PARTICLES, Marshal.SizeOf(typeof(Particle)));

    _displayMaterial = new Material(_displayShader);
    _displayMaterial.SetBuffer("_Particles", _particleBuffer);

    _simulationKernelIndex = _simulationShader.FindKernel(SIMULATION_KERNEL_NAME);
    _simulationShader.SetBuffer(_simulationKernelIndex, "_Particles", _particleBuffer);

    Particle[] toSpawn = new Particle[MAX_PARTICLES];
    for (int i = 0; i < MAX_PARTICLES; i++) {
      toSpawn[i] = new Particle() {
        position = Random.insideUnitSphere,
        velocity = Vector3.zero
      };
    }

    _particleBuffer.SetData(toSpawn);
  }

  void OnDisable() {
    if (_particleBuffer != null) {
      _particleBuffer.Release();
    }
  }

  void Update() {
    //dispatch particle simulation here
  }

  void OnPostRender() {
    _displayMaterial.SetPass(0);
    Graphics.DrawProcedural(MeshTopology.Points, MAX_PARTICLES);
  }
}
