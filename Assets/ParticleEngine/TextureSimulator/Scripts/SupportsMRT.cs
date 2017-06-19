using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SupportsMRT : MonoBehaviour {

  // Use this for initialization
  void Start() {
    GetComponent<TextMesh>().text = SystemInfo.maxTextureSize.ToString();
    
  }

  // Update is called once per frame
  void Update() {

  }
}
