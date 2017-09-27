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

    base.OnInspectorGUI();
  }

  protected void drawTransitionButtons() {
    EditorGUILayout.BeginHorizontal();
    
    if (GUILayout.Button(new GUIContent("Panel Mode",
      "Calls TransitionToPanelNow() on the selected widget controller(s)."))) {

      // Undo history
      Undo.IncrementCurrentGroup();
      var curGroupIdx = Undo.GetCurrentGroup();
      Undo.SetCurrentGroupName("Panel Mode");


      foreach (var target in targets) {
        Undo.RegisterFullObjectHierarchyUndo(target, "Panel Mode");
        target.TransitionToPanelNow();
      }

      Undo.CollapseUndoOperations(curGroupIdx);
      Undo.SetCurrentGroupName("Panel Mode");

    }
    if (GUILayout.Button(new GUIContent("Ball Mode",
      "Calls TransitionToBallNow() on the selected widget controller(s)."))) {

      Undo.IncrementCurrentGroup();
      var curGroupIdx = Undo.GetCurrentGroup();
      Undo.SetCurrentGroupName("Ball Mode");

      foreach (var target in targets) {
        Undo.RegisterFullObjectHierarchyUndo(target, "Ball Mode");
        target.TransitionToBallNow();
      }

      Undo.CollapseUndoOperations(curGroupIdx);
      Undo.SetCurrentGroupName("Ball Mode");

    }

    EditorGUILayout.EndHorizontal();
  }

}
