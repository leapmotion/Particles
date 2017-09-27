using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Leap.Unity.Animation {
  
  [CustomEditor(typeof(ScaleAppearVanishController), editorForChildClasses: true)]
  public class ScaleAppearVanishControllerEditor : AppearVanishControllerEditorBase<ScaleAppearVanishController> {

    protected override void OnEnable() {
      base.OnEnable();

      specifyConditionalDrawing("nonUniformScale",
                                "xScaleCurve",
                                "yScaleCurve",
                                "zScaleCurve");
    }

    public override void OnInspectorGUI() {
      base.OnInspectorGUI();
    }

  }

}
