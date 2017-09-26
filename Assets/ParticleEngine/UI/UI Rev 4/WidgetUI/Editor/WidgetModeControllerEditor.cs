using Leap.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CanEditMultipleObjects]
[CustomEditor(typeof(WidgetModeController), editorForChildClasses: true)]
public class WidgetModeControllerEditor : CustomEditorBase<WidgetModeController> {

  protected override void OnEnable() {
    base.OnEnable();
  }

  public override void OnInspectorGUI() {
    drawTransitionButtons();
    drawMatchBallPoseButton();

    base.OnInspectorGUI();
  }

  protected void drawTransitionButtons() {
    EditorGUILayout.BeginHorizontal();
    
    if (GUILayout.Button(new GUIContent("Panel Mode",
      "Calls TransitionToPanelNow() on the selected widget controller(s)."))) {
      foreach (var target in targets) {
        target.TransitionToPanelNow();
      }
    }
    if (GUILayout.Button(new GUIContent("Ball Mode",
      "Calls TransitionToBallNow() on the selected widget controller(s)."))) {
      foreach (var target in targets) {
        target.TransitionToBallNow();
      }
    }

    EditorGUILayout.EndHorizontal();
  }

  protected void drawMatchBallPoseButton() {
    EditorGUILayout.BeginHorizontal();

    if (GUILayout.Button(new GUIContent("Match Ball Pose",
      "Calls MoveSelfToBall() on the selected widget controller(s)."))) {
      foreach (var target in targets) {
        target.MoveSelfToBall();
      }
    }

    EditorGUILayout.EndHorizontal();
  }

}
