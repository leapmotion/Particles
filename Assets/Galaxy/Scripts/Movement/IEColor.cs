using UnityEngine;
using Leap.Unity;
using Leap.Unity.Interaction;

public class IEColor : MonoBehaviour {

  public Color baseColor;
  public Color hoverColor;
  public Color primaryHoverColor;
  public Color graspColor;
  public Renderer _renderer;

  private InteractionBehaviour _behaviour;

  private void Start() {
    _behaviour = GetComponent<InteractionBehaviour>();
  }

  void LateUpdate() {
    if (_behaviour.isGrasped) {
      _renderer.material.color = graspColor;
    } else if (_behaviour.isPrimaryHovered) {
      _renderer.material.color = primaryHoverColor;
    } else if (_behaviour.isHovered) {
      _renderer.material.color = hoverColor;
    } else {
      _renderer.material.color = baseColor;
    }
  }
}
