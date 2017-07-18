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

  [Header("Page Controllers")]
  public RadioToggleGroup pageIndexController;

  void OnValidate() {
    _pageIdx = Mathf.Max(0, Mathf.Min(pages.Length, pageIdx));
  }

  void Awake() {
    //pageIndexController.OnIndexToggled += (i) => { pageIdx = i; };
  }

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
