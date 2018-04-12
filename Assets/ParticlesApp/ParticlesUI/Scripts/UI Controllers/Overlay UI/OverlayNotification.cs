using Leap.Unity.Attributes;
using UnityEngine;
using UnityEngine.UI;

namespace Leap.Unity.Particles {

  /// <summary>
  /// Simple fade-over-time driver for a UnityEngine.Text element; call Notify().
  /// </summary>
  public class OverlayNotification : MonoBehaviour {

    public Text notificationText;
    [MinValue(0.001f)]
    public float sustainDuration = 1.0f;
    [MinValue(0.001f)]
    public float fadeDuration = 1.75f;
    public AnimationCurve alphaOverFade = DefaultCurve.SigmoidDown;

    private float _curT = 0f;

    private void Reset() {
      if (notificationText == null) notificationText = GetComponent<Text>();
    }

    private void OnEnable() {
      _curT = 0f;
    }

    private void Update() {
      _curT += Time.deltaTime;

      var fadeAge = Mathf.Clamp01((_curT - sustainDuration) / fadeDuration);
      var alpha = alphaOverFade.Evaluate(fadeAge);
      notificationText.color = notificationText.color.WithAlpha(alpha);

      if (fadeAge >= 1f) {
        gameObject.SetActive(false);
      }
    }

    public void Notify() {
      gameObject.SetActive(true);
      _curT = 0f;
    }

  }

}
