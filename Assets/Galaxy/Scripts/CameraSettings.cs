using UnityEngine;
using Leap.Unity.DevGui;

public class CameraSettings : MonoBehaviour {

  [DevCategory("General Settings")]
  [Range(0.001f, 0.4f)]
  [DevValue]
  public float nearClipPlane = 0.2f;

  private Camera _camera;

  private void Start() {
    _camera = GetComponent<Camera>();
  }

  private void Update() {
    _camera.nearClipPlane = nearClipPlane;
  }
}
