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
      _pageIdx = Mathf.Max(0, Mathf.Min(pages.Length, value));

      disableOtherPages(_pageIdx);
      enablePage(_pageIdx);
    }
  }

  void OnValidate() {
    _pageIdx = Mathf.Max(0, Mathf.Min(pages.Length, pageIdx));
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
