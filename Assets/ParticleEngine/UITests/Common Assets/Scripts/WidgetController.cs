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

  private Pose _widgetPose;
  private Pose _rendererPose;
  private Pose _panelPose;
  private Pose _widgetVisualPose;

  private bool _aligningWidgetVisual = false;
  private Pose _widgetVisualTargetPose;

  void Start() {
    _panelWidgetDeltaPos = panelPivot.position - widget.rigidbody.position;
    _panelWidgetDeltaRot = panelPivot.rotation * Quaternion.Inverse(widget.rigidbody.rotation);

    _rendererPanelDeltaPos = graphicRenderer.transform.position - panelPivot.position;
    _rendererPanelDeltaRot = graphicRenderer.transform.rotation * Quaternion.Inverse(panelPivot.rotation);

    widget.OnGraspedMovement += onGraspedMovement;
    widget.OnGraspEnd += onGraspEnd;

    onGraspedMovement(this.transform.position, this.transform.rotation, this.transform.position, this.transform.rotation, null);
    onGraspEnd();
  }

  private void onGraspedMovement(Vector3 oldPos, Quaternion oldRot,
                                 Vector3 newPos, Quaternion newRot,
                                 List<InteractionController> graspingControllers) {
    Quaternion faceUserRot = getFaceUserRot(newPos);

    _widgetPose = new Pose(newPos, faceUserRot);
    _panelPose = new Pose(faceUserRot * _panelWidgetDeltaPos + newPos,
                          faceUserRot * _panelWidgetDeltaRot);
    _rendererPose = new Pose(_panelPose.rot * _rendererPanelDeltaPos + _panelPose.pos,
                             _panelPose.rot * _rendererPanelDeltaRot);
    _widgetVisualPose = new Pose(newPos, newRot);

    doAlignment();
  }

  private Quaternion getFaceUserRot(Vector3 position) {
    return Quaternion.LookRotation((position + Vector3.up * 0.35F /* hacky heuristic */) - Camera.main.transform.position);
  }

  private struct Pose {
    public Vector3 pos;
    public Quaternion rot;

    public Vector3 position { get { return pos; } set { pos = value; } }
    public Quaternion rotation { get { return rot; } set { rot = value; } }

    public Pose(Vector3 pos, Quaternion rot) { this.pos = pos; this.rot = rot; }

    public static implicit operator Pose(Transform t) {
      return new Pose(t.position, t.rotation);
    }
  }

  private void doAlignment() {
    widget.rigidbody.position = _widgetPose.pos;
    widget.transform.position = _widgetPose.pos;
    widget.rigidbody.rotation = _widgetPose.rot;
    widget.transform.rotation = _widgetPose.rot;

    graphicRenderer.transform.position = _rendererPose.pos;
    graphicRenderer.transform.rotation = _rendererPose.rot;

    panelPivot.position = _panelPose.pos;
    panelPivot.rotation = _panelPose.rot;

    widgetVisualPivot.position = _widgetVisualPose.pos;
    widgetVisualPivot.rotation = _widgetVisualPose.rot;
  }

  private void onGraspEnd() {
    _aligningWidgetVisual = true;
  }

  private void FixedUpdate() {
    if (_aligningWidgetVisual) {
      Quaternion targetRot = getFaceUserRot(_widgetVisualPose.position);

      _widgetVisualPose.rot = Quaternion.Slerp(_widgetVisualPose.rot, targetRot, 10F * Time.deltaTime);
      doAlignment();

      if (Quaternion.Angle(targetRot, _widgetVisualPose.rot) < 0.5F) {
        _aligningWidgetVisual = false;
      }
    }
  }

}
