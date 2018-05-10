using Leap.Unity.GraphicalRenderer;
using Leap.Unity.Interaction;
using UnityEngine;

namespace Leap.Unity.Particles {
  
  public class SetTextWithPrimaryHoverDistance : MonoBehaviour {

    public LeapTextGraphic textGraphic;
    public InteractionBehaviour intObj;

    private void Reset() {
      if (textGraphic == null) textGraphic = GetComponent<LeapTextGraphic>();
    }

    private void Update() {
      if (intObj != null && textGraphic != null) {
        textGraphic.text = intObj.primaryHoverDistance.ToString();
      }
    }

  }

}
