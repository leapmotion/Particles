using Leap.Unity;
using Leap.Unity.Query;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Leap.Unity.Animation {
  
  public abstract class AppearVanishControllerEditorBase<T> : CustomEditorBase<T>
                                                              where T : UnityEngine.Object,
                                                                        IAppearVanishController {

    protected override void OnEnable() {
      base.OnEnable();
    }

    public override void OnInspectorGUI() {
      drawAppearVanishButtons();

      base.OnInspectorGUI();
    }

    protected void drawAppearVanishButtons() {
      EditorGUILayout.BeginHorizontal();

      if (GUILayout.Button(new GUIContent("Appear Now",
        "Calls AppearNow() on the selected appear/vanish controller(s)."))) {
        
        // Undo history
        Undo.IncrementCurrentGroup();
        var curGroupIdx = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Appear Object(s) Now");

        foreach (var target in targets) {
          target.AppearNow();
        }

        Undo.CollapseUndoOperations(curGroupIdx);
        Undo.SetCurrentGroupName("Appear Object(s) Now");

      }
      if (GUILayout.Button(new GUIContent("Vanish Now",
        "Calls VanishNow() on the selected appear/vanish controller."))) {

        // Undo history
        Undo.IncrementCurrentGroup();
        var curGroupIdx = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Vanish Object(s) Now");

        foreach (var target in targets) {
          target.VanishNow();
        }

        Undo.CollapseUndoOperations(curGroupIdx);
        Undo.SetCurrentGroupName("Vanish Object(s) Now");

      }

      EditorGUILayout.EndHorizontal();
    }

  }

}
