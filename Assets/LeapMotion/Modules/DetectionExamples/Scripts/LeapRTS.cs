/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity {

  /// <summary>
  /// Use this component on a Game Object to allow it to be manipulated by a pinch gesture.  The component
  /// allows rotation, translation, and scale of the object (RTS).
  /// </summary>
  public class LeapRTS : MonoBehaviour {

    public enum RotationMethod {
      None,
      Single,
      Full
    }

    [SerializeField]
    public GrabSwitch _switchA;

    [SerializeField]
    public GrabSwitch _switchB;

    [SerializeField]
    private RotationMethod _oneHandedRotationMethod;

    [SerializeField]
    private RotationMethod _twoHandedRotationMethod;

    [SerializeField]
    private bool _allowScale = true;

    [SerializeField]
    public bool vroomVroom = true;

    [SerializeField]
    private bool _stateFull = true;

    [Header("GUI Options")]
    [SerializeField]
    private KeyCode _toggleGuiState = KeyCode.None;

    [SerializeField]
    private bool _showGUI = true;

    private Transform _anchor;
    private bool _wasAGrasped, _wasBGrasped;

    private float _defaultNearClip;

    void Start() {
      //      if (_pinchDetectorA == null || _pinchDetectorB == null) {
      //        Debug.LogWarning("Both Pinch Detectors of the LeapRTS component must be assigned. This component has been disabled.");
      //        enabled = false;
      //      }

      GameObject pinchControl = new GameObject("RTS Anchor");
      _anchor = pinchControl.transform;
      _anchor.transform.parent = transform.parent;
      transform.parent = _anchor;
    }

    void Update() {
      if (Input.GetKeyDown(_toggleGuiState)) {
        _showGUI = !_showGUI;
      }

      bool didUpdate = false;
      if (_switchA != null)
        didUpdate |= _switchA.grasped != _wasAGrasped;
      if (_switchB != null)
        didUpdate |= _switchB.grasped != _wasBGrasped;

      _wasAGrasped = _switchA.grasped;
      _wasBGrasped = _switchB.grasped;

      if (didUpdate) {
        _prevA = _switchA.Position;
        _prevB = _switchB.Position;
        _prevRotA = _switchA.Rotation;
        _prevRotB = _switchB.Rotation;
        transform.SetParent(null, true);
      }

      if (_switchA != null && _switchA.grasped &&
          _switchB != null && _switchB.grasped) {
        transformDoubleAnchor();
      } else if (_switchA != null && _switchA.grasped) {
        transformSingleAnchor(_switchA);
      } else if (_switchB != null && _switchB.grasped) {
        transformSingleAnchor(_switchB);
      }

      if (didUpdate) {
        transform.SetParent(_anchor, true);
      }
    }

    void OnGUI() {
      if (_showGUI) {
        GUILayout.Label("One Handed Settings");
        doRotationMethodGUI(ref _oneHandedRotationMethod);
        GUILayout.Label("Two Handed Settings");
        doRotationMethodGUI(ref _twoHandedRotationMethod);
        _allowScale = GUILayout.Toggle(_allowScale, "Allow Two Handed Scale");
      }
    }

    private void doRotationMethodGUI(ref RotationMethod rotationMethod) {
      GUILayout.BeginHorizontal();

      GUI.color = rotationMethod == RotationMethod.None ? Color.green : Color.white;
      if (GUILayout.Button("No Rotation")) {
        rotationMethod = RotationMethod.None;
      }

      GUI.color = rotationMethod == RotationMethod.Single ? Color.green : Color.white;
      if (GUILayout.Button("Single Axis")) {
        rotationMethod = RotationMethod.Single;
      }

      GUI.color = rotationMethod == RotationMethod.Full ? Color.green : Color.white;
      if (GUILayout.Button("Full Rotation")) {
        rotationMethod = RotationMethod.Full;
      }

      GUI.color = Color.white;

      GUILayout.EndHorizontal();
    }

    private Quaternion _prevRotA, _prevRotB;
    private Vector3 _prevA, _prevB;
    private void transformDoubleAnchor() {
      _anchor.position = (_switchA.Position + _switchB.Position) / 2.0f;

      switch (_twoHandedRotationMethod) {
        case RotationMethod.None:
          break;
        case RotationMethod.Single:
          Vector3 p = _switchA.Position;
          p.y = _anchor.position.y;
          _anchor.LookAt(p);
          break;
        case RotationMethod.Full:
          if (_stateFull) {
            if (vroomVroom) {
              Vector3 axis = _switchA.Position - _switchB.Position;
              Vector3 perp = Utils.Perpendicular(axis);

              Vector3 deltaA = _switchA.Rotation * Quaternion.Inverse(_prevRotA) * perp;
              float deltaAngleA = Vector3.SignedAngle(perp, deltaA, axis);

              Vector3 deltaB = _switchB.Rotation * Quaternion.Inverse(_prevRotB) * perp;
              float deltaAngleB = Vector3.SignedAngle(perp, deltaB, axis);

              float totalDeltaAngle = (deltaAngleA + deltaAngleB) * 0.5f;

              var delta = Quaternion.FromToRotation(_prevB - _prevA, _switchB.Position - _switchA.Position);
              _anchor.rotation = Quaternion.AngleAxis(totalDeltaAngle, axis) * delta * _anchor.rotation;
            } else {
              var delta = Quaternion.FromToRotation(_prevB - _prevA, _switchB.Position - _switchA.Position);
              _anchor.rotation = delta * _anchor.rotation;
            }
          } else {
            //Quaternion pp = Quaternion.Lerp(_switchA.Rotation, _switchB.Rotation, 0.5f);
            //Vector3 u = pp * Vector3.up;
            //_anchor.LookAt(_switchA.Position, u);
          }
          break;
      }

      _prevA = _switchA.Position;
      _prevB = _switchB.Position;
      _prevRotA = _switchA.Rotation;
      _prevRotB = _switchB.Rotation;

      if (_allowScale) {
        _anchor.localScale = Vector3.one * Vector3.Distance(_switchA.Position, _switchB.Position);
      }
    }

    private void transformSingleAnchor(GrabSwitch singlePinch) {
      _anchor.position = singlePinch.Position;

      switch (_oneHandedRotationMethod) {
        case RotationMethod.None:
          break;
        case RotationMethod.Single:
          Vector3 p = singlePinch.Rotation * Vector3.right;
          p.y = _anchor.position.y;
          _anchor.LookAt(p);
          break;
        case RotationMethod.Full:
          _anchor.rotation = singlePinch.Rotation;
          break;
      }

      _anchor.localScale = Vector3.one;
    }
  }
}
