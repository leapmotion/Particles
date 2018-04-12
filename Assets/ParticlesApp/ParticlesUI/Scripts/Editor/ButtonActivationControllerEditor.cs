using Leap.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ButtonActivationController))]
public class ButtonActivationControllerEditor : CustomEditorBase<ButtonActivationController> {

  protected override void OnEnable() {
    base.OnEnable();
  }

  public override void OnInspectorGUI() {
    base.OnInspectorGUI();
  }

}