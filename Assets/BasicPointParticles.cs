using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;
using Leap.Unity.Query;
using UnityEngine.Rendering;

public class BasicPointParticles : MonoBehaviour {

  public bool render = true;
  public bool simulate = true;
  public bool loop = true;
  public int loopTime = 10;
  public int frameSkip = 1;

  [Header("Settings")]
  public bool quads = true;
  public float size = 0.05f;

  [Header("Galazy")]
  public float minDiscRadius = 0.01f;
  public float maxDiscRadius = 1;
  public float maxDiscHeight = 1;

  [Header("References")]
  public RenderTexture pos0, pos1;
  public RenderTexture vel0, vel1;

  public Material displayMat;
  public Material simulateMat;
  public Material gammaBlit;

  public Transform[] planets;

  private Vector4[] planetPositions;
  private Matrix4x4[] planetRotations;

  private void Start() {
    simulateMat.SetTexture("_Positions", pos0);
    simulateMat.SetTexture("_Velocities", vel0);

    pos0.Create();
    vel0.Create();
    pos0.DiscardContents();
    vel0.DiscardContents();

    planetPositions = new Vector4[planets.Length];
    planetRotations = new Matrix4x4[planets.Length];

    if (!displayMat.shader.isSupported) {
      FindObjectOfType<Renderer>().material.color = Color.red;
    }

    initGalaxies();
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
    updateShaderConstants();

    GL.LoadPixelMatrix(0, 1, 0, 1);

    RenderBuffer[] buffer = new RenderBuffer[2];
    buffer[0] = pos0.colorBuffer;
    buffer[1] = vel0.colorBuffer;
    Graphics.SetRenderTarget(buffer, pos0.depthBuffer);

    simulateMat.SetPass(2);

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

  private void Update() {
    if (loop && Time.frameCount % loopTime == 0) {
      initGalaxies();
    }

    if (simulate) {
      for (int i = 0; i < frameSkip; i++) {
        updateShaderConstants();

        vel1.DiscardContents();
        Graphics.Blit(vel0, vel1, simulateMat, 0);
        simulateMat.SetTexture("_Velocities", vel1);
        Utils.Swap(ref vel0, ref vel1);

        pos1.DiscardContents();
        Graphics.Blit(pos0, pos1, simulateMat, 1);
        simulateMat.SetTexture("_Positions", pos1);
        Utils.Swap(ref pos0, ref pos1);
      }
    }

    displayMat.mainTexture = pos0;
    displayMat.SetTexture("_Velocity", vel0);
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

  bool hasDoneIt = false;
  void OnRenderImage(Texture src, RenderTexture dst) {
    if (!hasDoneIt) {
      CommandBuffer buffer = new CommandBuffer();
      dothisNow(buffer, src.width, src.height);
      GetComponent<Camera>().AddCommandBuffer(CameraEvent.AfterForwardOpaque, buffer);
      hasDoneIt = true;
    }

    Graphics.Blit(src, dst);
  }

  void dothisNow(CommandBuffer buffer, int width, int height) {
    RenderTargetIdentifier id = new RenderTargetIdentifier(123);

    buffer.GetTemporaryRT(123, width / 1, height / 1, 0, FilterMode.Bilinear, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear, 1);

    buffer.SetRenderTarget(id);
    buffer.ClearRenderTarget(clearDepth: true, clearColor: true, backgroundColor: Color.black);

    buffer.DrawProcedural(Matrix4x4.identity, displayMat, 0, MeshTopology.Points, pos0.width * pos0.height);

    buffer.Blit(id, BuiltinRenderTextureType.CameraTarget, gammaBlit);

    buffer.ReleaseTemporaryRT(123);
  }
}
