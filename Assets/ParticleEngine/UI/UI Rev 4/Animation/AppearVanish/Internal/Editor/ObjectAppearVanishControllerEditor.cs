using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Leap.Unity.Animation {
  
  [CustomEditor(typeof(ObjectAppearVanishController), editorForChildClasses: true)]
  public class ObjectAppearVanishControllerEditor : AppearVanishControllerEditorBase<ObjectAppearVanishController> {

    protected override void OnEnable() {
      base.OnEnable();
    }

    public override void OnInspectorGUI() {
      base.OnInspectorGUI();

      refreshAppearVanishControllers();

      drawAttachedAppearVanishControllers();
    }

    private void refreshAppearVanishControllers() {
      foreach (var target in targets) {
        target.RefreshAppearVanishControllers();
      }
    }

    private void drawAttachedAppearVanishControllers() {
      EditorGUILayout.Space();

      EditorGUILayout.LabelField("Object AppearVanish Controllers",
                                 EditorStyles.boldLabel);

      EditorGUILayout.BeginVertical();

      foreach (var appearVanishComponent in target.appearVanishControllers) {
        EditorGUILayout.LabelField(new GUIContent(appearVanishComponent.GetType().Name));
      }

      EditorGUILayout.EndVertical();
    }

  }

}
