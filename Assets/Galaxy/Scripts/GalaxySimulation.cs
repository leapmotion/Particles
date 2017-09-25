using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;
using Leap.Unity.Query;
using Leap.Unity.Attributes;
using Leap.Unity.DevGui;

[DevCategory("General Settings")]
public class GalaxySimulation : MonoBehaviour {
  public const float TIME_FREEZE_THRESHOLD = 0.05f;

  //#######################
  //## General Settings ###
  //#######################
  [Header("General Settings")]
  public KeyCode resetKeycode = KeyCode.Space;

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

  [Range(1, 10)]
  [DevValue("Count")]
  public int blackHoleCount = 3;

  [MinValue(0)]
  [DevValue]
  public float gravConstant = 0.0001f;

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

  //##################
  //### References ###
  //##################
  [Header("References")]
  public RenderTexture prevPos;
  public RenderTexture currPos;
  public RenderTexture nextPos;

  public Material simulateMat;

  private float _prevTimestep = -1000;
  private float _simulationTime = 0;
  private int _seed = 0;

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

  [DevButton("Reset Sim")]
  public void ResetSimulation() {
    _prevTimestep = timestep;
    _simulationTime = 0;

    blackHoles.Clear();

    Random.InitState(_seed);
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

    simulateMat.SetFloat("_Force", starGravConstant);

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

    if (loop && _simulationTime > loopTime) {
      ResetSimulation();
      return;
    }

    Random.InitState(Time.frameCount);
    _seed = Random.Range(int.MinValue, int.MaxValue);

    if (timestep > TIME_FREEZE_THRESHOLD) {
      _simulationTime += timestep * Time.deltaTime;

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

        for (int j = 0; j < blackHoles.Count; j++) {
          BlackHole blackHole = blackHoles[j];
          galaxyRenderer.DrawBlackHole(blackHole.position);
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
}
