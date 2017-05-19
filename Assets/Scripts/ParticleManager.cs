using System.Runtime.InteropServices;
using UnityEngine;
using float4 = UnityEngine.Vector4;
using float3 = UnityEngine.Vector3;

public class ParticleManager : MonoBehaviour {
  public const int MAX_PARTICLES = 64 * 300;
  public const float MAX_DELTA_TIME = 1.0f / 30.0f;
  public const string SIMULATION_KERNEL_NAME = "Simulate_Basic";

  public const float MAX_FORCE_STEPS = 5;
  public const float MAX_SPECIES = 8;

  public const float FRICTION = 0.01f;
  public const float DAMP_CONSTANT = (1.0f - FRICTION);

  public const float RADIUS = 0.01f;

  public const float MAX_SOCIAL_RANGE = (RADIUS * 20);
  public const float MAX_COLLISION_FORCE = 2;
  public const float MAX_SOCIAL_FORCE = 0.5f;

  [SerializeField]
  private bool _useComputeShader = false;

  [SerializeField]
  private Mesh _mesh;

  [SerializeField]
  private Shader _cpuShader;

  [SerializeField]
  private Shader _displayShader;

  [SerializeField]
  private ComputeShader _simulationShader;


  private Particle[] _particles;
  private Particle[] _backBuffer;

  private Material _cpuMaterial;
  private Material _computeMaterial;
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
    if (!SystemInfo.supportsComputeShaders) {
      Debug.LogError("This system does not support compute shaders");
      return;
    }

    if (!SystemInfo.supportsInstancing) {
      Debug.LogError("This system does not support instancing!");
      return;
    }

    _argBuffer = new ComputeBuffer(5, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
    _particleBuffer = new ComputeBuffer(MAX_PARTICLES, Marshal.SizeOf(typeof(Particle)));

    _cpuMaterial = new Material(_cpuShader);

    _computeMaterial = new Material(_displayShader);
    _computeMaterial.SetBuffer("_Particles", _particleBuffer);

    _simulationKernelIndex = _simulationShader.FindKernel(SIMULATION_KERNEL_NAME);
    _simulationShader.SetInt("_MaxParticles", MAX_PARTICLES);
    _simulationShader.SetBuffer(_simulationKernelIndex, "_Particles", _particleBuffer);

    _particles = new Particle[MAX_PARTICLES];
    _backBuffer = new Particle[MAX_PARTICLES];
    for (int i = 0; i < MAX_PARTICLES; i++) {
      _particles[i] = new Particle() {
        position = Random.insideUnitSphere * 1.1f,
        velocity = Vector3.zero,
        color = new Color(Random.value, Random.value, Random.value, 1)
      };
    }

    _particleBuffer.SetData(_particles);


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
    SimulateParticles(1.0f / 30.0f);
    DisplayParticles();
  }

  void OnPostRender() {
    _computeMaterial.SetPass(0);
    Graphics.DrawProcedural(MeshTopology.Points, MAX_PARTICLES);
  }

  void SimulateParticles(float deltaTime) {
    if (_useComputeShader) {
      simulateParticlesCompute(deltaTime);
    } else {
      simulateParticlesCPU(deltaTime);
    }
  }

  void DisplayParticles() {
    if (_useComputeShader) {
      displayParticlesCompute();
    } else {
      displayParticlesCPU();
    }
  }

  #region COMPUTE SHADER IMPLEMENTATION
  private void simulateParticlesCompute(float deltaTime) {
    _simulationShader.SetFloat("_DeltaTime", deltaTime);

    _simulationShader.Dispatch(_simulationKernelIndex, MAX_PARTICLES / 64, 1, 1);
  }

  private void displayParticlesCompute() {
    Graphics.DrawMeshInstancedIndirect(_mesh,
                                        0,
                                        _computeMaterial,
                                        new Bounds(Vector3.zero, Vector3.one * 10000),
                                        _argBuffer);
  }
  #endregion

  #region CPU IMPLEMENTATION

  private void simulateParticlesCPU(float deltaTime) {
    for (int index = 0; index < MAX_PARTICLES; index++) {
      Particle p = _particles[index];

      //float3 force = 0.01 * (float3(0,0,0) - p.position);
      //p.velocity += force;

      float4 accumForce = new float4(0, 0, 0, 0);

      //Loop through every other particle to compare against this particle
      for (uint i = 0; i < MAX_PARTICLES; i++) {
        //Dont compare against ourself!
        if (i == index) continue;

        Particle other = _particles[i];
        float3 toOther = other.position - p.position;
        float distanceSqrd = toOther.sqrMagnitude;

        if (distanceSqrd < MAX_SOCIAL_RANGE * MAX_SOCIAL_RANGE && distanceSqrd > 0) {
          float distance = Mathf.Sqrt(distanceSqrd);

          //This is for long range interaction
          if (distance < MAX_SOCIAL_RANGE) {
            accumForce += (float4)(MAX_SOCIAL_FORCE * (toOther / distance));
            accumForce.w += 1;
          }

          //This is for collision
          if (distance < RADIUS * 2) {
            float penetration = 1.0f - distance / (2 * RADIUS);
            p.velocity -= deltaTime * MAX_COLLISION_FORCE * (toOther / distance) * penetration;
          }
        }
      }

      //Apply accumulated forces towards (or away) from other particles
      if (accumForce.w > 0) {
        //Divide by w, which is the number of particles we are interacting with
        p.velocity += deltaTime * (Vector3)accumForce / accumForce.w;
      }

      //Integration
      p.velocity *= DAMP_CONSTANT;
      p.position += deltaTime * p.velocity;

      _backBuffer[index] = p;
    }

    //swap back and front buffer
    var temp = _backBuffer;
    _backBuffer = _particles;
    _particles = temp;
  }

  private void displayParticlesCPU() {
    var block = new MaterialPropertyBlock();
    for (int i = 0; i < MAX_PARTICLES; i++) {
      block.SetColor("_Color", _particles[i].color);
      var matrix = Matrix4x4.TRS(_particles[i].position, Quaternion.identity, Vector3.one * RADIUS * 2);
      Graphics.DrawMesh(_mesh, matrix, _cpuMaterial, 0, null, 0, block);
    }
  }

  #endregion

}
