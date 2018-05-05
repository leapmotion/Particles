using UnityEngine;

namespace Leap.Unity.Particles {

  public class SetTextGraphic_NumberOfSpeciesLabel : SetTextGraphicWithSimulatorParam {

    public override string GetTextValue() {
      if (simulatorSetters.IsCurrentEcosystemAPreset()) {
        return "Number of Species (requires Randomize)";
      }
      else {
        return "Number of Species";
      }
    }

  }

}
