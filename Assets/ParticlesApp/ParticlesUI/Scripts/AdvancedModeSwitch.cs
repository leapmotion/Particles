using Leap.Unity.Animation;
using Leap.Unity.Attributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Particles {

  public enum MenuMode { Simple, Advanced };

  public class AdvancedModeSwitch : MonoBehaviour, IPropertySwitch {

    [EditTimeOnly]
    public MenuMode startingMode = MenuMode.Simple;

    [MinValue(0.001f)]
    public float openWidgetsTime = 0.2f;
    public Transform[] panelWidgetTransforms;
    public PanelStateController[] panelStateControllers;

    private bool _targetOn = false;
    private float _onAmount = 0f;

    private void Start() {
      if (startingMode == MenuMode.Simple) {
        OffNow();
      }
      else {
        OnNow();
      }
    }

    private void Update() {
      var targetAmount = _targetOn ? 1f : 0f;
      if (_onAmount != targetAmount) {
        var speed = 1 / openWidgetsTime * (_targetOn ? 1f : -1f);
        _onAmount = Mathf.Clamp01(_onAmount + speed * Time.deltaTime);
      }

      updateTransition(_onAmount, isActivating: _targetOn);
    }


    private void updateTransition(float activationAmount, bool isActivating) {
      if (panelStateControllers.Length != 0) {
        var perPanelFraction = (1f / panelStateControllers.Length);

        // Disable each panel in the linked panel state controllers.
        for (int i = 0; i < panelStateControllers.Length; i++) {
          var panelController = panelStateControllers[i];
          var openTime = perPanelFraction * i;
          if (activationAmount > openTime) {
            // Panel can be open, don't override it closed.
            panelController.overrideClosePanel = false;
          }
          else if (activationAmount == openTime) {
            // Panel should only be open if directionality is towards opening.
            panelController.overrideClosePanel = isActivating;
          }
          else {
            // Panel should be closed.
            panelController.overrideClosePanel = true;
          }
        }
      }

      // Shrink and disable each panel widget transform.
      var minScaleAmount = 0.0001f;
      if (panelWidgetTransforms.Length > 0) {
        var perTransformFraction = 1f / panelWidgetTransforms.Length;

        for (int i = 0; i < panelWidgetTransforms.Length; i++) {
          var widgetTransform = panelWidgetTransforms[i];
          var minTime = perTransformFraction * i;
          var maxTime = perTransformFraction * (i + 1);
          var scaleAmount = activationAmount.Map(minTime, maxTime, 0f, 1f);

          if (scaleAmount < minScaleAmount) {
            widgetTransform.gameObject.SetActive(false);
          }
          else {
            widgetTransform.gameObject.SetActive(true);
          }
          scaleAmount = Mathf.Max(minScaleAmount, scaleAmount);
          widgetTransform.localScale = widgetTransform.localScale.WithY(scaleAmount);
        }
      }
    }

    #region IPropertySwitch

    public bool GetIsOffOrTurningOff() {
      return !_targetOn;
    }

    public bool GetIsOnOrTurningOn() {
      return _targetOn;
    }

    public void Off() {
      _targetOn = false;
    }

    public void OffNow() {
      _targetOn = false;
      _onAmount = 0f;
      updateTransition(0f, isActivating: false);
    }

    public void On() {
      _targetOn = true;
    }

    public void OnNow() {
      _targetOn = true;
      _onAmount = 1f;
      updateTransition(1f, isActivating: true);
    }

    #endregion

  }

}
