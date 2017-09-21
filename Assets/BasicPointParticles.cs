using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;
using Leap.Unity.Query;
using Leap.Unity.Attributes;
using Leap.Unity.DevGui;

public class BasicPointParticles : DevBehaviour {
  public const float TIME_FREEZE_THRESHOLD = 0.05f;

  //#######################
  //## General Settings ###
  //#######################
  [Header("General Settings")]

  public bool render = true;

  [DevValue]
  public bool useQuads = true;

  public KeyCode resetKeycode = KeyCode.Space;

  [DevValue]
  public bool loop = false;

  [Range(1, 1000)]
  [DevValue]
  public int loopTime = 10;

  [Range(0, 2)]
  [DevValue]
  public float timestep = 1;

  [Range(0.05f, 2f)]
  [DevValue]
  public float scale = 1;

  //#####################
  //### Star Settings ###
  //#####################
  [Header("Star Settings")]

  public bool simulateStars = true;

  [Range(0, 0.05f)]
  [DevCategory("Stars")]
  [DevValue("Particle Size")]
  public float starSize = 0.05f;

  [Range(0, 1)]
  [DevCategory("Stars")]
  [DevValue]
  public float starBrightness = 0.1f;

  [DevCategory("Stars")]
  [DevValue("Grav Constant")]
  public float starGravConstant = 5e-05f;

  [DevCategory("Stars")]
  [DevValue]
  [Range(0, 2)]
  public float minDiscRadius = 0.01f;

  [Range(0, 2)]
  [DevCategory("Stars")]
  [DevValue]
  public float maxDiscRadius = 1;

  [Range(0, 0.5f)]
  [DevCategory("Stars")]
  [DevValue]
  public float maxDiscHeight = 1;

  public AnimationCurve radiusDistribution;

  //###########################
  //### Black Hole Settings ###
  //###########################
  [Header("Black Hole Settings")]

  public bool simulateBlackHoles = true;

  public int blackHoleSubFrames = 10;

  public Mesh blackHoleMesh;

  public Material blackHoleMaterial;

  [Range(0, 1)]
  [DevCategory("Black Holes")]
  [DevValue("Mass Variance")]
  public float blackHoleMassVariance = 0;

  [Range(0, 1)]
  [DevCategory("Black Holes")]
  [DevValue("Mass Affects Radius")]
  public float blackHoleMassAffectsSize = 1;

  [Range(0, 1)]
  [DevCategory("Black Holes")]
  [DevValue("Mass Affects Density")]
  public float blackHoleMassAffectsDensity = 1;

  [DevCategory("Black Holes")]
  [DevValue("Draw")]
  public bool renderBlackHoles = true;

  [Range(1, 10)]
  [DevCategory("Black Holes")]
  [DevValue("Count")]
  public int blackHoleCount = 3;

  [MinValue(0)]
  [DevCategory("Black Holes")]
  [DevValue]
  public float gravConstant = 0.0001f;

  [MinValue(0)]
  [DevCategory("Black Holes")]
  [DevValue("Start Velocity")]
  public float blackHoleVelocity = 0.1f;

  [Range(0, 1)]
  [DevCategory("Black Holes")]
  [DevValue("Direction Variance")]
  public float initialDirVariance = 0;

  [Range(0, 4)]
  [DevCategory("Black Holes")]
  [DevValue("Spawn Radius")]
  public float blackHoleSpawnRadius = 0.5f;

  [Range(0, 0.1f)]
  [DevCategory("Black Holes")]
  [DevValue("Combine Dist")]
  public float blackHoleCombineDistance = 0.05f;

  //##################
  //### References ###
  //##################
  [Header("References")]
  public RenderTexture prevPos;
  public RenderTexture currPos;
  public RenderTexture nextPos;

  public Material displayMat;
  public Material quadMat;
  public Material simulateMat;
  public Material gammaBlit;

  private float _prevTimestep = -1000;

  private struct BlackHole {
    public Vector3 position;
    public Vector3 velocity;
    public Quaternion rotation;
    public float mass;
  }

  private List<BlackHole> blackHoles = new List<BlackHole>();

  private float[] _floatArray = new float[32];
  private Vector4[] _vectorArray = new Vector4[32];
  private Matrix4x4[] _matrixArray = new Matrix4x4[32];

  private IEnumerator Start() {
    prevPos.Create();
    currPos.Create();
    nextPos.Create();

    prevPos.DiscardContents();
    currPos.DiscardContents();
    nextPos.DiscardContents();

    if (!displayMat.shader.isSupported) {
      FindObjectOfType<Renderer>().material.color = Color.red;
    }

    initGalaxies();
    yield return null;
    yield return null;
    initGalaxies();
  }

  private void OnEnable() {
    Camera.onPostRender += drawStars;
  }

  private void OnDisable() {
    Camera.onPostRender -= drawStars;
  }

  private void updateShaderConstants() {
    simulateMat.SetFloat("_MinDiscRadius", minDiscRadius);
    simulateMat.SetFloat("_MaxDiscRadius", maxDiscRadius);
    simulateMat.SetFloat("_MaxDiscHeight", maxDiscHeight);

    blackHoles.Query().Select(t => {
      Vector4 planet = t.position;
      planet.w = t.mass;
      return planet;
    }).FillArray(_vectorArray);
    simulateMat.SetVectorArray("_Planets", _vectorArray);

    blackHoles.Query().Select(t => Matrix4x4.Rotate(t.rotation)).FillArray(_matrixArray);
    simulateMat.SetMatrixArray("_PlanetRotations", _matrixArray);

    simulateMat.SetInt("_PlanetCount", blackHoles.Count);

    displayMat.SetFloat("_Size", starSize);
    displayMat.SetFloat("_Bright", starBrightness);
    quadMat.SetFloat("_Size", starSize);
    quadMat.SetFloat("_Bright", starBrightness);

    simulateMat.SetFloat("_Force", starGravConstant);

    if (timestep > TIME_FREEZE_THRESHOLD) {
      simulateMat.SetFloat("_Timestep", timestep);
      simulateMat.SetFloat("_PrevTimestep", _prevTimestep);

      _prevTimestep = timestep;
    }
  }

  [DevButton("Reset Sim")]
  private void initGalaxies() {
    _prevTimestep = timestep;

    blackHoles.Clear();

    Random.InitState(seed);
    for (int i = 0; i < blackHoleCount; i++) {
      Vector3 position = Random.onUnitSphere * blackHoleSpawnRadius;

      blackHoles.Add(new BlackHole() {
        position = position,
        rotation = Random.rotationUniform,
        velocity = Vector3.Slerp(Vector3.zero - position, Random.onUnitSphere, initialDirVariance).normalized * blackHoleVelocity,
        mass = Random.Range(1 - blackHoleMassVariance, 1 + blackHoleMassVariance)
      });
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

    blackHoles.Query().Select(t => (Vector4)t.velocity).FillArray(_vectorArray);
    simulateMat.SetVectorArray("_PlanetVelocities", _vectorArray);

    _floatArray.Fill(0);
    blackHoles.Query().Select(t => Mathf.Lerp(1, t.mass, blackHoleMassAffectsDensity)).FillArray(_floatArray);
    simulateMat.SetFloatArray("_PlanetDensities", _floatArray);
    simulateMat.SetFloat("_TotalDensity", _floatArray.Query().Fold((a, b) => a + b));

    blackHoles.Query().Select(t => Mathf.Lerp(1, t.mass, blackHoleMassAffectsSize)).FillArray(_floatArray);
    simulateMat.SetFloatArray("_PlanetSizes", _floatArray);

    GL.LoadPixelMatrix(0, 1, 0, 1);

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
  }

  int seed = 0;

  private void Update() {
    if (Input.GetKeyDown(resetKeycode)) {
      initGalaxies();
    }

    if (loop && Time.frameCount % loopTime == 0) {
      initGalaxies();
      quadMat.mainTexture = currPos;
      displayMat.mainTexture = currPos;
      return;
    }

    Random.InitState(Time.frameCount);
    seed = Random.Range(int.MinValue, int.MaxValue);

    if (timestep > TIME_FREEZE_THRESHOLD) {
      if (simulateBlackHoles) {
        float planetDT = 1.0f / blackHoleSubFrames;
        for (int stepVar = 0; stepVar < blackHoleSubFrames; stepVar++) {

          for (int j = 0; j < blackHoles.Count; j++) {
            BlackHole blackHole = blackHoles[j];

            for (int k = 0; k < blackHoles.Count; k++) {
              if (j == k) continue;

              BlackHole other = blackHoles[k];

              Vector3 toOther = other.position - blackHole.position;
              float dist = toOther.magnitude;
              Vector3 force = gravConstant * (toOther / dist) / (dist * dist);
              blackHole.velocity += blackHole.mass * other.mass * force * planetDT * timestep;
            }

            blackHoles[j] = blackHole;
          }

          for (int j = 0; j < blackHoles.Count; j++) {
            BlackHole blackHole = blackHoles[j];
            blackHole.position += blackHole.velocity * planetDT * timestep;

            if (renderBlackHoles) {
              Graphics.DrawMesh(blackHoleMesh, Matrix4x4.Scale(Vector3.one * scale) * Matrix4x4.TRS(blackHole.position, Quaternion.identity, Vector3.one * 0.01f), blackHoleMaterial, 0);
            }

            blackHoles[j] = blackHole;
          }

          for (int j = 0; j < blackHoles.Count; j++) {
            BlackHole blackHole = blackHoles[j];

            for (int k = j + 1; k < blackHoles.Count; k++) {
              BlackHole other = blackHoles[k];

              float distToOther = Vector3.Distance(other.position, blackHole.position);
              if (distToOther < blackHoleCombineDistance) {
                blackHoles.RemoveAtUnordered(k);

                blackHole.position = (blackHole.position * blackHole.mass + other.position * other.mass) / (blackHole.mass + other.mass);
                blackHole.velocity = (blackHole.velocity * blackHole.mass + other.velocity * other.mass) / (blackHole.mass + other.mass);
                blackHole.mass += other.mass;

                k--;
              }
            }

            blackHoles[j] = blackHole;
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

    quadMat.mainTexture = currPos;
    displayMat.mainTexture = currPos;
  }

  public void SetSize(float per) {
    displayMat.SetFloat("_Size", per.Map(0, 1, 0, 20));
  }

  public void SetBright(float per) {
    displayMat.SetFloat("_Bright", per.Map(0, 1, 0, 0.01f));
  }

  //bool hasDoneIt = false;
  //void OnRenderImage(Texture src, RenderTexture dst) {
  //  if (!hasDoneIt) {
  //    CommandBuffer buffer = new CommandBuffer();
  //    dothisNow(buffer, src.width, src.height);
  //    GetComponent<Camera>().AddCommandBuffer(CameraEvent.AfterForwardOpaque, buffer);
  //    hasDoneIt = true;
  //  }

  //  Graphics.Blit(src, dst);
  //}

  //void dothisNow(CommandBuffer buffer, int width, int height) {
  //  RenderTargetIdentifier id = new RenderTargetIdentifier(123);

  //  buffer.GetTemporaryRT(123, width / 1, height / 1, 0, FilterMode.Bilinear, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear, 8);

  //  buffer.SetRenderTarget(id);
  //  buffer.ClearRenderTarget(clearDepth: true, clearColor: true, backgroundColor: Color.black);

  //  buffer.DrawProcedural(Matrix4x4.identity, displayMat, 0, MeshTopology.Points, prevPos.width * prevPos.height);

  //  buffer.Blit(id, BuiltinRenderTextureType.CameraTarget, gammaBlit);

  //  buffer.ReleaseTemporaryRT(123);
  //}

  private void drawStars(Camera camera) {
    if (useQuads) {
      quadMat.SetFloat("_Scale", scale);
      quadMat.SetPass(0);
    } else {
      displayMat.SetFloat("_Scale", scale);
      displayMat.SetPass(0);
    }

    Graphics.DrawProcedural(MeshTopology.Points, prevPos.width * prevPos.height);
  }
}
