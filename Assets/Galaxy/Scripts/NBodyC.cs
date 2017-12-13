using System.Runtime.InteropServices;

public static class NBodyC {

  [DllImport("NBody")]
  public static unsafe extern int GetOffsetOfVelocity();

  [DllImport("NBody")]
  public static unsafe extern void SetGravity(float gravity);

  [DllImport("NBody")]
  public static unsafe extern GalaxySimulation.UniverseState* CreateGalaxy(int numBodies);

  [DllImport("NBody")]
  public static unsafe extern void DestroyGalaxy(GalaxySimulation.UniverseState* ptr);

  [DllImport("NBody")]
  public static unsafe extern void CopyGalaxy(GalaxySimulation.UniverseState* from, GalaxySimulation.UniverseState* to);

  [DllImport("NBody")]
  public static unsafe extern void StepGalaxy(GalaxySimulation.UniverseState* ptr);

  public static unsafe GalaxySimulation.UniverseState* Clone(GalaxySimulation.UniverseState* src) {
    var dst = CreateGalaxy(src->numBlackHoles);
    CopyGalaxy(src, dst);
    return dst;
  }

}
