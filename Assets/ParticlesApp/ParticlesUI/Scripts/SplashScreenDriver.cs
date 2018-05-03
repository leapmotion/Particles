using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;

namespace Leap.Unity.Particles {

  public class SplashScreenDriver : MonoBehaviour {

    [Header("Splash Screen")]
    public Transform targetCamera;
    public string loadSceneWhenFinished;
    public float maintainDistance = 3f;
    public float lerpSpeed = 3f;

    [Header("Animation")]
    public float hiddenTime = 1f;
    public float fadeTime = 1f;
    public AnimationCurve fadeCurve = DefaultCurve.SigmoidUp;
    public float stayTime = 4f;

    [Header("Renderer Binding (drives material alpha channel)")]
    public Renderer boundRenderer;
    public string fadeColorProperty = "_Color";
    public int _fadeColorPropId = -1;

    [Header("(Optioanl) Vignette Fade Out Binding")]
    public PostProcessVolume postProcessVolume;

    private enum FadeState { Hidden, FadingIn, Staying, FadingOut }
    private FadeState _currState = FadeState.Hidden;

    private float _currT = 0f;
    private bool _doSceneLoad = false;
    private bool _sceneLoadRequested = false;
    private bool _animComplete = false;

    private void Start() {
      _fadeColorPropId = Shader.PropertyToID(fadeColorProperty);
    }

    private void Update() {
      updateTransitions();

      updateLerpFaceTarget();

      updateVisuals();

      _currT += Time.deltaTime;

      if (_doSceneLoad) {
        if (!_sceneLoadRequested) {
          SceneManager.LoadSceneAsync(loadSceneWhenFinished, LoadSceneMode.Single);
          _sceneLoadRequested = true;
        }
      }

      if (_animComplete) {
        _currT = 0f;
      }
    }

    private void updateTransitions() {
      if (_currT > hiddenTime && _currState == FadeState.Hidden) {
        _currState = FadeState.FadingIn;
        _currT = 0f;
      }

      if (_currT > fadeTime && _currState == FadeState.FadingIn) {
        _currState = FadeState.Staying;
        _currT = 0f;
      }

      if (_currT > stayTime && _currState == FadeState.Staying) {
        _currState = FadeState.FadingOut;
        _currT = 0f;
      }

      if (_currT > stayTime && _currState == FadeState.FadingOut) {
        _currState = FadeState.Hidden;
        _currT = 0f;

        _doSceneLoad = true;
        _animComplete = true;
      }
    }

    private void updateLerpFaceTarget() {
      if (_currState == FadeState.Hidden) {
        this.transform.position = getTargetPosition();
      }
      else {
        var targetPosition = getTargetPosition();

        this.transform.position = Vector3.Lerp(this.transform.position, targetPosition,
          lerpSpeed * Time.deltaTime);
      }

      this.transform.rotation = Utils.FaceTargetWithoutTwist(this.transform.position,
        targetCamera.position, flip180: true);
    }

    private Vector3 getTargetPosition() {
      return targetCamera.position + getGroundForward() * maintainDistance;
    }

    // Robust ground-aligned forward.
    private Vector3 getGroundForward() {
      var camPose = targetCamera.ToPose();
      var camRot = camPose.rotation;
      var camForward = camRot * Vector3.forward;
      var camForwardXZ = Vector3.ProjectOnPlane(camForward, Vector3.up);
      if (camForwardXZ.sqrMagnitude < 0.00001f) { // Special cases for looking up or down.
        var camUp = camRot * Vector3.up;

        if (Vector3.Dot(camForward, Vector3.up) > 0f) { // Looking almost perfectly up.
          return -Vector3.ProjectOnPlane(camUp, Vector3.up).normalized;
        }
        else { // Looking almost perfectly down.
          return Vector3.ProjectOnPlane(camUp, Vector3.up).normalized;
        }
      }
      else {
        return camForwardXZ.normalized;
      }
    }

    private void updateVisuals() {
      var alpha = 0f;
      switch (_currState) {
        case FadeState.Hidden:
          alpha = 0f;
          break;
        case FadeState.FadingIn:
          alpha = Mathf.Lerp(0f, 1f, fadeCurve.Evaluate(_currT / fadeTime));
          break;
        case FadeState.Staying:
          alpha = 1f;
          break;
        case FadeState.FadingOut:
          alpha = Mathf.Lerp(1f, 0f, fadeCurve.Evaluate(_currT / fadeTime));
          break;
      }

      if (postProcessVolume != null) {
        if (_animComplete || _currState == FadeState.FadingOut) {
          Vignette vignette;
          if (postProcessVolume.profile.TryGetSettings(out vignette)) {
            vignette.color.value = Color.Lerp(Color.black, Color.white, alpha);
          }
        }
      }

      boundRenderer.sharedMaterial.SetColor(_fadeColorPropId,
        boundRenderer.sharedMaterial.GetColor(_fadeColorPropId).WithAlpha(alpha));
    }

  }

}
