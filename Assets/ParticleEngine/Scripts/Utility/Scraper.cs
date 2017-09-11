using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEngine;

public class Scraper : MonoBehaviour {

  public static Regex regex = new Regex(@">([\w\s]+)</a></span>&nbsp; &nbsp;<span class=");
  public TextAsset asset;

  [ContextMenu("try it")]
  void tryit() {
    var matches = regex.Matches(asset.text);

    using (var writer = File.CreateText("molecules.txt")) {
      for (int i = 0; i < matches.Count; i++) {
        var match = matches[i];
        writer.WriteLine(match.Groups[1]);
      }
    }
  }
}
