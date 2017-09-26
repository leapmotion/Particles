using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Leap.Unity.Animation {

  [CanEditMultipleObjects]
  [CustomEditor(typeof(TweenAppearVanishController), editorForChildClasses: true)]
  public class TweenAppearVanishControllerEditor : AppearVanishControllerEditorBase<TweenAppearVanishController> {

    protected override void OnEnable() {
      base.OnEnable();
    }

    public override void OnInspectorGUI() {
      base.OnInspectorGUI();
    }

  }

}
