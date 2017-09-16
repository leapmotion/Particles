using Leap.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Leap.Unity.Layout {

  [CustomEditor(typeof(HorizontalPointLayout))]
  public class HorizontalPointLayoutEditor : CustomEditorBase<HorizontalPointLayout> {

    protected override void OnEnable() {
      base.OnEnable();

      //specifyCustomDecorator("layoutTransforms", drawLayoutTransformsDecorator);
    }

    //private void drawLayoutTransformsDecorator(SerializedProperty property) {

    //}

  }

}
