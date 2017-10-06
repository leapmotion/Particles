using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Leap.Unity;
using Leap.Unity.Query;
using Leap.Unity.Attributes;
using Leap.Unity.DevGui;
using Leap.Unity.RuntimeGizmos;

[DevCategory("General Settings")]
public class GalaxySimulation : MonoBehaviour {
  public const float TIME_FREEZE_THRESHOLD = 0.05f;

  //#######################
  //## General Settings ###
  //#######################
  [Header("General Settings")]
  public KeyCode resetKeycode = KeyCode.Space;

  [DevValue]
  public bool simulate = true;

  [DevValue]
  public bool respawnMode = false;

  [DevValue]
  public bool loop = false;

  [Range(0, 10)]
  [DevValue]
  public float loopTime = 10;

  [Range(0, 2)]
  [DevValue]
  public float timestep = 1;

  public GalaxyRenderer galaxyRenderer;

  //#####################
  //### Star Settings ###
  //#####################
  [Header("Stars"), DevCategory]
  public bool simulateStars = true;

  [DevValue("Grav Constant")]
  public float starGravConstant = 5e-05f;

  [DevValue]
  [Range(0, 2)]
  public float minDiscRadius = 0.01f;

  [Range(0, 2)]
  [DevValue]
  public float maxDiscRadius = 1;

  [Range(0, 0.5f)]
  [DevValue]
  public float maxDiscHeight = 1;

  public AnimationCurve radiusDistribution;

  //###########################
  //### Black Hole Settings ###
  //###########################
  [Header("Black Holes"), DevCategory]
  public bool simulateBlackHoles = true;

  public int blackHoleSubFrames = 10;

  [Range(0, 1)]
  [DevValue("Mass Variance")]
  public float blackHoleMassVariance = 0;

  [Range(0, 1)]
  [DevValue("Mass Affects Radius")]
  public float blackHoleMassAffectsSize = 1;

  [Range(0, 1)]
  [DevValue("Mass Affects Density")]
  public float blackHoleMassAffectsDensity = 1;

  [Range(1, 100)]
  [DevValue("Count")]
  public int blackHoleCount = 3;

  [MinValue(0)]
  [DevValue]
  public float gravConstant = 0.0001f;

  [Range(0, 0.001f)]
  [DevValue]
  public float fuzzValue = 0.0005f;

  [MinValue(0)]
  [DevValue("Start Velocity")]
  public float blackHoleVelocity = 0.1f;

  [Range(0, 1)]
  [DevValue("Direction Variance")]
  public float initialDirVariance = 0;

  [Range(0, 4)]
  [DevValue("Spawn Radius")]
  public float blackHoleSpawnRadius = 0.5f;

  [Range(0, 0.1f)]
  [DevValue("Combine Dist")]
  public float blackHoleCombineDistance = 0.05f;

  //####################
  //### Orbit Trails ###
  //####################
  [Header("Orbit Trails"), DevCategory]
  [SerializeField, DevValue]
  private bool _enableTrails = false;

  [Range(1, 10000)]
  [SerializeField, DevValue]
  private int _maxTrailLength = 100;

  [Range(2, 100)]
  [SerializeField, DevValue]
  private int _trailUpdateRate = 2;

  //##################
  //### References ###
  //##################
  [Header("References")]
  public RenderTexture prevPos;
  public RenderTexture currPos;
  public RenderTexture nextPos;

  public Material simulateMat;

  private float _prevTimestep = -1000;
  private int _seed = 0;
  private int _nextId = 0;

  [StructLayout(LayoutKind.Sequential)]
  public struct BlackHoleMainState {
    public static int SIZE;

    static BlackHoleMainState() {
      SIZE = Marshal.SizeOf(typeof(BlackHoleMainState));
    }

    public float x, y, z;
    public float vx, vy, vz;
    public float mass;

    public Vector3 position {
      get {
        return new Vector3(x, y, z);
      }
      set {
        x = value.x;
        y = value.y;
        z = value.z;
      }
    }

    public Vector3 velocity {
      get {
        return new Vector3(vx, vy, vz);
      }
      set {
        vx = value.x;
        vy = value.y;
        vz = value.z;
      }
    }
  }

  [StructLayout(LayoutKind.Sequential)]
  public struct BlackHoleSecondaryState {
    public static int SIZE;

    static BlackHoleSecondaryState() {
      SIZE = Marshal.SizeOf(typeof(BlackHoleSecondaryState));
    }

    public int id;
    public Quaternion rotation;
  }

  public unsafe class UniverseState {
    public float time;
    public int count;

    public byte* totalState;
    public BlackHoleMainState* mainState;
    public BlackHoleSecondaryState* secondaryState;

    public UniverseState(int count) {
      time = 0;
      this.count = count;

      int totalSize = (BlackHoleMainState.SIZE + BlackHoleSecondaryState.SIZE) * count;

      totalState = (byte*)Marshal.AllocHGlobal(totalSize);
      mainState = (BlackHoleMainState*)totalState;
      secondaryState = (BlackHoleSecondaryState*)(totalState + BlackHoleMainState.SIZE * count);
    }

    public void Dispose() {
      Marshal.FreeHGlobal((System.IntPtr)totalState);
    }

    public UniverseState Clone() {
      UniverseState clone = new UniverseState(count);
      clone.time = time;

      int toCopy = clone.count;

      BlackHoleMainState* srcMain = mainState;
      BlackHoleMainState* dstMain = clone.mainState;

      BlackHoleSecondaryState* srcSecond = secondaryState;
      BlackHoleSecondaryState* dstSecond = clone.secondaryState;
      do {
        *dstMain = *srcMain;
        *dstSecond = *srcSecond;
        dstMain++;
        srcMain++;
        dstSecond++;
        srcSecond++;
      } while (--toCopy != 0);

      return clone;
    }
  }

  public UniverseState mainState;
  private UniverseState _trailState;
  private Dictionary<int, List<Vector3>> _trails = new Dictionary<int, List<Vector3>>();

  private float[] _floatArray = new float[100];
  private Vector4[] _vectorArray = new Vector4[100];
  private Matrix4x4[] _matrixArray = new Matrix4x4[100];

  [DevButton("Reset Sim")]
  public unsafe void ResetSimulation() {
    if (mainState != null) {
      mainState.Dispose();
      mainState = null;
    }

    if (_trailState != null) {
      _trailState.Dispose();
      _trailState = null;
    }

    _trails.Clear();

    _prevTimestep = timestep;
    mainState = new UniverseState(blackHoleCount);

    {
      Random.InitState(_seed);
      BlackHoleMainState* dstMain = mainState.mainState;
      BlackHoleSecondaryState* dstSecond = mainState.secondaryState;

      for (int i = 0; i < blackHoleCount; i++) {
        Vector3 position = Random.onUnitSphere * blackHoleSpawnRadius;

        *dstMain = new BlackHoleMainState() {
          position = position,
          velocity = Vector3.Slerp(Vector3.zero - position, Random.onUnitSphere, initialDirVariance).normalized * blackHoleVelocity,
          mass = Random.Range(1 - blackHoleMassVariance, 1 + blackHoleMassVariance)
        };

        *dstSecond = new BlackHoleSecondaryState() {
          id = _nextId,
          rotation = Random.rotationUniform
        };

        _trails[_nextId] = new List<Vector3>();

        dstMain++;
        dstSecond++;
        _nextId++;
      }
    }

    Texture2D tex = new Texture2D(512, 1, TextureFormat.RFloat, mipmap: false, linear: true);
    for (int i = 0; i < tex.width; i++) {
      tex.SetPixel(i, 0, new Color(radiusDistribution.Evaluate(i / 512.0f), 0, 0, 0));
    }
    tex.Apply();
    tex.filterMode = FilterMode.Bilinear;
    tex.wrapMode = TextureWrapMode.Clamp;
    simulateMat.SetTexture("_RadiusDistribution", tex);

    updateShaderConstants();

    {
      BlackHoleMainState* src = mainState.mainState;
      for (int i = 0; i < mainState.count; i++, src++) {
        _vectorArray[i] = (*src).velocity;
      }
      simulateMat.SetVectorArray("_PlanetVelocities", _vectorArray);
    }

    {
      BlackHoleMainState* src = mainState.mainState;
      _floatArray.Fill(0);
      for (int i = 0; i < mainState.count; i++, src++) {
        _floatArray[i] = Mathf.Lerp(1, (*src).mass, blackHoleMassAffectsDensity);
      }
      simulateMat.SetFloatArray("_PlanetDensities", _floatArray);
      simulateMat.SetFloat("_TotalDensity", _floatArray.Query().Fold((a, b) => a + b));
    }

    {
      BlackHoleMainState* src = mainState.mainState;
      for (int i = 0; i < mainState.count; i++, src++) {
        _floatArray[i] = Mathf.Lerp(1, (*src).mass, blackHoleMassAffectsSize);
      }
      simulateMat.SetFloatArray("_PlanetSizes", _floatArray);
    }

    GL.LoadPixelMatrix(0, 1, 0, 1);

    prevPos.DiscardContents();
    currPos.DiscardContents();

    RenderBuffer[] buffer = new RenderBuffer[2];
    buffer[0] = prevPos.colorBuffer;
    buffer[1] = currPos.colorBuffer;
    Graphics.SetRenderTarget(buffer, prevPos.depthBuffer);

    simulateMat.SetPass(1);

    GL.Begin(GL.QUADS);
    GL.TexCoord2(0, 0);
    GL.Vertex3(0, 0, 0);
    GL.TexCoord2(1, 0);
    GL.Vertex3(1, 0, 0);
    GL.TexCoord2(1, 1);
    GL.Vertex3(1, 1, 0);
    GL.TexCoord2(0, 1);
    GL.Vertex3(0, 1, 0);
    GL.End();

    _trailState = mainState.Clone();
  }

  [DevButton]
  public void ResetTrails() {
    if (_trailState != null) {
      _trailState.Dispose();
      _trailState = null;
    }

    _trailState = mainState.Clone();

    _trails.Clear();
    unsafe {
      BlackHoleSecondaryState* src = mainState.secondaryState;
      for (int i = 0; i < mainState.count; i++, src++) {
        _trails[(*src).id] = new List<Vector3>();
      }
    }
  }

  private IEnumerator Start() {
    prevPos.Create();
    currPos.Create();
    nextPos.Create();

    prevPos.DiscardContents();
    currPos.DiscardContents();
    nextPos.DiscardContents();

    ResetSimulation();
    yield return null;
    yield return null;
    ResetSimulation();
  }

  private void OnDisable() {
    if (mainState != null) {
      mainState.Dispose();
      mainState = null;
    }

    if (_trailState != null) {
      _trailState.Dispose();
      _trailState = null;
    }
  }

  private unsafe void updateShaderConstants() {
    simulateMat.SetFloat("_MinDiscRadius", minDiscRadius);
    simulateMat.SetFloat("_MaxDiscRadius", maxDiscRadius);
    simulateMat.SetFloat("_MaxDiscHeight", maxDiscHeight);

    {
      BlackHoleMainState* src = mainState.mainState;
      for (int i = 0; i < mainState.count; i++, src++) {
        Vector4 planet = (*src).position;
        planet.w = (*src).mass;
        _vectorArray[i] = planet;
      }
      simulateMat.SetVectorArray("_Planets", _vectorArray);
    }

    {
      BlackHoleSecondaryState* src = mainState.secondaryState;
      for (int i = 0; i < mainState.count; i++, src++) {
        _matrixArray[i] = Matrix4x4.Rotate((*src).rotation);
      }
      simulateMat.SetMatrixArray("_PlanetRotations", _matrixArray);
    }

    simulateMat.SetInt("_PlanetCount", mainState.count);

    simulateMat.SetFloat("_Force", starGravConstant);
    simulateMat.SetFloat("_FuzzValue", fuzzValue);

    if (timestep > TIME_FREEZE_THRESHOLD) {
      simulateMat.SetFloat("_Timestep", timestep);
      simulateMat.SetFloat("_PrevTimestep", _prevTimestep);

      _prevTimestep = timestep;
    }
  }

  private void Update() {
    if (Input.GetKeyDown(resetKeycode)) {
      ResetSimulation();
    }

    if ((loop && mainState.time > loopTime) || respawnMode) {
      ResetSimulation();
      return;
    }

    Random.InitState(Time.frameCount);
    _seed = Random.Range(int.MinValue, int.MaxValue);

    if (_enableTrails) {
      for (int i = 0; i < _trailUpdateRate; i++) {
        stepState(_trailState);
        bool isAtMaxLength = false;

        unsafe {
          BlackHoleMainState* main = _trailState.mainState;
          BlackHoleSecondaryState* secondary = _trailState.secondaryState;
          for (int j = 0; j < _trailState.count; j++, main++, secondary++) {
            if (_trails[(*secondary).id].Count >= _maxTrailLength) {
              isAtMaxLength = true;
              continue;
            }

            _trails[(*secondary).id].Add((*main).position);
          }
        }

        if (isAtMaxLength) {
          break;
        }
      }

      RuntimeGizmoDrawer drawer;
      if (RuntimeGizmoManager.TryGetGizmoDrawer(out drawer)) {
        drawer.color = Color.white;
        foreach (var pair in _trails) {
          foreach (var seg in pair.Value.Query().Zip(Values.From(0), (a, b) => new KeyValuePair<int, Vector3>(b, a)).Where(p => p.Key % 16 == 0).Select(p => p.Value).WithPrevious()) {
            drawer.DrawLine(seg.prev, seg.value);
          }
        }
      }
    }

    if (timestep > TIME_FREEZE_THRESHOLD && simulate) {
      if (simulateBlackHoles) {
        stepState(mainState);
        renderState(mainState);

        foreach (var pair in _trails) {
          if (pair.Value.Count > 0) {
            pair.Value.RemoveAt(0);
          }
        }
      }

      if (simulateStars) {
        updateShaderConstants();

        nextPos.DiscardContents();
        Graphics.Blit(null, nextPos, simulateMat, 0);

        var tmp = prevPos;
        prevPos = currPos;
        currPos = nextPos;
        nextPos = tmp;

        simulateMat.SetTexture("_PrevPositions", prevPos);
        simulateMat.SetTexture("_CurrPositions", currPos);
      }
    }
  }

  private void LateUpdate() {
    galaxyRenderer.UpdatePositions(currPos, prevPos, nextPos);
  }

  private unsafe void stepState(UniverseState state) {
    using (new ProfilerSample("Step Galaxy")) {
      mainState.time += timestep * Time.deltaTime;
      float planetDT = 1.0f / blackHoleSubFrames;

      float preStepConstant = gravConstant * planetDT * timestep;

      for (int stepVar = 0; stepVar < blackHoleSubFrames; stepVar++) {

        //Force accumulation
        {
          BlackHoleMainState* srcA = state.mainState;
          for (int indexA = 0; indexA < state.count; indexA++, srcA++) {

            BlackHoleMainState* srcB = state.mainState + indexA + 1;
            for (int indexB = indexA + 1; indexB < state.count; indexB++, srcB++) {
              float toBX = (*srcB).x - (*srcA).x;
              float toBY = (*srcB).y - (*srcA).y;
              float toBZ = (*srcB).z - (*srcA).z;

              float dist = Mathf.Sqrt(toBX * toBX + toBY * toBY + toBZ * toBZ);
              float forceConst = (*srcA).mass * (*srcB).mass * preStepConstant / (dist * dist * dist);

              float forceX = toBX * forceConst;
              float forceY = toBY * forceConst;
              float forceZ = toBZ * forceConst;

              (*srcA).vx += forceX;
              (*srcA).vy += forceY;
              (*srcA).vz += forceZ;

              (*srcB).vx -= forceX;
              (*srcB).vy -= forceY;
              (*srcB).vz -= forceZ;
            }
          }
        }

        //Position intergration
        {
          BlackHoleMainState* src = state.mainState;
          float combinedDT = planetDT * timestep;
          for (int j = 0; j < state.count; j++, src++) {
            (*src).x += (*src).vx * combinedDT;
            (*src).y += (*src).vy * combinedDT;
            (*src).z += (*src).vz * combinedDT;
          }
        }

        //Black hole combination
        {
          float combineDistSqrd = blackHoleCombineDistance * blackHoleCombineDistance;

          BlackHoleMainState* mainA = state.mainState;
          BlackHoleSecondaryState* secondA = state.secondaryState;
          for (int indexA = 0; indexA < state.count; indexA++, mainA++, secondA++) {

            BlackHoleMainState* mainB = state.mainState + indexA + 1;
            BlackHoleSecondaryState* secondB = state.secondaryState + indexA + 1;
            for (int indexB = indexA + 1; indexB < state.count; indexB++, mainB++, secondB++) {
              float dx = (*mainA).x - (*mainB).x;
              float dy = (*mainA).y - (*mainB).y;
              float dz = (*mainA).z - (*mainB).z;

              float distSqrd = dx * dx + dy * dy + dz * dz;
              if (distSqrd <= combineDistSqrd) {
                float totalMass = (*mainA).mass + (*mainB).mass;
                (*mainA).x = ((*mainA).x * (*mainA).mass + (*mainB).x * (*mainB).mass) / totalMass;
                (*mainA).y = ((*mainA).y * (*mainA).mass + (*mainB).y * (*mainB).mass) / totalMass;
                (*mainA).z = ((*mainA).z * (*mainA).mass + (*mainB).z * (*mainB).mass) / totalMass;

                (*mainA).vx = ((*mainA).vx * (*mainA).mass + (*mainB).vx * (*mainB).mass) / totalMass;
                (*mainA).vy = ((*mainA).vy * (*mainA).mass + (*mainB).vy * (*mainB).mass) / totalMass;
                (*mainA).vz = ((*mainA).vz * (*mainA).mass + (*mainB).vz * (*mainB).mass) / totalMass;

                (*mainA).mass += (*mainB).mass;

                state.count--;
                *mainB = *(state.mainState + state.count);
                *secondB = *(state.secondaryState + state.count);

                indexB--;
                mainB--;
                secondB--;
              }
            }
          }
        }
      }
    }
  }

  private unsafe void renderState(UniverseState state) {
    BlackHoleMainState* src = state.mainState;
    for (int j = 0; j < state.count; j++, src++) {
      galaxyRenderer.DrawBlackHole((*src).position);
    }
  }
}
