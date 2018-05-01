using Leap.Unity.GraphicalRenderer;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Debug_SetTextWithPulsatorValue : MonoBehaviour {

  public LeapTextGraphic textGraphic;
  public LeapGraphicButtonPaletteController lgbpc;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
    textGraphic.text = lgbpc.pressPulsator.value.ToString();

  }
}
