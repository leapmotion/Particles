using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;

namespace Leap.Unity.Particles {
  using Animation;

  public class SplashScreenDriver : MonoBehaviour {

    [Header("Splash Screen")]
    public Transform targetCamera;
    public int loadSceneWhenFinished;
    public float maintainDistance = 3f;
    public float lerpSpeed = 3f;

    [Header("Animation")]
    public float hiddenTime = 1f;
    public float fadeTime = 1f;
    public AnimationCurve fadeCurve = DefaultCurve.SigmoidUp;
    public float stayTime = 4f;

    [Header("Renderer Binding (drives material alpha channel)")]
    public SpriteRenderer splashRenderer;

    [Header("(Optioanl) Vignette Fade Out Binding")]
    public PostProcessVolume postProcessVolume;

    private void Start() {
      StartCoroutine(fadeCoroutine());
    }

    private void Update() {
      updateLerpFaceTarget();
    }

    IEnumerator fadeCoroutine() {
      Vignette vignette;
      postProcessVolume.profile.TryGetSettings(out vignette);

      //Start loading the next scene right away
      //but don't let it activate
      var sceneLoadAsync = SceneManager.LoadSceneAsync(loadSceneWhenFinished, LoadSceneMode.Single);
      sceneLoadAsync.allowSceneActivation = false;

      //Construct a fade tween that allows us to fade the vignette and the 
      //sprite at the same time
      var fadeTween = Tween.Persistent().
                            Value(0, 1, alpha => {
                              splashRenderer.color = splashRenderer.color.WithAlpha(alpha);
                              vignette.color.value = Color.Lerp(Color.black, Color.white, alpha);
                            }).
                            OverTime(fadeTime).
                            Smooth(fadeCurve);

      //Make sure everything starts faded out
      fadeTween.Invoke();

      //Wait for the hidden time first
      yield return new WaitForSeconds(hiddenTime);

      //Once we are finished waiting, move this transform directly to the 
      //target position so it doesn't start in the wrong location
      transform.position = getTargetPosition();

      //Then fade everything in
      yield return fadeTween.Play(Direction.Forward).
                             Yield();

      //Wait for the stay time
      yield return new WaitForSeconds(stayTime);

      //then fade everything back out
      yield return fadeTween.Play(Direction.Backward).
                             Yield();

      //Finally release the tween, and allow the scene to activate
      //it should be loaded by now, and so should be an instant load
      fadeTween.Release();
      sceneLoadAsync.allowSceneActivation = true;
    }

    private void updateLerpFaceTarget() {
      var targetPosition = getTargetPosition();

      this.transform.position = Vector3.Lerp(this.transform.position, targetPosition,
           lerpSpeed * Time.deltaTime);

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
        } else { // Looking almost perfectly down.
          return Vector3.ProjectOnPlane(camUp, Vector3.up).normalized;
        }
      } else {
        return camForwardXZ.normalized;
      }
    }
  }
}
