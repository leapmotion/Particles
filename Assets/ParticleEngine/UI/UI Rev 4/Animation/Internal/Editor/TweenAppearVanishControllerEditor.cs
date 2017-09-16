using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Leap.Unity.Animation {

  [CanEditMultipleObjects]
  [CustomEditor(typeof(TweenAppearVanishController), editorForChildClasses: true)]
  public class TweenAppearVanishControllerEditor : CustomEditorBase<TweenAppearVanishController> {

    protected override void OnEnable() {
      base.OnEnable();
    }

    public override void OnInspectorGUI() {
      drawAppearVanishButtons();

      base.OnInspectorGUI();
    }

    private void drawAppearVanishButtons() {
      EditorGUILayout.BeginHorizontal();

      if (GUILayout.Button(new GUIContent("Appear Now",
        "Calls AppearNow() on the selected appear/vanish controller(s)."))) {
        foreach (var target in targets) {
          target.AppearNow();
        }
      }
      if (GUILayout.Button(new GUIContent("Vanish Now",
        "Calls VanishNow() on the selected appear/vanish controller."))) {
        foreach (var target in targets) {
          target.VanishNow();
        }
      }

      EditorGUILayout.EndHorizontal();
    }

  }

}
