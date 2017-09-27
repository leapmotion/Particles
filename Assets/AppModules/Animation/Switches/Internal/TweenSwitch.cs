using Leap.Unity.Animation;
using Leap.Unity.Attributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity.Animation {

  /// <summary>
  /// An easy-to-implement IPropertySwitch implementation backed by a single Tween.
  /// </summary>
  public abstract class TweenSwitch : MonoBehaviour, IPropertySwitch {

    #region Inspector

    [Header("Tween Control")]

    [MinValue(0f), OnEditorChange("refreshTween")]
    public float tweenTime = 1f;

    #endregion

    #region Unity Events

    // TODO: When this has been bug/battle-tested, add HideInInspector
    [SerializeField]
    private bool _startOn = false;

    protected virtual void Start() {
      if (_backingSwitchTween.isValid) {
        // Something other call initialized the tween, don't initialize here.
        return;
      }

      if (_startOn) {
        OnNow();
      }
      else {
        OffNow();
      }
    }

    protected virtual void OnDestroy() {
      if (_backingSwitchTween.isValid) {
        _backingSwitchTween.Release();
      }
    }

    #endregion

    #region Tween

    /// <summary>
    /// Check if this tween is valid to know if anything has forced the tween to be
    /// constructed by accessing the lazy _switchTween property.
    /// </summary>
    private Tween _backingSwitchTween;

    /// <summary>
    /// 0 for off, 1 for on.
    /// </summary>
    private Tween _switchTween {
      get {
        if (!_backingSwitchTween.isValid) {
          refreshTween();
        }

        return _backingSwitchTween;
      }
    }

    protected void refreshTween() {
      if (!_backingSwitchTween.isValid) {
        _backingSwitchTween = Tween.Persistent().Value(0f, 1f, onTweenValue);
      }

      _backingSwitchTween.OverTime(tweenTime);
    }

    private void onTweenValue(float value) {
      updateSwitch(value);
    }

    #endregion

    #region Abstraction
    /// <summary>
    /// Sets the switch state somewhere between 0 (off) and 1 (on). The "immediately"
    /// argument is a hint to know OnNow() or OffNow() was called, in which case any
    /// delays should be ignored (important when TweenSwitches are chained).
    /// </summary>
    protected abstract void updateSwitch(float time, bool immediately = false);

    #endregion

    #region IPropertySwitch

    public void On() {
      _switchTween.Play(Direction.Forward);
    }

    public bool GetIsOnOrTurningOn() {
      return _switchTween.direction == Direction.Forward;
    }

    public void Off() {
      _switchTween.Play(Direction.Backward);
    }

    public bool GetIsOffOrTurningOff() {
      return _switchTween.direction == Direction.Backward;
    }

    public void OnNow() {
#if UNITY_EDITOR
      if (!Application.isPlaying) {
        Undo.RegisterFullObjectHierarchyUndo(gameObject, "Appear Object(s) Now");
        _startOn = true;
      }
#endif

      if (Application.isPlaying) {
        if (!gameObject.activeSelf) {
          gameObject.SetActive(true);
        }

        var tween = _switchTween;
        tween.progress = 1f;
      }

      updateSwitch(1f, immediately: true);
    }

    public void OffNow() {
#if UNITY_EDITOR
      if (!Application.isPlaying) {
        Undo.RegisterFullObjectHierarchyUndo(gameObject, "Vanish Object(s) Now");
        _startOn = false;
      }
#endif

      if (Application.isPlaying) {
        var tween = _switchTween;
        tween.progress = 0f;
      }

      updateSwitch(0f, immediately: true);
    }

    #endregion

  }

}
