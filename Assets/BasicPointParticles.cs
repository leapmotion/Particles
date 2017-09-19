using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;
using Leap.Unity.Query;
using Leap.Unity.Attributes;
using UnityEngine.Rendering;

public class BasicPointParticles : MonoBehaviour {

  [Header("Display")]
  public bool render = true;
  public bool useQuads = true;

  [Header("Settings")]
  public bool quads = true;
  public float size = 0.05f;

  [Header("Stars")]
  public bool simulateStars = true;
  public bool loop = true;
  public int loopTime = 10;
  public int frameSkip = 1;

  public float minDiscRadius = 0.01f;
  public float maxDiscRadius = 1;
  public float maxDiscHeight = 1;
  public AnimationCurve radiusDistribution;

  [Header("Planets")]
  public bool simulatePlanets = true;
  [MinValue(0)]
  public float gravConstant = 0.0001f;
  [MinValue(0)]
  public float randomVelocity = 0.1f;
  public float planetSpawnRadius = 0.5f;

  [Header("References")]
  public RenderTexture prevPos;
  public RenderTexture currPos;
  public RenderTexture nextPos;

  public Material displayMat;
  public Material quadMat;
  public Material simulateMat;
  public Material gammaBlit;

  public Transform[] planets;

  private Vector4[] planetPositions;
  private Vector3[] planetVelocities;
  private Matrix4x4[] planetRotations;

  private void Start() {
    prevPos.Create();
    currPos.Create();
    nextPos.Create();

    prevPos.DiscardContents();
    currPos.DiscardContents();
    nextPos.DiscardContents();

    planetPositions = new Vector4[planets.Length];
    planetRotations = new Matrix4x4[planets.Length];

    if (!displayMat.shader.isSupported) {
      FindObjectOfType<Renderer>().material.color = Color.red;
    }

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

    planets.Query().Select(t => (Vector4)t.position).FillArray(planetPositions);
    simulateMat.SetVectorArray("_Planets", planetPositions);

    planets.Query().Select(t => Matrix4x4.Rotate(t.rotation)).FillArray(planetRotations);
    simulateMat.SetMatrixArray("_PlanetRotations", planetRotations);

    simulateMat.SetInt("_PlanetCount", planets.Length);
  }

  private void initGalaxies() {
    Texture2D tex = new Texture2D(512, 1, TextureFormat.RFloat, mipmap: false, linear: true);
    for (int i = 0; i < tex.width; i++) {
      tex.SetPixel(i, 0, new Color(radiusDistribution.Evaluate(i / 512.0f), 0, 0, 0));
    }
    tex.Apply();
    tex.filterMode = FilterMode.Bilinear;
    tex.wrapMode = TextureWrapMode.Clamp;
    simulateMat.SetTexture("_RadiusDistribution", tex);

    updateShaderConstants();

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

    Random.InitState(seed);
    foreach (var planet in planets) {
      planet.position = Random.insideUnitSphere * planetSpawnRadius;
      planet.velocity = Random.insideUnitSphere * randomVelocity;
      planet.rotation = Random.rotationUniform;
    }
  }

  int seed = 0;

  private void Update() {
    if (loop && Time.frameCount % loopTime == 0) {
      initGalaxies();
      quadMat.mainTexture = currPos;
      displayMat.mainTexture = currPos;
      return;
    }

    Random.InitState(Time.frameCount);
    seed = Random.Range(int.MinValue, int.MaxValue);

    if (simulateStars) {
      for (int i = 0; i < frameSkip; i++) {
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

  private void FixedUpdate() {
    foreach (var planet1 in planets) {
      foreach (var planet2 in planets) {
        if (planet1 == planet2) continue;

        Vector3 toPlanet2 = planet2.position - planet1.position;
        float distance = toPlanet2.magnitude;
        Vector3 force = gravConstant * (toPlanet2 / distance) / (0.001f + distance * distance);

        planet1.AddForce(force, ForceMode.Acceleration);
      }
    }
  }

  public void SetSize(float per) {
    displayMat.SetFloat("_Size", per.Map(0, 1, 0, 20));
  }

  public void SetBright(float per) {
    displayMat.SetFloat("_Bright", per.Map(0, 1, 0, 0.01f));
  }

  void OnGUI() {
    GUILayout.Label(":::::::::::::::::::::::::::::::::::::   " + Mathf.RoundToInt(1.0f / Time.smoothDeltaTime));
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
      quadMat.SetPass(0);
    } else {
      displayMat.SetPass(0);
    }

    Graphics.DrawProcedural(MeshTopology.Points, prevPos.width * prevPos.height);
  }
}
