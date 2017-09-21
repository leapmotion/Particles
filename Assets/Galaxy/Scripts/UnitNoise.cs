using UnityEngine;

public static class UnitNoise {

  public static Texture2D Create(int resolution, TextureFormat format) {
    Texture2D tex = new Texture2D(resolution, resolution, format, mipmap: false, linear: true);
    tex.filterMode = FilterMode.Point;
    for (int x = 0; x < tex.width; x++) {
      for (int y = 0; y < tex.height; y++) {
        Vector3 v = Random.onUnitSphere;
        tex.SetPixel(x, y, new Color(v.x, v.y, v.z, 1));
      }
    }
    tex.Apply(updateMipmaps: false, makeNoLongerReadable: true);
    return tex;
  }

}
