using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;
using Leap.Unity.RuntimeGizmos;

public class HaloScript : MonoBehaviour {

  public GrabSwitch switchA;
  public GrabSwitch switchB;

  public Transform haloAnchor;
  public Transform graphicAnchor;
  public Vector3 offset;
  public Renderer[] renderers;
  public Color regularColor;
  public Color translateColor;
  public Color rotateColor;
  public Color scaleColor;
  public Mode mode;

  [Header("Grab")]
  public float startTranslate = 0.5f;
  public float endTranslate = 0.3f;

  [Header("Scale")]
  public float startPinch = 0.5f;
  public float endPinch = 0.3f;

  private Vector3 _startPosition;
  private float _startRadius;

  private RuntimeGizmoDrawer drawer;

  public enum Mode {
    None,
    Translate,
    Rotate,
    Scale
  }

  private Vector3 getLocalToHalo(Vector3 pos) {
    return graphicAnchor.InverseTransformPoint(pos);
  }

  private void Update() {
    RuntimeGizmoManager.TryGetGizmoDrawer(out drawer);


    switch (mode) {
      case Mode.None:
        none();
        break;
      case Mode.Translate:
        translate();
        break;
      case Mode.Rotate:
        rotate();
        break;
      case Mode.Scale:
        scale();
        break;
    }
  }

  private void none() {
    if (Hands.Left == null) {
      return;
    }

    switchA.grasped = false;
    switchB.grasped = false;

    setColor(regularColor);
    updateHaloPosition();

    //Reset scale here
    haloAnchor.transform.localScale = Vector3.one;
    graphicAnchor.transform.localRotation = Quaternion.identity;

    if (Hands.Left.GrabAngle > 0.5f) {
      mode = Mode.Translate;
      _startPosition = Hands.Left.PalmPosition.ToVector3();
      return;
    }

    if (Hands.Right == null) {
      return;
    }

    if (Hands.Right.PinchStrength > startPinch && graphicAnchor.InverseTransformPoint(Hands.Right.GetPinchPosition()).magnitude < 1.1f) {
      mode = Mode.Scale;
      _startRadius = haloAnchor.InverseTransformPoint(Hands.Right.GetPinchPosition()).magnitude;
      return;
    }

    Vector3 localIndex = graphicAnchor.InverseTransformPoint(Hands.Right.GetIndex().TipPosition.ToVector3());
    if (localIndex.magnitude < 1.1f && localIndex.z < 0) {
      mode = Mode.Rotate;
      _startPosition = localIndex;
      return;
    }
  }

  private void translate() {
    if (Hands.Left == null) {
      mode = Mode.None;
      return;
    }

    setColor(translateColor);
    updateHaloPosition();

    switchA.grasped = true;
    switchA.Position = Hands.Left.PalmPosition.ToVector3();

    if (Hands.Left.GrabAngle < endTranslate) {
      mode = Mode.None;
      return;
    }
  }

  private void rotate() {
    setColor(rotateColor);

    if (Hands.Left == null || Hands.Right == null) {
      mode = Mode.None;
      return;
    }

    Vector3 startDelta = _startPosition;
    Vector3 nowDelta = haloAnchor.InverseTransformPoint(Hands.Right.GetIndex().TipPosition.ToVector3());
    float angle = Vector3.SignedAngle(startDelta, nowDelta, Vector3.forward);

    //drawer.color = Color.red;
    //drawer.DrawLine(haloAnchor.position, haloAnchor.TransformPoint(startDelta));
    //drawer.color = Color.green;
    //drawer.DrawLine(haloAnchor.position, haloAnchor.TransformPoint(nowDelta));
    //drawer.color = Color.blue;
    //drawer.DrawLine(haloAnchor.position, haloAnchor.TransformPoint(Vector3.forward));

    switchA.grasped = true;
    switchA.Position = Hands.Left.PalmPosition.ToVector3();
    switchA.Rotation = Quaternion.AngleAxis(angle, haloAnchor.forward);

    graphicAnchor.localEulerAngles = new Vector3(0, 0, angle);

    if (haloAnchor.InverseTransformPoint(Hands.Right.GetIndex().TipPosition.ToVector3()).z > 0) {
      mode = Mode.None;
      return;
    }
  }

  private void scale() {
    setColor(scaleColor);

    if (Hands.Left == null || Hands.Right == null) {
      mode = Mode.None;
      return;
    }

    float nowRadius = haloAnchor.InverseTransformPoint(Hands.Right.GetPinchPosition()).magnitude;
    float delta = nowRadius / _startRadius;

    haloAnchor.localScale = Vector3.one * delta;

    switchA.grasped = true;
    switchA.Position = Hands.Left.PalmPosition.ToVector3() + Vector3.one * delta;
    switchB.grasped = true;
    switchB.Position = Hands.Left.PalmPosition.ToVector3() - Vector3.one * delta;

    if (Hands.Right.PinchStrength < endPinch) {
      mode = Mode.None;
      return;
    }
  }

  private void setColor(Color color) {
    foreach (var renderer in renderers) {
      renderer.material.color = color;
    }
  }

  private void updateHaloPosition() {
    haloAnchor.position = Hands.Left.PalmPosition.ToVector3() + Hands.Left.Rotation.ToQuaternion() * offset;
    haloAnchor.rotation = Hands.Left.Rotation.ToQuaternion() * Quaternion.Euler(90, 0, 0);
  }

}
