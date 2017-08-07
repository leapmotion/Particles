using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ComputeTest : MonoBehaviour {

  public int[] strides;

  public ComputeShader shader;
  public Text label;
  public float perTest = 5;

  private ComputeBuffer velocities;
  private ComputeBuffer positions;

  private int maxParticles;
  private int velocityKernel;
  private int positionKernel;

  private IEnumerator Start() {
    yield return new WaitForSeconds(2);

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
          shader.Dispatch(velocityKernel, maxParticles / stride, 1, 1);
          shader.Dispatch(velocityKernel, maxParticles / stride, 1, 1);
          shader.Dispatch(velocityKernel, maxParticles / stride, 1, 1);
          frames++;
          yield return null;
        }

        Debug.Log("Finished");

        label.text = label.text + Mathf.RoundToInt(frames / perTest).ToString() + "      ";
      }

      label.text = label.text + "\n";
    }
  }

  private void begin(int stride) {
    end();

    maxParticles = (4096 / stride) * stride;

    Debug.Log("Creating velocity... " + maxParticles);
    velocities = new ComputeBuffer(maxParticles, sizeof(float) * 4);
    Debug.Log("Created");

    Debug.Log("Creating position... " + maxParticles);
    positions = new ComputeBuffer(maxParticles, sizeof(float) * 4);
    Debug.Log("Created");

    Debug.Log("Finding Kernels");
    velocityKernel = shader.FindKernel("UpdateVelocity_" + stride);
    Debug.Log("Found");

    Debug.Log("Assigning buffers");
    foreach (var kernel in new int[] { velocityKernel }) {
      shader.SetBuffer(kernel, "_Positions", positions);
      shader.SetBuffer(kernel, "_Velocities", velocities);
    }
    Debug.Log("Assigned");

    Debug.Log("Velocity kernel: " + velocityKernel);
    Debug.Log("Position kernel: " + positionKernel);
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
