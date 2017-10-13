using UnityEngine;

namespace Leap.Unity {

  public static class LeapColor {

    public static Color black {
      get { return Color.black; }
    }

    public static Color gray {
      get { return Lerp(white, black, 0.5f); }
    }

    public static Color white {
      get { return Color.white; }
    }

    public static Color pink {
      get { return Lerp(red, white, 0.5f); }
    }

    public static Color magenta {
      get { return Color.magenta; }
    }

    public static Color red {
      get { return Color.red; }
    }

    public static Color orange {
      get { return Lerp(red, yellow, 0.5f); }
    }

    public static Color yellow {
      get { return Color.yellow; }
    }

    public static Color green {
      get { return Color.green; }
    }

    public static Color teal {
      get { return Lerp(cyan, green, 0.5f); }
    }

    public static Color cyan {
      get { return Color.cyan; }
    }

    public static Color blue {
      get { return Color.blue; }
    }

    public static Color Purple {
      get { return Lerp(magenta, blue, 0.5f); }
    }

    #region Shorthand

    private static Color Lerp(Color a, Color b, float amount) {
      return Color.Lerp(a, b, amount);
    }

    #endregion

  }

}