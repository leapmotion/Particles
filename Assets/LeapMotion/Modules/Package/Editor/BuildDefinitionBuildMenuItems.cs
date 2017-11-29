using UnityEditor;

namespace Leap.Unity.Packaging {

  public class BuildDefinitionBuildMenuItems { 

    // Galaxies
    [MenuItem("Build/Galaxies", priority = 20)]
    public static void Build_d1d88f9c232529d4a87841795c181229() {
      BuildDefinition.Build("d1d88f9c232529d4a87841795c181229");
    }
  }
}

