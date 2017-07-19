using Leap.Unity.Attributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class TabBarController : MonoBehaviour {

  public RadioToggleGroup tabGroup;

  [SerializeField, Disable]
  private Vector3 _localOffsetFromTab = Vector3.zero;
  private Vector3 _targetLocalPos = Vector3.zero;

  void Awake() {
    if (Application.isPlaying) {
      tabGroup.OnIndexToggled += onIndexToggled;

      _targetLocalPos = this.transform.localPosition;
    }
  }

  private void onIndexToggled(int idx) {
    _targetLocalPos = tabGroup.activeToggle.RelaxedLocalPosition + _localOffsetFromTab;
  }

  void Update() {
    if (Application.isPlaying) {
      this.transform.localPosition = Vector3.Lerp(this.transform.localPosition, _targetLocalPos, 20F * Time.deltaTime);
    }
    else {
      _localOffsetFromTab = this.transform.localPosition - tabGroup.activeToggle.RelaxedLocalPosition;
    }
  }

  void OnDrawGizmos() {
    Gizmos.color = Color.red;
    Gizmos.DrawSphere(this.transform.parent.TransformPoint(tabGroup.activeToggle.RelaxedLocalPosition), 0.01F);
  }

}
