using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.GalaxySim {

  public class UIButton_AddBlackHole : UIButton{

    public override void OnPress() {
      GalaxyUIOperations.AddBlackHole();
    }

  }

}
