using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;
using Leap.Unity.Attributes;

public abstract class ParticleEngineBase : MonoBehaviour {

  [Header("Simulation")]
  [MinValue(0)]
  [EditTimeOnly]
  [SerializeField]
  private int _maxParticles = 1024;

  [SerializeField]
  private bool _useMultithreading = false;

  [Header("Collision Chunks")]
  [EditTimeOnly]
  [SerializeField]
  private BoxCollider _collisionBounds;

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
  private int[] _count;
  private int _aliveParticles = 0;
  private Queue<Particle> _toEmit = new Queue<Particle>();

  private SpeciesData[] _speciesData;
  private SocialData[] _socialData;

  //Threading
  private ParallelForeach _integrationForeach;
  private ParallelForeach _accumulationForeach;
  private ParallelForeach _sortingForeach;

  //Collision acceleration structures
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
    _accumulationForeach = new ParallelForeach(accumulateCollisionChunksNaive);
    _sortingForeach = new ParallelForeach(sortParticlesIntoChunks);

    _integrationForeach.OnComplete += () => {
      emitParticles();
      //_accumulationForeach.Dispatch(0);
    };

    _accumulationForeach.OnComplete += () => {
      _sortingForeach.Dispatch(0);
    };

    _particlesBack = new Particle[_maxParticles];
    _particlesFront = new Particle[_maxParticles];
    _count = new int[_maxParticles];
    _speciesData = new SpeciesData[1];
  }

  protected virtual void Update() {
    if (_useMultithreading) {
      _integrationForeach.Dispatch(_aliveParticles);
    } else {
      using (new ProfilerSample("Integrate Particles")) {
        integrateParticles(0, _aliveParticles);
      }

      using (new ProfilerSample("Emit Particles")) {
        emitParticles();
      }

      /*
      using (new ProfilerSample("Accumulate Collision Chunks")) {
        accumulateCollisionChunksNaive(0, 0);
      }

      using (new ProfilerSample("Sort Particles Into Chunks")) {
        sortParticlesIntoChunks(0, 0);
      }
      */
    }
  }

  protected virtual void LateUpdate() {
    if (_useMultithreading) {
      using (new ProfilerSample("Wait For Simulation Jobs")) {
        _integrationForeach.Wait();
        _accumulationForeach.Wait();
        _sortingForeach.Wait();
      }
    }

    if (_renderMethod == DisplayMethod.DrawMesh) {
      for (int i = 0; i < _aliveParticles; i++) {
        var matrix = Matrix4x4.TRS(_particlesFront[i].position, Quaternion.identity, Vector3.one * 0.05f);
        Graphics.DrawMesh(_particleMesh, matrix, _displayMaterial, 0);
      }
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
      _count[emitChunk]++;

      //Plop the particle onto the end of the front array, will be sorted into
      //the right chunk by the next accumulate/sort cycle
      _particlesFront[_aliveParticles++] = toEmit;
    }
  }

  private void integrateParticles(int startIndex, int endIndex) {
    for (int i = startIndex; i < endIndex; i++) {
      integrateParticle(i, ref _particlesFront[i], ref _speciesData[_particlesFront[i].species]);
    }
  }

  private void integrateParticle(int index, ref Particle particle, ref SpeciesData speciesData) {
    Vector3 originalPos = particle.position;

    particle.position += (particle.position - particle.prevPosition);

    DoParticleGlobalForces(ref particle, ref speciesData);

    //TODO: particle-particle forces

    //resolveParticleCollisionsNaive(index, ref particle, ref speciesData);
    //resolveParticleCollisions2x2(index, ref particle, ref speciesData);

    DoParticleConstraints(ref particle, ref speciesData);

    particle.prevPosition = originalPos;

    //int newChunk = getChunk(ref particle);
    //Interlocked.Add(ref _count[newChunk], 1);
  }

  private void accumulateCollisionChunksNaive(int startIndex, int endIndex) {
    for (int i = startIndex; i < endIndex; i++) {

      int sum = 0;
      for (int j = 0; j <= i; j++) {
        sum += _count[j];
      }

      _chunkStart[i] = sum;
      _chunkEnd[i] = sum;
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
    _particlesBack[newIndex] = particle;
  }

  private void resolveParticleCollisionsNaive(int index, ref Particle particle, ref SpeciesData speciesData) {
    Vector3 totalDepenetration = Vector3.zero;
    int numCollisions = 0;

    resolveParticleCollisions(0, index, ref particle, ref speciesData, ref totalDepenetration, ref numCollisions);
    resolveParticleCollisions(index + 1, aliveParticles, ref particle, ref speciesData, ref totalDepenetration, ref numCollisions);

    if (numCollisions > 0) {
      particle.position += totalDepenetration / numCollisions;
    }
  }

  private void resolveParticleCollisions2x2(int index, ref Particle particle, ref SpeciesData speciesData) {
    Vector3 chunkFloatPos = particle.position / 1 + Vector3.zero;
    ChunkIndex chunkIndex = new ChunkIndex(chunkFloatPos);

    chunkIndex.x += frac(chunkFloatPos.x) < 0.5 ? -1 : 0;
    int offsetY = frac(chunkFloatPos.y) > 0.5f ? 1 : -1;
    int offsetZ = frac(chunkFloatPos.z) > 0.5f ? 1 : -1;

    int numCollisions = 0;
    Vector3 totalDepenetration = Vector3.zero;

    int chunkA = getChunkPlusYZ(chunkIndex, offsetY, 0);
    int chunkA_Start = _chunkStart[chunkA];
    int chunkA_End = _chunkEnd[chunkA + 1];

    resolveParticleCollisions(chunkA_Start, chunkA_End, ref particle, ref speciesData, ref totalDepenetration, ref numCollisions);

    int chunkB = getChunkPlusYZ(chunkIndex, 0, offsetZ);
    int chunkB_Start = _chunkStart[chunkA];
    int chunkB_End = _chunkEnd[chunkA + 1];

    resolveParticleCollisions(chunkB_Start, chunkB_End, ref particle, ref speciesData, ref totalDepenetration, ref numCollisions);

    int chunkC = getChunkPlusYZ(chunkIndex, offsetY, offsetZ);
    int chunkC_Start = _chunkStart[chunkC];
    int chunkC_End = _chunkEnd[chunkC + 1];

    resolveParticleCollisions(chunkC_Start, chunkC_End, ref particle, ref speciesData, ref totalDepenetration, ref numCollisions);

    int chunkD = getChunk(chunkIndex);
    int chunkD_Start = _chunkStart[chunkD];
    int chunkD_End = index;

    resolveParticleCollisions(chunkD_Start, chunkD_End, ref particle, ref speciesData, ref totalDepenetration, ref numCollisions);

    int chunkE_Start = index + 1;
    int chunkE_End = _chunkEnd[chunkD + 1];

    resolveParticleCollisions(chunkE_Start, chunkE_End, ref particle, ref speciesData, ref totalDepenetration, ref numCollisions);

    if (numCollisions > 0) {
      particle.position += totalDepenetration / numCollisions;
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
    return 0;
  }

  private int getChunk(ChunkIndex index) {
    return 0;
  }

  private int getChunkPlusYZ(ChunkIndex index, int offsetY, int offsetZ) {
    return 0;
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
