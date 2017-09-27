using Leap.Unity.Animation;
using Leap.Unity.Attributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity.Animation {

  public abstract class TweenAppearVanishController : MonoBehaviour, IAppearVanishController {

    #region Inspector

    [Header("Tween Control")]

    [MinValue(0f), OnEditorChange("refreshTween")]
    public float animTime = 1f;

    #endregion

    #region Unity Events
    
    [SerializeField]
    private bool _startAppeared;

    protected virtual void Start() {
      if (_backingAppearVanishTween.isValid) {
        // Something other call initialized the tween, don't initialize here.
        return;
      }

      if (_startAppeared) {
        AppearNow();
      }
      else {
        VanishNow();
      }
    }

    protected virtual void OnDestroy() {
      if (_backingAppearVanishTween.isValid) {
        _backingAppearVanishTween.Release();
      }
    }

    #endregion

    #region Tween

    private Tween _backingAppearVanishTween;
    /// <summary>
    /// 0 for vanished, 1 for appeared.
    /// </summary>
    private Tween _appearVanishTween {
      get {
        if (!_backingAppearVanishTween.isValid) {
          refreshTween();
        }

        return _backingAppearVanishTween;
      }
    }

    protected void refreshTween() {
      if (!_backingAppearVanishTween.isValid) {
        _backingAppearVanishTween = Tween.Persistent().Value(0f, 1f, onTweenValue);
      }

      _backingAppearVanishTween.OverTime(animTime);
    }

    private void onTweenValue(float value) {
      updateAppearVanish(value);
    }

    #endregion

    /// <summary>
    /// The implementor should update the appearance or vanishing state of the object
    /// based on the given time between 0 (fully vanished) and 1 (fully appeared).
    /// 
    /// If immediately is set to true, the appearance or vanishing should occur instantly.
    /// </summary>
    protected abstract void updateAppearVanish(float time, bool immediately = false);

    public void SetTweenProgress(float progress) {
      var tween = _appearVanishTween;
      tween.progress = progress;
    }

    #region IAppearVanishController

    public bool GetVisible() {
      return _appearVanishTween.progress > 0f;
    }

    public void Appear() {
      _appearVanishTween.Play(Direction.Forward);
    }

    public bool GetAppearingOrAppeared() {
      return _appearVanishTween.direction == Direction.Forward;
    }

    public void Vanish() {
      _appearVanishTween.Play(Direction.Backward);
    }

    public bool GetVanishingOrVanished() {
      return _appearVanishTween.direction == Direction.Backward;
    }

    public void AppearNow() {
      if (Application.isPlaying) {
        if (!gameObject.activeSelf) {
          gameObject.SetActive(true);
        }

        var tween = _appearVanishTween;
        tween.progress = 1f;
      }

#if UNITY_EDITOR
      if (!Application.isPlaying) {
        Undo.RegisterFullObjectHierarchyUndo(gameObject, "Appear Object(s) Now");
        _startAppeared = true;
      }
#endif

      updateAppearVanish(1f, immediately: true);
    }

    public void VanishNow() {
      if (Application.isPlaying) {
        var tween = _appearVanishTween;
        tween.progress = 0f;
      }

#if UNITY_EDITOR
      if (!Application.isPlaying) {
        Undo.RegisterFullObjectHierarchyUndo(gameObject, "Vanish Object(s) Now");
        _startAppeared = false;
      }
#endif

      updateAppearVanish(0f, immediately: true);
    }

    #endregion

  }

}
