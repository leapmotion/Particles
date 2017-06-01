using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;
using Leap.Unity.Attributes;
using Leap.Unity.RuntimeGizmos;

public abstract class ParticleEngineBase : MonoBehaviour {

  [Header("Simulation")]
  [MinValue(0)]
  [EditTimeOnly]
  [SerializeField]
  private int _maxParticles = 1024;

  [SerializeField]
  private bool _useMultithreading = false;

  [Header("Collision Chunks")]
  [SerializeField]
  private bool _useNaiveChunking = false;

  [EditTimeOnly]
  [SerializeField]
  private float _chunkSize = 0.1f;

  [EditTimeOnly]
  [SerializeField]
  private ChunkResolution _chunkResolution = ChunkResolution.Sixteen;

  [Header("Rendering")]
  [SerializeField]
  private DisplayMethod _renderMethod = DisplayMethod.DrawMesh;

  [SerializeField]
  private Mesh _particleMesh;

  [SerializeField]
  private Material _displayMaterial;

  //Particle simulation data
  private Particle[] _particlesBack;
  private Particle[] _particlesFront;
  private int[] _chunkCount;
  private int _aliveParticles = 0;
  private Queue<Particle> _toEmit = new Queue<Particle>();

  private SpeciesData[] _speciesData;
  private SocialData[] _socialData;

  //Threading
  private ParallelForeach _integrationForeach;
  private ParallelForeach _accumulationNaiveForeach;
  private ParallelForeach _accumulationXForeach;
  private ParallelForeach _accumulationYForeach;
  private ParallelForeach _accumulationZForeach;
  private ParallelForeach _sortingForeach;

  //Collision acceleration structures
  private int _chunkSide;
  private int _numChunks;
  private Vector3 _collisionOffset;
  private Vector3 _collisionSize;

  private int[] _chunkStart;
  private int[] _chunkEnd;

  public struct Particle {
    public Vector3 position;
    public Vector3 prevPosition;
    public int species;
  }

  public struct SpeciesData {

  }

  public struct SocialData {

  }

  public enum ChunkResolution {
    Four = 4,
    Eight = 8,
    Sixteen = 16,
    ThirtyTwo = 32,
    SixyFour = 64
  }

  public enum DisplayMethod {
    DrawMesh
  }

  #region UNITY MESSAGES

  protected virtual void Awake() {
    _integrationForeach = new ParallelForeach(integrateParticles);
    _accumulationNaiveForeach = new ParallelForeach(accumulateCollisionChunksNaive);
    _accumulationXForeach = new ParallelForeach(accumulateCollisionChunksX);
    _accumulationYForeach = new ParallelForeach(accumulateCollisionChunksY);
    _accumulationZForeach = new ParallelForeach(accumulateCollisionChunksZ);
    _sortingForeach = new ParallelForeach(sortParticlesIntoChunks);

    _integrationForeach.OnComplete += () => {
      emitParticles();

      if (_useNaiveChunking) {
        _accumulationNaiveForeach.Dispatch(_numChunks);
      } else {
        _accumulationXForeach.Dispatch(_chunkSide);
      }
    };

    _accumulationNaiveForeach.OnComplete += () => {
      _sortingForeach.Dispatch(_aliveParticles);
    };

    _accumulationXForeach.OnComplete += () => {
      _accumulationYForeach.Dispatch(_chunkSide);
    };

    _accumulationYForeach.OnComplete += () => {
      _accumulationZForeach.Dispatch(_chunkSide);
    };

    _accumulationZForeach.OnComplete += () => {
      System.Array.Copy(_chunkStart, _chunkEnd, _chunkEnd.Length);
      _sortingForeach.Dispatch(_aliveParticles);
    };

    _chunkSide = (int)_chunkResolution;
    _numChunks = _chunkSide * _chunkSide * _chunkSide;

    _particlesBack = new Particle[_maxParticles];
    _particlesFront = new Particle[_maxParticles];
    _speciesData = new SpeciesData[1];

    _chunkCount = new int[_numChunks];
    _chunkStart = new int[_numChunks];
    _chunkEnd = new int[_numChunks];
  }

  protected virtual void Update() {
    System.Array.Clear(_chunkCount, 0, _chunkCount.Length);

    if (_useMultithreading) {
      using (new ProfilerSample("Dispatch Simulation Jobs")) {
        _integrationForeach.Dispatch(_aliveParticles);
      }
    } else {
      using (new ProfilerSample("Integrate Particles")) {
        integrateParticles(0, _aliveParticles);
      }

      using (new ProfilerSample("Emit Particles")) {
        emitParticles();
      }

      using (new ProfilerSample("Accumulate Collision Chunks")) {
        if (_useNaiveChunking) {
          accumulateCollisionChunksNaive(0, _numChunks);
        } else {
          using (new ProfilerSample("Accumulate X")) {
            accumulateCollisionChunksX(0, _chunkSide);
          }

          using (new ProfilerSample("Accumulate Y")) {
            accumulateCollisionChunksY(0, _chunkSide);
          }

          using (new ProfilerSample("Accumulate Z")) {
            accumulateCollisionChunksZ(0, _chunkSide);
          }

          using (new ProfilerSample("Copy")) {
            System.Array.Copy(_chunkStart, _chunkEnd, _chunkEnd.Length);
          }
        }
      }

      using (new ProfilerSample("Sort Particles Into Chunks")) {
        sortParticlesIntoChunks(0, _aliveParticles);
      }
    }
  }

  protected virtual void LateUpdate() {
    if (_useMultithreading) {
      using (new ProfilerSample("Wait For Simulation Jobs")) {
        _integrationForeach.Wait();
        _accumulationNaiveForeach.Wait();
        _accumulationXForeach.Wait();
        _accumulationYForeach.Wait();
        _accumulationZForeach.Wait();
        _sortingForeach.Wait();
      }
    }

    if (_renderMethod == DisplayMethod.DrawMesh) {
      int state = Random.Range(int.MinValue, int.MaxValue);

      MaterialPropertyBlock block = new MaterialPropertyBlock();
      for (int i = 0; i < _aliveParticles; i++) {
        var matrix = Matrix4x4.TRS(_particlesFront[i].position, Quaternion.identity, Vector3.one * 0.05f);

        int chunk = getChunk(ref _particlesFront[i]);
        Random.InitState(chunk);
        block.SetColor("_Color", new Color(Random.value, Random.value, Random.value));

        //int chunkStart = _chunkStart[chunk];
        //int chunkEnd = _chunkEnd[chunk];
        //block.SetColor("_Color", (i >= chunkStart && i < chunkEnd) ? Color.green : Color.red);

        //float p = i / (float)_aliveParticles;
        //block.SetColor("_Color", new Color(p, p, p));



        Graphics.DrawMesh(_particleMesh, matrix, _displayMaterial, 0, null, 0, block);
      }

      Random.InitState(state);
    }
  }

  #endregion

  #region PUBLIC API

  public int aliveParticles {
    get {
      return _aliveParticles;
    }
  }

  public int maxParticles {
    get {
      return _maxParticles;
    }
  }

  public bool TryEmit(Particle particle) {
    if (_toEmit.Count + _aliveParticles >= _maxParticles) {
      return false;
    } else {
      _toEmit.Enqueue(particle);
      return true;
    }
  }

  protected abstract void DoParticleCollision(ref Particle particle,
                                              ref SpeciesData speciesData,
                                              ref Particle other,
                                              ref SpeciesData otherSpeciesData,
                                              ref Vector3 totalDisplacement,
                                              ref int totalCollisions);

  protected abstract bool DoParticleInteraction(ref Particle particle,
                                                ref SpeciesData speciesData,
                                                ref Particle other,
                                                ref SpeciesData otherSpeciesData,
                                                ref Vector3 particleDisplacement);

  protected abstract void DoParticleGlobalForces(ref Particle particle,
                                                 ref SpeciesData speciesData);

  protected abstract void DoParticleConstraints(ref Particle particle,
                                                ref SpeciesData speciesData);

  protected abstract bool ShouldKillParticle(ref Particle particle);
  #endregion

  #region PRIVATE IMPLEMENTATION
  private void emitParticles() {
    while (_toEmit.Count > 0) {
      Particle toEmit = _toEmit.Dequeue();

      //Make sure to increment the count of the chunk that we are emitting into
      int emitChunk = getChunk(ref toEmit);
      _chunkCount[emitChunk]++;

      //Plop the particle onto the end of the front array, will be sorted into
      //the right chunk by the next accumulate/sort cycle
      _particlesBack[_aliveParticles++] = toEmit;
    }
  }

  private void integrateParticles(int startIndex, int endIndex) {
    for (int i = startIndex; i < endIndex; i++) {
      _particlesBack[i] = _particlesFront[i];
      integrateParticle(i, ref _particlesBack[i], ref _speciesData[_particlesBack[i].species]);
    }
  }

  public bool use2x2 = true;
  private void integrateParticle(int index, ref Particle particle, ref SpeciesData speciesData) {
    Vector3 originalPos = particle.position;

    particle.position += 0.99f * (particle.position - particle.prevPosition);

    DoParticleGlobalForces(ref particle, ref speciesData);

    //TODO: particle-particle forces

    if (use2x2) {
      resolveParticleCollisions2x2(index, ref particle, ref speciesData);
    } else {
      resolveParticleCollisionsNaive(index, ref particle, ref speciesData);
    }

    DoParticleConstraints(ref particle, ref speciesData);

    particle.prevPosition = originalPos;

    int newChunk = getChunk(ref particle);
    Interlocked.Add(ref _chunkCount[newChunk], 1);
  }

  private void accumulateCollisionChunksNaive(int startIndex, int endIndex) {
    for (int i = startIndex; i < endIndex; i++) {

      int sum = 0;
      for (int j = 0; j <= i; j++) {
        sum += _chunkCount[j];
      }

      _chunkStart[i] = sum;
      _chunkEnd[i] = sum;
    }
  }

  private void accumulateCollisionChunksX(int startX, int endX) {
    for (int x = startX; x < endX; x++) {
      for (int y = 0; y < _chunkSide; y++) {
        for (int z = 0; z < _chunkSide; z++) {
          int start = y * _chunkSide + z * _chunkSide * _chunkSide;
          int index = start + x;

          int sum = 0;
          for (int i = start; i <= index; i++) {
            sum += _chunkCount[i];
          }

          _chunkStart[index] = sum;
        }
      }
    }
  }

  private void accumulateCollisionChunksY(int startX, int endX) {
    for (int z = 0; z < _chunkSide; z++) {
      for (int y = 0; y < _chunkSide; y++) {
        for (int x = startX; x < endX; x++) {
          int start = (_chunkSide - 1) + z * _chunkSide * _chunkSide;
          int index = x + y * _chunkSide + z * _chunkSide * _chunkSide;

          int sum = _chunkStart[index];
          for (int i = start; i < index; i += _chunkSide) {
            sum += _chunkStart[i];
          }

          _chunkEnd[index] = sum;
        }
      }
    }
  }

  private void accumulateCollisionChunksZ(int startX, int endX) {
    for (int x = startX; x < endX; x++) {
      for (int y = 0; y < _chunkSide; y++) {
        for (int z = 0; z < _chunkSide; z++) {
          int start = (_chunkSide - 1) + ((_chunkSide - 1) * _chunkSide);
          int index = x + y * _chunkSide + z * _chunkSide * _chunkSide;

          int sum = _chunkEnd[index];
          for (int i = start; i < index; i += (_chunkSide * _chunkSide)) {
            sum += _chunkEnd[i];
          }

          _chunkStart[index] = sum;
        }
      }
    }
  }

  private void sortParticlesIntoChunks(int startIndex, int endIndex) {
    for (int i = startIndex; i < endIndex; i++) {
      sortParticleIntoChunk(i, ref _particlesBack[i]);
    }
  }

  private void sortParticleIntoChunk(int index, ref Particle particle) {
    int chunk = getChunk(ref particle);

    int newIndex = Interlocked.Add(ref _chunkStart[chunk], -1);
    _particlesFront[newIndex] = particle;
  }

  private void resolveParticleCollisionsNaive(int index, ref Particle particle, ref SpeciesData speciesData) {
    Vector3 totalDepenetration = Vector3.zero;
    int numCollisions = 0;

    resolveParticleCollisions(0, aliveParticles, index, ref particle, ref speciesData, ref totalDepenetration, ref numCollisions);

    if (numCollisions > 0) {
      particle.position += totalDepenetration / numCollisions;
    }
  }

  private void resolveParticleCollisions2x2(int index, ref Particle particle, ref SpeciesData speciesData) {
    Vector3 chunkFloatPos = particle.position / _chunkSize + Vector3.one * _chunkSide * 0.5f;
    ChunkIndex chunkIndex = new ChunkIndex(chunkFloatPos);

    chunkIndex.x += (frac(chunkFloatPos.x) < 0.5) ? -1 : 0;
    int offsetY = (frac(chunkFloatPos.y) > 0.5f) ? 1 : -1;
    int offsetZ = (frac(chunkFloatPos.z) > 0.5f) ? 1 : -1;

    int numCollisions = 0;
    Vector3 totalDepenetration = Vector3.zero;

    int chunkA = getChunkAtOffset(chunkIndex, 0, offsetY, 0);
    int chunkA_Start = _chunkStart[chunkA];
    int chunkA_End = _chunkEnd[chunkA + 1];

    resolveParticleCollisions(chunkA_Start, chunkA_End, index, ref particle, ref speciesData, ref totalDepenetration, ref numCollisions);

    int chunkB = getChunkAtOffset(chunkIndex, 0, 0, offsetZ);
    int chunkB_Start = _chunkStart[chunkB];
    int chunkB_End = _chunkEnd[chunkB + 1];

    resolveParticleCollisions(chunkB_Start, chunkB_End, index, ref particle, ref speciesData, ref totalDepenetration, ref numCollisions);

    int chunkC = getChunkAtOffset(chunkIndex, 0, offsetY, offsetZ);
    int chunkC_Start = _chunkStart[chunkC];
    int chunkC_End = _chunkEnd[chunkC + 1];

    resolveParticleCollisions(chunkC_Start, chunkC_End, index, ref particle, ref speciesData, ref totalDepenetration, ref numCollisions);

    int chunkD = getChunkAtOffset(chunkIndex, 0, 0, 0);
    int chunkD_Start = _chunkStart[chunkD];
    int chunkD_End = _chunkEnd[chunkD + 1];

    resolveParticleCollisions(chunkD_Start, chunkD_End, index, ref particle, ref speciesData, ref totalDepenetration, ref numCollisions);

    if (numCollisions > 0) {
      particle.position += totalDepenetration / numCollisions;
    }
  }

  private void resolveParticleCollisions(int start,
                                         int end,
                                         int toSkip,
                                     ref Particle particle,
                                     ref SpeciesData speciesData,
                                     ref Vector3 totalDepenetration,
                                     ref int numCollisions) {

    if (toSkip >= start && toSkip <= end) {
      resolveParticleCollisions(start, toSkip, ref particle, ref speciesData, ref totalDepenetration, ref numCollisions);
      resolveParticleCollisions(toSkip + 1, end, ref particle, ref speciesData, ref totalDepenetration, ref numCollisions);
    } else {
      resolveParticleCollisions(start, end, ref particle, ref speciesData, ref totalDepenetration, ref numCollisions);
    }
  }

  private void resolveParticleCollisions(int start,
                                         int end,
                                     ref Particle particle,
                                     ref SpeciesData speciesData,
                                     ref Vector3 totalDepenetration,
                                     ref int numCollisions) {
    for (int i = start; i < end; i++) {
      DoParticleCollision(ref particle,
                          ref speciesData,
                          ref _particlesFront[i],
                          ref _speciesData[_particlesFront[i].species],
                          ref totalDepenetration,
                          ref numCollisions);
    }
  }

  private int getChunk(ref Particle particle) {
    Vector3 floatPos = particle.position / _chunkSize + Vector3.one * _chunkSide * 0.5f;
    return getChunk(new ChunkIndex(floatPos));
  }

  private int getChunk(ChunkIndex index) {
    return index.x + index.y * _chunkSide + index.z * _chunkSide * _chunkSide;
  }

  private int getChunkAtOffset(ChunkIndex index, int offsetX, int offsetY, int offsetZ) {
    return index.x + offsetX + (index.y + offsetY) * _chunkSide + (index.z + offsetZ) * _chunkSide * _chunkSide;
  }


  private float frac(float value) {
    return value - (int)value;
  }

  private struct ChunkIndex {
    public int x, y, z;

    public ChunkIndex(Vector3 position) {
      x = (int)position.x;
      y = (int)position.y;
      z = (int)position.z;
    }

    public static ChunkIndex operator +(ChunkIndex a, ChunkIndex b) {
      return new ChunkIndex() { x = a.x + b.x, y = a.y + b.y, z = a.z + b.z };
    }
  }
  #endregion
}
