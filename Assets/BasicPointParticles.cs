using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;
using Leap.Unity.Query;
using Leap.Unity.Attributes;
using Leap.Unity.DevGui;

public class BasicPointParticles : DevBehaviour {

  [Header("Display")]
  public bool render = true;

  [DevValue]
  public bool useQuads = true;

  public bool loop = true;

  [DevValue]
  [Range(1, 1000)]
  public int loopTime = 10;

  [Header("Settings")]

  [DevCategory("Stars")]
  [DevValue("Particle Size")]
  [Range(0, 0.05f)]
  public float size = 0.05f;

  [DevCategory("Stars")]
  [DevValue]
  [Range(0, 1)]
  public float brightness = 0.1f;

  [DevCategory("Stars")]
  [DevValue("Grav Constant")]
  public float starGravConstant = 5e-05f;

  [Header("Stars")]
  public bool simulateStars = true;
  public int frameSkip = 1;

  [DevCategory("Stars")]
  [DevValue]
  [Range(0, 2)]
  public float minDiscRadius = 0.01f;

  [DevCategory("Stars")]
  [DevValue]
  [Range(0, 2)]
  public float maxDiscRadius = 1;

  [DevCategory("Stars")]
  [DevValue]
  [Range(0, 0.5f)]
  public float maxDiscHeight = 1;

  public AnimationCurve radiusDistribution;

  [Header("Planets")]
  public bool simulatePlanets = true;
  public int planetSubframes = 10;

  public GameObject blackHolePrefab;

  [DevCategory("Black Holes")]
  [DevValue("Count")]
  [Range(1, 10)]
  public int blackHoleCount = 3;

  [DevCategory("Black Holes")]
  [MinValue(0)]
  [DevValue]
  public float gravConstant = 0.0001f;

  [MinValue(0)]
  [DevCategory("Black Holes")]
  [DevValue("Start Velocity")]
  public float planetVelocity = 0.1f;

  [Range(0, 1)]
  [DevCategory("Black Holes")]
  [DevValue("Initial Direction Variance")]
  public float initialDirVariance = 0;

  [Range(0, 4)]
  [DevCategory("Black Holes")]
  [DevValue]
  public float planetSpawnRadius = 0.5f;

  [Header("References")]
  public RenderTexture prevPos;
  public RenderTexture currPos;
  public RenderTexture nextPos;

  public Material displayMat;
  public Material quadMat;
  public Material simulateMat;
  public Material gammaBlit;

  private Transform[] planets;
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

    displayMat.SetFloat("_Size", size);
    displayMat.SetFloat("_Bright", brightness);
    quadMat.SetFloat("_Size", size);
    quadMat.SetFloat("_Bright", brightness);

    simulateMat.SetFloat("_Force", starGravConstant);
  }

  private void initGalaxies() {
    if (planets != null) {
      foreach (var planet in planets) {
        DestroyImmediate(planet.gameObject);
      }
    }

    planets = new Transform[blackHoleCount];
    planetPositions = new Vector4[blackHoleCount];
    planetRotations = new Matrix4x4[blackHoleCount];
    planetVelocities = new Vector3[blackHoleCount];
    planets.Fill(() => {
      var obj = Instantiate(blackHolePrefab);
      obj.SetActive(true);
      return obj.transform;
    });

    Random.InitState(seed);
    for (int i = 0; i < planets.Length; i++) {
      planets[i].position = Random.onUnitSphere * planetSpawnRadius;
      planets[i].rotation = Random.rotationUniform;
      planetVelocities[i] = Vector3.Slerp(Vector3.zero - planets[i].position, Random.onUnitSphere, initialDirVariance).normalized * planetVelocity;
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

    simulateMat.SetVectorArray("_PlanetVelocities", planetVelocities.Query().Select(t => (Vector4)t).ToArray());

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
    if (loop && Time.frameCount % loopTime == 0) {
      initGalaxies();
      quadMat.mainTexture = currPos;
      displayMat.mainTexture = currPos;
      return;
    }

    Random.InitState(Time.frameCount);
    seed = Random.Range(int.MinValue, int.MaxValue);

    if (simulatePlanets) {
      float planetDT = 1.0f / planetSubframes;
      for (int i = 0; i < planetSubframes; i++) {
        for (int j = 0; j < planets.Length; j++) {
          for (int k = 0; k < planets.Length; k++) {
            if (j == k) continue;

            Vector3 toOther = planets[k].position - planets[j].position;
            float dist = toOther.magnitude;
            Vector3 force = gravConstant * (toOther / dist) / (dist * dist);
            planetVelocities[j] += force * planetDT;
          }
        }

        for (int j = 0; j < planets.Length; j++) {
          planets[j].position += planetVelocities[j] * planetDT;
        }
      }
    }

    if (simulateStars) {
      updateShaderConstants();

      for (int i = 0; i < frameSkip; i++) {
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
      quadMat.SetPass(0);
    } else {
      displayMat.SetPass(0);
    }

    Graphics.DrawProcedural(MeshTopology.Points, prevPos.width * prevPos.height);
  }
}
