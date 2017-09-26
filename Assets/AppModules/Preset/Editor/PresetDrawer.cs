using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Leap.Unity {

  [CustomPropertyDrawer(typeof(PresetBase), useForChildren: true)]
  public class PresetDrawer : PropertyDrawer {

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
      position.height = EditorGUIUtility.singleLineHeight;

      if (property.serializedObject.targetObjects.Length > 1) {
        GUI.Label(position, "Cannot edit more that one preset at once.");
        return;
      }

      var presets = property.FindPropertyRelative("_presets");
      var names = property.FindPropertyRelative("_presetNames");
      var index = property.FindPropertyRelative("_currPresetIndex");

      index.intValue = Mathf.Clamp(index.intValue, 0, presets.arraySize - 1);

      var currentNameProp = names.GetArrayElementAtIndex(index.intValue);
      var currentPresetProp = presets.GetArrayElementAtIndex(index.intValue);

      Rect contextPos, textPos, buttonPos;
      position.SplitHorizontallyWithLeft(out contextPos, out textPos, EditorGUIUtility.labelWidth);
      textPos.SplitHorizontallyWithRight(out textPos, out buttonPos, EditorGUIUtility.singleLineHeight * 1.5f);

      currentNameProp.stringValue = EditorGUI.TextField(textPos, currentNameProp.stringValue);

      if (GUI.Button(buttonPos, "v")) {
        GenericMenu menu = new GenericMenu();

        for (int i = 0; i < names.arraySize; i++) {
          int toAssign = i;
          menu.AddItem(new GUIContent(names.GetArrayElementAtIndex(i).stringValue), i == index.intValue, () => {
            index.intValue = toAssign;
            property.serializedObject.ApplyModifiedProperties();
          });
        }

        menu.AddSeparator("");

        menu.AddItem(new GUIContent("New Preset"), on: false, func: () => {
          index.intValue = presets.arraySize;
          presets.InsertArrayElementAtIndex(presets.arraySize);
          names.InsertArrayElementAtIndex(names.arraySize);
          names.GetArrayElementAtIndex(names.arraySize - 1).stringValue = "My Preset";
          property.serializedObject.ApplyModifiedProperties();
        });

        menu.ShowAsContext();
      }

      if (Event.current.type == EventType.MouseUp && Event.current.button == 1) {
        Event.current.Use();

        GenericMenu menu = new GenericMenu();
        menu.AddItem(new GUIContent("Delete Preset"), on: false, func: () => {
          int currCount = presets.arraySize;
          int maxIt = 1000;
          while (presets.arraySize == currCount) {
            if (maxIt-- < 0) throw new InvalidOperationException("Unable to delete array element.");
            presets.DeleteArrayElementAtIndex(index.intValue);
          }
          while (names.arraySize == currCount) {
            if (maxIt-- < 0) throw new InvalidOperationException("Unable to delete array element.");
            names.DeleteArrayElementAtIndex(index.intValue);
          }
          names.serializedObject.ApplyModifiedProperties();
        });
        menu.ShowAsContext();
      }

      EditorGUI.PropertyField(position, currentPresetProp, new GUIContent(property.displayName), includeChildren: true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
      if (property.serializedObject.targetObjects.Length > 1) {
        return EditorGUIUtility.singleLineHeight;
      }

      var presets = property.FindPropertyRelative("_presets");
      var names = property.FindPropertyRelative("_presetNames");
      var index = property.FindPropertyRelative("_currPresetIndex");

      if (presets.arraySize != names.arraySize) {
        int minCount = Mathf.Min(presets.arraySize, names.arraySize);
        var largerProp = presets.arraySize > minCount ? presets : names;
        while (largerProp.arraySize > minCount) {
          largerProp.DeleteArrayElementAtIndex(largerProp.arraySize - 1);
        }
      }

      if (presets.arraySize == 0) {
        presets.InsertArrayElementAtIndex(0);
        names.InsertArrayElementAtIndex(0);
        names.GetArrayElementAtIndex(0).stringValue = "My Preset";
      }

      var currentPresetProp = presets.GetArrayElementAtIndex(index.intValue);
      return EditorGUI.GetPropertyHeight(currentPresetProp, includeChildren: true);
    }
  }
}
