using Leap.Unity.GraphicalRenderer;
using Leap.Unity.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WidgetController : MonoBehaviour {

  public InteractionBehaviour widget;
  public Transform widgetVisualPivot;
  public Transform panelPivot;
  public LeapGraphicRenderer graphicRenderer;

  private Vector3    _panelWidgetDeltaPos;
  private Quaternion _panelWidgetDeltaRot;

  private Vector3    _rendererPanelDeltaPos;
  private Quaternion _rendererPanelDeltaRot;

  void Start() {
    _panelWidgetDeltaPos = panelPivot.position - widget.rigidbody.position;
    _panelWidgetDeltaRot = panelPivot.rotation * Quaternion.Inverse(widget.rigidbody.rotation);

    _rendererPanelDeltaPos = graphicRenderer.transform.position - panelPivot.position;
    _rendererPanelDeltaRot = graphicRenderer.transform.rotation * Quaternion.Inverse(panelPivot.rotation);

    widget.OnGraspedMovement += onGraspedMovement;
  }

  private void onGraspedMovement(Vector3 oldPos, Quaternion oldRot,
                                 Vector3 newPos, Quaternion newRot,
                                 List<InteractionController> graspingControllers) {
    Quaternion faceUserRot = Quaternion.LookRotation((newPos + Vector3.up * 0.35F /* hacky heuristic */) - Camera.main.transform.position);

    Vector3    alignedPanelPos = faceUserRot * _panelWidgetDeltaPos + newPos;
    Quaternion alignedPanelRot = faceUserRot * _panelWidgetDeltaRot;

    Vector3    rendererPos = alignedPanelRot * _rendererPanelDeltaPos + alignedPanelPos;
    Quaternion rendererRot = alignedPanelRot * _rendererPanelDeltaRot;

    widget.rigidbody.position = newPos;
    widget.transform.position = newPos;
    widget.rigidbody.rotation = faceUserRot;
    widget.transform.rotation = faceUserRot;

    graphicRenderer.transform.position = rendererPos;
    graphicRenderer.transform.rotation = rendererRot;

    widgetVisualPivot.position = newPos;
    widgetVisualPivot.rotation = newRot;

    panelPivot.position = alignedPanelPos;
    panelPivot.rotation = alignedPanelRot;
  }

}
