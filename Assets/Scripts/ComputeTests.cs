using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Leap;
using Leap.Unity;

public class ComputeTests : MonoBehaviour {
  public const int MAX_PARTICLES = 1024 * 64;
  public const int MAX_CAPSULES = 1024;

  public const int BOX_SIDE = 64;
  public const int BOX_COUNT = BOX_SIDE * BOX_SIDE * BOX_SIDE;

  [SerializeField]
  private LeapProvider _provider;

  [SerializeField]
  private Mesh _mesh;

  [SerializeField]
  private ComputeShader _shader;

  [SerializeField]
  private Shader _display;

  [StructLayout(LayoutKind.Sequential)]
  private struct Particle {
    public Vector3 position;
    public Vector3 prevPosition;
    public Vector3 color;
  }

  [StructLayout(LayoutKind.Sequential)]
  private struct Capsule {
    public Vector3 pointA;
    public Vector3 pointB;
  }

  private int _integrateVerlet;
  private int _simulate;
  private int _integrateNaive;
  private int _integrate_x;
  private int _integrate_y;
  private int _integrate_z;
  private int _copy;
  private int _sort;

  private ComputeBuffer _capsules;
  private Capsule[] _capsuleArray = new Capsule[MAX_CAPSULES];

  private ComputeBuffer _particleFront;
  private ComputeBuffer _particleBack;

  private ComputeBuffer _count;
  private ComputeBuffer _boxStart;
  private ComputeBuffer _boxCount;

  private ComputeBuffer _argBuffer;

  private Material _displayMat;

  void OnEnable() {
    _capsules = new ComputeBuffer(MAX_CAPSULES, Marshal.SizeOf(typeof(Capsule)));

    _particleFront = new ComputeBuffer(MAX_PARTICLES, Marshal.SizeOf(typeof(Particle)));
    _particleBack = new ComputeBuffer(MAX_PARTICLES, Marshal.SizeOf(typeof(Particle)));

    _count = new ComputeBuffer(BOX_COUNT, sizeof(uint));
    _boxStart = new ComputeBuffer(BOX_COUNT, sizeof(uint));
    _boxCount = new ComputeBuffer(BOX_COUNT, sizeof(uint));

    _argBuffer = new ComputeBuffer(5, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
    uint[] args = new uint[5];
    args[0] = (uint)_mesh.GetIndexCount(0);
    args[1] = MAX_PARTICLES;
    _argBuffer.SetData(args);

    uint[] counts = new uint[BOX_COUNT];
    for (int i = 0; i < BOX_COUNT; i++) {
      counts[i] = 0;
    }
    _count.SetData(counts);

    _integrateVerlet = _shader.FindKernel("IntegrateVerlet");
    _simulate = _shader.FindKernel("Simulate");
    _integrate_x = _shader.FindKernel("Integrate_X");
    _integrate_y = _shader.FindKernel("Integrate_Y");
    _integrate_z = _shader.FindKernel("Integrate_Z");
    _integrateNaive = _shader.FindKernel("Integrate_Naive");
    _copy = _shader.FindKernel("Copy");
    _sort = _shader.FindKernel("Sort");

    Particle[] particles = new Particle[MAX_PARTICLES];
    for (int i = 0; i < MAX_PARTICLES; i++) {
      Vector3 pos = 0.5f * Random.insideUnitSphere;
      particles[i] = new Particle() {
        position = pos,
        prevPosition = pos,
        color = new Vector3(Random.value, Random.value, Random.value)
      };
    }
    _particleFront.SetData(particles);

    foreach (var index in new int[] { _integrateVerlet, _simulate, _integrate_x, _integrate_y, _integrate_z, _integrateNaive, _copy, _sort }) {
      _shader.SetBuffer(index, "_Capsules", _capsules);
      _shader.SetBuffer(index, "_ParticleFront", _particleFront);
      _shader.SetBuffer(index, "_ParticleBack", _particleBack);
      _shader.SetBuffer(index, "_Count", _count);
      _shader.SetBuffer(index, "_BoxStart", _boxStart);
      _shader.SetBuffer(index, "_BoxCount", _boxCount);
    }

    _displayMat = new Material(_display);
    _displayMat.SetBuffer("_Particles", _particleFront);
  }

  void OnDisable() {
    if (_particleFront != null) _particleFront.Release();
    if (_particleBack != null) _particleBack.Release();

    if (_count != null) _count.Release();
    if (_boxStart != null) _boxStart.Release();
    if (_boxCount != null) _boxCount.Release();

    if (_argBuffer != null) _argBuffer.Release();
    if (_capsules != null) _capsules.Release();
  }

  void Update() {
    int index = 0;
    Frame frame = _provider.CurrentFrame;
    foreach (var hand in frame.Hands) {
      foreach (var finger in hand.Fingers) {
        foreach (var bone in finger.bones) {
          _capsuleArray[index++] = new Capsule() {
            pointA = bone.PrevJoint.ToVector3(),
            pointB = bone.NextJoint.ToVector3()
          };
        }
      }
    }

    _capsules.SetData(_capsuleArray);
    _shader.SetInt("_CapsuleCount", index);

    for (int i = 0; i < 2; i++) {
      _shader.SetVector("_Center", transform.position);

      using (new ProfilerSample("Integrate")) {
        _shader.Dispatch(_integrateVerlet, MAX_PARTICLES / 64, 1, 1);
      }

      using (new ProfilerSample("Simulate")){
        _shader.Dispatch(_simulate, MAX_PARTICLES / 64, 1, 1);
      }

      /*
      Debug.Log("###### Counts after simulate: ");
      uint[] counts = new uint[BOX_COUNT];
      _count.GetData(counts);
      uint prevCount = uint.MaxValue;
      uint totalCount = 0;
      for (int i = 0; i < counts.Length; i++) {
        uint value = counts[i];
        totalCount += value;
        if (value != prevCount) {
          Debug.Log("i: " + value);
        }
        prevCount = value;
      }
      Debug.Log("Total count " + totalCount);
      */

      using (new ProfilerSample("Accumulate")) {
        _shader.Dispatch(_integrate_x, BOX_SIDE / 4, BOX_SIDE / 4, BOX_SIDE / 4);
        _shader.Dispatch(_integrate_y, BOX_SIDE / 4, BOX_SIDE / 4, BOX_SIDE / 4);
        _shader.Dispatch(_integrate_z, BOX_SIDE / 4, BOX_SIDE / 4, BOX_SIDE / 4);
      }

      //_shader.Dispatch(_integrateNaive, BOX_COUNT / 64, 1, 1);

      using (new ProfilerSample("Copy")) {
        _shader.Dispatch(_copy, BOX_COUNT / 64, 1, 1);
      }

      using (new ProfilerSample("Sort")) {
        _shader.Dispatch(_sort, MAX_PARTICLES / 64, 1, 1);
      }

      /*
      uint[] starts = new uint[BOX_COUNT];
      _boxStart.GetData(starts);


      Debug.Log("#####");
      uint prev = uint.MaxValue;
      for (int i = 0; i < starts.Length; i++) {
        uint value = starts[i];
        if (value != prev) {
          Debug.Log(i + ": " + value);
        }
        prev = value;
      }
      */
    }
  }

  void LateUpdate() {
    Graphics.DrawMeshInstancedIndirect(_mesh,
                                        0,
                                        _displayMat,
                                        new Bounds(Vector3.zero, Vector3.one * 10000),
                                        _argBuffer);
  }
}
