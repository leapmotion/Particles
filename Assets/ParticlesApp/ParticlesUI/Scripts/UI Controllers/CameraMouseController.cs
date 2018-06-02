using UnityEngine;
using Leap.Unity.Attributes;
using Leap.Unity.DevGui;

[RequireComponent(typeof(Camera))]
public class CameraMouseController : MonoBehaviour {

  public const KeyCode MOVE_CODE = KeyCode.Mouse0;
  public const KeyCode ROTATE_CODE = KeyCode.Mouse1;

  [SerializeField]
  private Transform _pivot;

  [SerializeField]
  private Transform _focusPoint;

  [SerializeField]
  private TextureSimulator _texSim;

  [DevCategory("Basic Controls")]
  [DevValue]
  [SerializeField]
  private bool _followVRPlayer = true;

  [Header("Movement Settings")]
  [MinValue(0)]
  [SerializeField]
  private float _rotationSpeed;

  [Range(0, 1)]
  [SerializeField]
  private float _rotationSmoothing;

  [MinValue(0)]
  [SerializeField]
  private float _moveFactor = 1;

  [MinValue(0)]
  [SerializeField]
  private float _keyboardMoveSpeed = 1f;

  [SerializeField]
  private float _speedFocusDistance = 0;

  [Range(0, 1)]
  [SerializeField]
  private float _moveSmoothing;

  [MinValue(0)]
  [SerializeField]
  private float _maxMoveDistance;

  [MinValue(0)]
  [SerializeField]
  private float _zoomSpeed;

  [Range(0, 1)]
  [SerializeField]
  private float _zoomSmoothing;

  [MinMax(0, 10)]
  [SerializeField]
  private Vector2 _distanceRange = new Vector2(0.1f, 5f);

  [Header("2D Mode"), DevCategory("Basic Controls")]
  [DevValue]
  [SerializeField]
  private bool _2dModeEnabled = false;

  [Range(0, 1)]
  [SerializeField]
  private float _2dPercent = 0;

  [MinValue(0)]
  [SerializeField]
  private float _2dTransitionTime = 1;

  [SerializeField]
  private AnimationCurve _2dTransitionCurve;

  [SerializeField]
  private AnimationCurve _2dStrengthCurve;

  private Camera _camera;
  private Vector2 _prevMousePos;

  private Vector3 _focalPointPosition;
  private Quaternion _pivotRotation;
  private float _cameraDistance;

  private void Awake() {
    _camera = GetComponent<Camera>();
    _focalPointPosition = _focusPoint.localPosition;
    _pivotRotation = _pivot.rotation;
    _cameraDistance = -transform.localPosition.z;
  }

  private void LateUpdate() {
    _camera.enabled = !_followVRPlayer;

    Vector2 mouseDelta = (Vector2)Input.mousePosition - _prevMousePos;
    _prevMousePos = Input.mousePosition;

    float cameraSamplePos = (_cameraDistance + _speedFocusDistance) * transform.lossyScale.x;
    Vector3 p0 = _camera.ScreenToWorldPoint(new Vector3(0, 0, cameraSamplePos));
    Vector3 p1 = _camera.ScreenToWorldPoint(new Vector3(_camera.pixelWidth, 0, cameraSamplePos));
    float pixelsToMeter = _camera.pixelWidth / Vector3.Distance(p0, p1) * transform.lossyScale.x;

    if (GUIUtility.hotControl == 0) {
      if (Input.GetKey(MOVE_CODE) && !Input.GetKeyDown(MOVE_CODE)) {
        _focalPointPosition -= (Vector3)mouseDelta * _moveFactor / pixelsToMeter;
      }

      if (Input.GetKey(ROTATE_CODE) && !Input.GetKeyDown(ROTATE_CODE) && !_2dModeEnabled) {
        _pivotRotation = _pivotRotation * Quaternion.AngleAxis(mouseDelta.x * _rotationSpeed, Vector3.up);
        _pivotRotation = _pivotRotation * Quaternion.AngleAxis(-mouseDelta.y * _rotationSpeed, Vector3.right);
      }
    }

    _focalPointPosition += (Input.GetAxis("Horizontal") * _keyboardMoveSpeed * Vector3.right +
                            Input.GetAxis("Vertical") * _keyboardMoveSpeed * Vector3.up) / pixelsToMeter;

    if (!Dev.hasMouseCursor) {
      float deltaAmount = Mathf.Pow(1 + _zoomSpeed, -Input.mouseScrollDelta.y);
      _cameraDistance = Mathf.Clamp(_cameraDistance * deltaAmount, _distanceRange.x, _distanceRange.y);
    }

    if (_focalPointPosition.magnitude > _maxMoveDistance) {
      _focalPointPosition = _focalPointPosition.normalized * _maxMoveDistance;
    }

    _2dPercent = Mathf.MoveTowards(_2dPercent, _2dModeEnabled ? 1 : 0, Time.deltaTime / _2dTransitionTime);
    _texSim.restrictionPlane = _pivot.forward;
    _texSim.restrictionPlaneStrength = _2dStrengthCurve.Evaluate(_2dPercent);

    _pivot.rotation = Quaternion.Slerp(transform.rotation, _pivotRotation, _rotationSmoothing);
    _focusPoint.localPosition = Vector3.Lerp(_focusPoint.localPosition, _focalPointPosition, _moveSmoothing);
    transform.localPosition = Vector3.Lerp(_camera.transform.localPosition, new Vector3(0, 0, -_cameraDistance), _zoomSmoothing);
  }

  private void OnPreRender() {
    Matrix4x4 perspectiveMat = Matrix4x4.Perspective(_camera.fieldOfView, _camera.aspect, _camera.nearClipPlane, _camera.farClipPlane);

    float orthoHeight = _cameraDistance * transform.lossyScale.x * Mathf.Tan(Mathf.Deg2Rad * _camera.fieldOfView / 2.0f);
    float orthoWidth = _camera.aspect * orthoHeight;
    Matrix4x4 orthographicMat = Matrix4x4.Ortho(-orthoWidth, orthoWidth, -orthoHeight, orthoHeight, 0, 1000);

    Matrix4x4 finalMatrix = lerpMat(perspectiveMat, orthographicMat, _2dTransitionCurve.Evaluate(_2dPercent));
    _camera.projectionMatrix = finalMatrix;
  }

  private static Matrix4x4 lerpMat(Matrix4x4 a, Matrix4x4 b, float t) {
    return new Matrix4x4(Vector4.Lerp(a.GetColumn(0), b.GetColumn(0), t),
                         Vector4.Lerp(a.GetColumn(1), b.GetColumn(1), t),
                         Vector4.Lerp(a.GetColumn(2), b.GetColumn(2), t),
                         Vector4.Lerp(a.GetColumn(3), b.GetColumn(3), t));
  }
}
