using System.Runtime.InteropServices;
using UnityEngine;

public class ParticleManager : MonoBehaviour {
  public const int MAX_PARTICLES = 64 * 100;
  public const float MAX_DELTA_TIME = 1.0f / 30.0f;
  public const string SIMULATION_KERNEL_NAME = "Simulate_Basic";

  [SerializeField]
  private Mesh _mesh;

  [SerializeField]
  private Shader _displayShader;

  [SerializeField]
  private ComputeShader _simulationShader;


  private Material _displayMaterial;
  private ComputeBuffer _particleBuffer;
  private ComputeBuffer _argBuffer;

  private int _simulationKernelIndex;

  [StructLayout(LayoutKind.Sequential)]
  public struct Particle {
    public Vector3 position;
    public Vector3 velocity;
    public Vector4 color;
  }

  void OnEnable() {
    _argBuffer = new ComputeBuffer(5, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
    _particleBuffer = new ComputeBuffer(MAX_PARTICLES, Marshal.SizeOf(typeof(Particle)));

    _displayMaterial = new Material(_displayShader);
    _displayMaterial.SetBuffer("_Particles", _particleBuffer);

    _simulationKernelIndex = _simulationShader.FindKernel(SIMULATION_KERNEL_NAME);
    _simulationShader.SetInt("_MaxParticles", MAX_PARTICLES);
    _simulationShader.SetBuffer(_simulationKernelIndex, "_Particles", _particleBuffer);

    Particle[] toSpawn = new Particle[MAX_PARTICLES];
    for (int i = 0; i < MAX_PARTICLES; i++) {
      toSpawn[i] = new Particle() {
        position = Random.insideUnitSphere * 1.1f,
        velocity = Vector3.zero,
        color = new Color(Random.value, Random.value, Random.value, 1)
      };
    }

    _particleBuffer.SetData(toSpawn);


    uint[] args = new uint[5];
    args[0] = (uint)_mesh.GetIndexCount(0);
    args[1] = MAX_PARTICLES;
    _argBuffer.SetData(args);
  }

  void OnDisable() {
    if (_particleBuffer != null) {
      _particleBuffer.Release();
    }

    if (_argBuffer != null) {
      _argBuffer.Release();
    }
  }

  void Update() {
    _simulationShader.SetFloat("_DeltaTime", Mathf.Min(MAX_DELTA_TIME, Time.deltaTime));
    Debug.Log(Time.deltaTime);

    _simulationShader.Dispatch(_simulationKernelIndex, MAX_PARTICLES / 64, 1, 1);
    
    //dispatch particle simulation here
    Graphics.DrawMeshInstancedIndirect(_mesh,
                                        0,
                                        _displayMaterial,
                                        new Bounds(Vector3.zero, Vector3.one * 10000),
                                        _argBuffer);

  }

  void OnPostRender() {
    //_displayMaterial.SetPass(0);
    //Graphics.DrawProcedural(MeshTopology.Points, MAX_PARTICLES);
  }
}
