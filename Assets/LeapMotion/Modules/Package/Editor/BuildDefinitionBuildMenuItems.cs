using UnityEditor;

namespace Leap.Unity.Packaging {

  public class BuildDefinitionBuildMenuItems { 

    // Galaxies
    [MenuItem("Build/Galaxies", priority = 20)]
    public static void Build_d1d88f9c232529d4a87841795c181229() {
      BuildDefinition.Build("d1d88f9c232529d4a87841795c181229");
    }

    // Solar System
    [MenuItem("Build/Solar System", priority = 20)]
    public static void Build_7eb6de42354fe7a40bd29dc50a3e3b0f() {
      BuildDefinition.Build("7eb6de42354fe7a40bd29dc50a3e3b0f");
    }
  }
}

