using Leap.Unity.Attributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PagedContentController : MonoBehaviour {

  public GameObject[] pages;

  [SerializeField, OnEditorChange("pageIdx")]
  private int _pageIdx = 0;
  public int pageIdx {
    get {
      return _pageIdx;
    }
    set {
      _pageIdx = Mathf.Max(0, Mathf.Min(pages.Length - 1, value));

      disableOtherPages(_pageIdx);
      enablePage(_pageIdx);
    }
  }

  [Header("Page Index Controller")]
  public RadioToggleGroup pageIndexController;

  void OnValidate() {
    _pageIdx = Mathf.Max(0, Mathf.Min(pages.Length, pageIdx));
  }

  void Awake() {
    pageIndexController.OnIndexToggled += (i) => {
      pageIdx = i;
    };
  }

  //private bool _firstFrame = true;
  //private bool _secondFrame = false;
  //void Update() {
  //  if (_secondFrame && !_firstFrame) {
  //    pageIdx = pageIdx;
  //    _secondFrame = false;
  //  }

  //  if (_firstFrame) {
  //    for (int i = 0; i < pages.Length; i++) {
  //      if (i == pageIdx) continue;
  //      pages[i].SetActive(true);
  //    }
  //    _firstFrame = false;
  //    _secondFrame = true;
  //  }
  //}

  private void disableOtherPages(int pageIdx) {
    for (int i = 0; i < pages.Length; i++) {
      if (i == pageIdx) continue;
      pages[i].SetActive(false);
    }
  }

  private void enablePage(int pageIdx) {
    pages[pageIdx].SetActive(true);
  }

}
