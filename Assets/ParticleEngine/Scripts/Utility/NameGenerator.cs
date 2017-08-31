using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;
using Leap.Unity.Query;

public class NameGenerator : MonoBehaviour {

  public TextAsset asset;
  public int keyLength = 1;

  private List<StartValue> _starts = new List<StartValue>();
  private Dictionary<Key, List<Value>> _mapping = new Dictionary<Key, List<Value>>();

  public class Key {
    public char[] chars;

    public override int GetHashCode() {
      Hash hash = new Hash();
      foreach (var character in chars) {
        hash.Add(character);
      }
      return hash;
    }

    public override bool Equals(object obj) {
      if (obj is Key) {
        Key other = obj as Key;
        for (int i = 0; i < chars.Length; i++) {
          if (chars[i] != other.chars[i]) {
            return false;
          }
        }
        return true;
      } else {
        return false;
      }
    }
  }

  public class Value {
    public int weight;
    public char character;
  }

  public class StartValue {
    public int weight;
    public char[] startChars;
  }

  public void Awake() {
    string[] lines = asset.text.Split('\n');
    foreach (var l in lines) {
      string line = l.Trim();
      if (string.IsNullOrEmpty(line)) {
        continue;
      }

      char[] starts = new char[keyLength];
      for (int t = 0; t < keyLength; t++) {
        starts[t] = line[t];
      }

      StartValue startValue = _starts.Query().FirstOrDefault(v => v.startChars.SequenceEqual(starts));
      if (startValue == null) {
        _starts.Add(new StartValue() {
          startChars = starts,
          weight = 1
        });
      } else {
        startValue.weight++;
      }

      for (int i = 0; i < line.Length - 3; i++) {
        char[] chars = new char[keyLength];
        for (int j = 0; j < keyLength; j++) {
          chars[j] = line[i + j];
        }
        Key key = new Key() {
          chars = chars
        };
        char value = line[i + keyLength];

        List<Value> valueList;
        if (!_mapping.TryGetValue(key, out valueList)) {
          valueList = new List<Value>();
          _mapping[key] = valueList;
        }

        var valueObj = valueList.Query().FirstOrDefault(v => v.character == value);
        if (valueObj == null) {
          valueList.Add(new Value() {
            character = value,
            weight = 1
          });
        } else {
          valueObj.weight++;
        }
      }
    }
  }

  public string GenerateName() {
    while (true) {
      top:

      string result = "";
      Key key = new Key() {
        chars = new char[keyLength]
      };

      {
        int totalWeight = _starts.Select(t => t.weight).Sum();
        int selection = UnityEngine.Random.Range(0, totalWeight);
        foreach (var start in _starts) {
          selection -= start.weight;
          if (selection <= 0) {
            Array.Copy(start.startChars, key.chars, keyLength);
            break;
          }
        }
      }

      result = result + new string(key.chars);

      while (true) {
        List<Value> values;
        if (!_mapping.TryGetValue(key, out values)) {
          goto top;
        }

        int totalWeight = values.Select(t => t.weight).Sum();
        int selection = UnityEngine.Random.Range(0, totalWeight);
        foreach (var value in values) {
          selection -= value.weight;
          if (selection <= 0) {
            result += value.character;

            for (int i = 0; i < keyLength - 1; i++) {
              key.chars[i] = key.chars[i + 1];
            }
            key.chars[keyLength - 1] = value.character;
            break;
          }
        }

        if (result.EndsWith(" ")) {
          result = new string(result.Trim().
                                     Split().
                                     Where(s => s.Length < 11).
                                     SelectMany(s => (s + " ").ToCharArray()).
                                     ToArray());
        }

        if (result.Trim().Split().Length == 2 && result.EndsWith(" ")) {

          result = new string(result.Trim().Split().SelectMany(s => char.ToUpper(s[0]) + s.Substring(1).ToLower() + " ").ToArray());
          result = result.Trim();
          return result;
        }
      }
    }
  }
}
