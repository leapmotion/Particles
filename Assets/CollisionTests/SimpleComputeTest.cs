using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class SimpleComputeTest : MonoBehaviour {
  public const int PARTICLE_COUNT = 1024;

  public ComputeShader _compute;
  public Shader _shader;

  private struct Particle {
    public Vector3 position;
    public Vector3 velocity;
  }

  private Material _material;
  private ComputeBuffer _particles;

  private void OnEnable() {
    _material = new Material(_shader);

    _particles = new ComputeBuffer(PARTICLE_COUNT, Marshal.SizeOf(typeof(Particle)));
    Particle[] data = new Particle[PARTICLE_COUNT];
    for (int i = 0; i < data.Length; i++) {
      data[i] = new Particle() {
        position = Random.insideUnitSphere,
        velocity = Random.insideUnitSphere * 0.001f
      };
    }
    _particles.SetData(data);

    _compute.SetBuffer(0, "_Particles", _particles);
    _material.SetBuffer("_Particles", _particles);
  }

  private void OnDisable() {
    if (_particles != null) _particles.Release();
  }

  private void Update() {
    _compute.Dispatch(0, PARTICLE_COUNT / 64, 1, 1);
  }

  private void OnPostRender() {
    _material.SetPass(0);
    Graphics.DrawProcedural(MeshTopology.Points, PARTICLE_COUNT);
  }
}
