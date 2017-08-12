using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;

public class BasicPointParticles : MonoBehaviour {

  public RenderTexture pos0, pos1;
  public RenderTexture vel0, vel1;

  public Material displayMat;
  public Material simulateMat;

  public Transform target0, target1;

  private void Start() {
    simulateMat.SetTexture("_Positions", pos0);
    simulateMat.SetTexture("_Velocities", vel0);

    pos0.DiscardContents();
    vel0.DiscardContents();

    simulateMat.SetFloat("_Seed", Random.value);
    Graphics.Blit(null, pos0, simulateMat, 2);
    simulateMat.SetFloat("_Seed", Random.value);
    Graphics.Blit(null, vel0, simulateMat, 2);
  }

  private void FixedUpdate() {
    simulateMat.SetVector("_Target0", target0.position);
    simulateMat.SetVector("_Target1", target1.position);

    vel1.DiscardContents();
    simulateMat.SetTexture("_Positions", pos0);
    Graphics.Blit(vel0, vel1, simulateMat, 0);
    Utils.Swap(ref vel0, ref vel1);

    pos1.DiscardContents();
    simulateMat.SetTexture("_Velocities", vel0);
    Graphics.Blit(pos0, pos1, simulateMat, 1);
    Utils.Swap(ref pos0, ref pos1);
  }

  private void OnPostRender() {
    displayMat.mainTexture = pos0;
    displayMat.SetPass(0);
    Graphics.DrawProcedural(MeshTopology.Points, pos0.width * pos0.height);
  }


}
