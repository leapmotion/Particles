using Leap.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Leap.Unity.Animation {

  [CustomEditor(typeof(AnimationStateObserver))]
  public class AnimationStateObserverEditor : CustomEditorBase<AnimationStateObserver> {

    protected override void OnEnable() {
      base.OnEnable();
    }

    public override void OnInspectorGUI() {
      if (target.animator != null) {

        AnimatorController controller = (target.animator.runtimeAnimatorController as AnimatorController);
        if (controller == null) {
          EditorGUILayout.HelpBox("The attached Animator doesn't have an AnimatorController.", MessageType.Error);
        }
        else {
          bool stateIsValid = false;
          try {
            var layer = controller.layers[target.stateLayer];
            var states = layer.stateMachine.states;
            foreach (var state in states) {
              if (state.state.name.Equals(target.stateName)) {
                stateIsValid = true;
                break;
              }
            }

            if (!stateIsValid) {
              string availableStates = "";
              foreach (var state in states) {
                availableStates += "\n - " + state.state.name;
              }
              EditorGUILayout.HelpBox("The provided state name doesn't exist. Available states:" + availableStates, MessageType.Error);
            }
          }
          catch (System.IndexOutOfRangeException) {
            EditorGUILayout.HelpBox("That layer index doesn't exist.", MessageType.Error);
          }
        }

      }

      base.OnInspectorGUI();
    }

  }

}
