using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestRunner : MonoBehaviour {

  public TextureSimulator sim;
  public TextMesh texMesh;

  private string _results;

	IEnumerator Start () {
    yield return new WaitForSeconds(0.5f);
    sim.RestartSimulation();

    //while (true) {
    //  int startTime = Time.frameCount;
    //  yield return new WaitForSeconds(1f);
    //  int endTime = Time.frameCount;
    //  texMesh.text = Mathf.Round(endTime - startTime).ToString();
    //}

    //for (int i = 1; i <= 10; i++) {
    //  sim.dynamicSpeciesCount = i;
    //  sim.RestartSimulation(TextureSimulator.EcosystemPreset.TEST_Dynamic);
    //  yield return StartCoroutine(runTest());
    //  if ((i + 1) % 4 == 0) {
    //    _results += "\n";
    //  }
    //}

    /*
    yield return new WaitForSeconds(0.5f);
    sim.ResetPositions();
    yield return new WaitForSeconds(0.5f);

    for(int i=1; i<=4; i++) {
      float percent = i / 4.0f;

      sim.particlesToSimulate = Mathf.RoundToInt(Mathf.Lerp(1, TextureSimulator.MAX_PARTICLES, percent));

      sim.simulationEnabled = true;
      sim.displayParticles = true;
      yield return StartCoroutine(runTest());

      sim.simulationEnabled = true;
      sim.displayParticles = false;
      yield return StartCoroutine(runTest());

      sim.simulationEnabled = false;
      sim.displayParticles = true;
      yield return StartCoroutine(runTest());

      _results += '\n';
      texMesh.text = _results;
    }
    */
  }

  IEnumerator runTest() {
    yield return new WaitForSeconds(0.5f);

    int startFrameCount = Time.frameCount;
    yield return new WaitForSeconds(3f);
    int endFrameCount = Time.frameCount;
    float fps = (endFrameCount - startFrameCount) / 3.0f;

    _results += Mathf.RoundToInt(fps).ToString() + " - ";
    texMesh.text = _results;
  }
}
