using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Leap.Unity.Query;

public class ComputeTest : MonoBehaviour {

  public bool useLayers = true;
  public int[] strides;

  public Material displayMat;

  public ComputeShader shader;
  public Text label;
  public float perTest = 5;

  private ComputeBuffer velocities;
  private ComputeBuffer positions;

  private int maxParticles;
  private int layers;

  private int randomizeKernel;
  private int velocityKernel;
  private int positionKernel;

  private IEnumerator Start() {
    while (true) {
      foreach (var stride in strides) {
        begin(stride);

        label.text = label.text + stride.ToString() + ":";

        yield return new WaitForSeconds(0.5f);

        Debug.Log("Begining dispatch");

        float startTime = Time.time;
        float endTime = Time.time + perTest;
        int frames = 0;
        while (Time.time < endTime) {

          for (int i = 0; i < 4; i++) {
            if (useLayers) {
              shader.Dispatch(velocityKernel, 4096 / stride, layers, 1);
            } else {
              shader.Dispatch(velocityKernel, maxParticles / stride, 1, 1);
            }
          }

          shader.Dispatch(positionKernel, maxParticles / stride, 1, 1);
          frames++;
          yield return null;
        }

        Debug.Log("Finished");

        label.text = label.text + Mathf.RoundToInt(frames / perTest).ToString() + "      ";
      }

      label.text = label.text + "\n";
    }
  }

  private void OnDisable() {
    end();
  }

  private void OnPostRender() {
    if (positions != null) {
      displayMat.SetPass(0);
      Graphics.DrawProcedural(MeshTopology.Points, 4096);
    }
  }

  private void begin(int stride) {
    end();

    if (useLayers) {
      layers = 1024 / stride;
      maxParticles = 4096;
    } else {
      maxParticles = (4096 / stride) * stride;
    }

    velocities = new ComputeBuffer(maxParticles, sizeof(float) * 4);
    positions = new ComputeBuffer(maxParticles, sizeof(float) * 4);

    positions.SetData(new Vector4[maxParticles].Fill(() => new Vector3(Random.value, Random.value * 0.1f, Random.value)));
    velocities.SetData(new Vector4[maxParticles]);

    if (useLayers) {
      velocityKernel = shader.FindKernel("UpdateVelocity_" + stride + "_" + layers);
    } else {
      velocityKernel = shader.FindKernel("UpdateVelocity_" + stride);
    }

    randomizeKernel = shader.FindKernel("RandomizePositions");
    positionKernel = shader.FindKernel("UpdatePosition");

    foreach (var kernel in new int[] { velocityKernel, positionKernel, randomizeKernel }) {
      shader.SetBuffer(kernel, "_Positions", positions);
      shader.SetBuffer(kernel, "_Velocities", velocities);
    }

    displayMat.SetBuffer("_Positions", positions);

    //shader.Dispatch(randomizeKernel, maxParticles, 1, 1);
  }

  private void end() {
    if (velocities != null) {
      Debug.Log("Release velocity");
      velocities.Release();
      velocities = null;
    }

    if (positions != null) {
      Debug.Log("Release Position");
      positions.Release();
      positions = null;
    }
  }
}
