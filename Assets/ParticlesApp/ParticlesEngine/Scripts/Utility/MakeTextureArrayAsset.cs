#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class MakeTextureArrayAsset : MonoBehaviour {
  public Texture2D[] textures;

  [ContextMenu("Make Asset")]
  public void MakeAsset() {
    Texture2DArray array = new Texture2DArray(textures[0].width, textures[0].height, textures.Length, textures[0].format, mipmap: false, linear: true);
    for (int i = 0; i < textures.Length; i++) {
      Graphics.CopyTexture(textures[i], 0, array, i);
    }
    array.Apply();

    AssetDatabase.CreateAsset(array, "Assets/TextureArray.asset");
    AssetDatabase.Refresh();
  }
}
#endif
