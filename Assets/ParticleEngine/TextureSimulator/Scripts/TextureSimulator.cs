using System.Collections.Generic;
using UnityEngine;
using Leap;
using Leap.Unity;
using Leap.Unity.Query;

public class TextureSimulator : MonoBehaviour {

  public string seed;
  public LeapProvider provider;
  public Mesh mesh;
  public Material sphereMat;
  public Mesh sphereMesh;
  public float influenceRadius = 0.1f;
  public float grabThreshold = 0.5f;
  public float normalOffset = 0.1f;
  public float forwardOffset = 0.1f;
  public RenderTexture positions;
  public RenderTexture velocities;
  public Material mat;
  public Material displayMat;

  private List<Mesh> _meshes = new List<Mesh>();

  void Start() {
    Graphics.Blit(positions, positions, mat, 4);

    var verts = mesh.vertices;
    for (int i = 0; i < verts.Length; i++) {
      verts[i] = verts[i] * 1;
    }

    var tris = mesh.triangles;

    List<Vector3> pos = new List<Vector3>();
    List<int> tri = new List<int>();
    List<Vector2> uv = new List<Vector2>();

    Mesh _mesh = null;
    for (int i = 0; i < positions.width; i++) {
      for (int j = 0; j < positions.height; j++) {
        if (pos.Count + verts.Length > 60000) {
          _mesh.SetVertices(pos);
          _mesh.SetTriangles(tri, 0);
          _mesh.SetUVs(0, uv);
          _mesh.RecalculateNormals();
          _mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 10000);
          _mesh = null;

          pos.Clear();
          tri.Clear();
          uv.Clear();
        }

        if (_mesh == null) {
          tris = mesh.triangles;
          _mesh = new Mesh();
          _mesh.hideFlags = HideFlags.HideAndDontSave;
          _meshes.Add(_mesh);
        }

        pos.AddRange(verts);
        tri.AddRange(tris);

        for (int k = 0; k < verts.Length; k++) {
          uv.Add(new Vector2((i + 0.5f) / (float)positions.width, (j + 0.5f) / (float)positions.height));
        }

        for (int k = 0; k < tris.Length; k++) {
          tris[k] += verts.Length;
        }
      }
    }

    _mesh.hideFlags = HideFlags.HideAndDontSave;
    _mesh.SetVertices(pos);
    _mesh.SetTriangles(tri, 0);
    _mesh.SetUVs(0, uv);
    _mesh.RecalculateNormals();
    _mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 10000);

    Debug.Log("Supports RT: " + SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat));

    {
      Color[] colors = new Color[10];
      colors[0] = new Color(1.0f, 0.0f, 0.0f);
      colors[1] = new Color(0.3f, 0.2f, 0.0f);
      colors[2] = new Color(0.3f, 0.3f, 0.0f);
      colors[3] = new Color(0.0f, 0.3f, 0.0f);
      colors[4] = new Color(0.0f, 0.0f, 0.3f);
      colors[5] = new Color(0.3f, 0.0f, 0.3f);
      colors[6] = new Color(0.3f, 0.3f, 0.3f);
      colors[7] = new Color(0.3f, 0.4f, 0.3f);
      colors[8] = new Color(0.3f, 0.4f, 0.3f);
      colors[9] = new Color(0.3f, 0.2f, 0.3f);

      displayMat.SetColorArray("_Colors", colors);
    }

    {
      const int MAX_SPECIES = 10;
      const float MAX_SOCIAL_FORCE = 0.003f;
      const float MAX_SOCIAL_RANGE = 0.5f;

      int redSpecies = 0;

      float normalLove = MAX_SOCIAL_FORCE * 0.04f;
      float fearOfRed = MAX_SOCIAL_FORCE * -1.0f;
      float redLoveOfOthers = MAX_SOCIAL_FORCE * 2.0f;
      float redLoveOfSelf = MAX_SOCIAL_FORCE * 0.9f;

      float normalRange = MAX_SOCIAL_RANGE * 0.4f;
      float fearRange = MAX_SOCIAL_RANGE * 0.3f;
      float loveRange = MAX_SOCIAL_RANGE * 0.3f;
      float redSelfRange = MAX_SOCIAL_RANGE * 0.4f;

      Vector4[] _socialData = new Vector4[MAX_SPECIES * MAX_SPECIES];

      for (int s = 0; s < MAX_SPECIES; s++) {
        for (int o = 0; o < MAX_SPECIES; o++) {
          _socialData[s * 10 + o] = new Vector2(normalLove, normalRange);
        }

        _socialData[s * 10 + redSpecies] = new Vector2(fearOfRed, fearRange * ((float)(s + 1) / (float)MAX_SPECIES));
        _socialData[redSpecies * 10 + redSpecies] = new Vector2(redLoveOfSelf, redSelfRange);
        _socialData[redSpecies * 10 + s] = new Vector2(redLoveOfOthers, loveRange);
      }

      //Planet
      {
        for (var t = 0; t < MAX_SPECIES; t++) {
          for (var f = 0; f < MAX_SPECIES; f++) {
            _socialData[t * 10 + f] = new Vector2(0, 0);
          }
        }

        //A and B like their own species and each other a whole lot
        for (int i = 0; i < 2; i++) {
          for (int j = 0; j < 2; j++) {
            _socialData[i * 10 + j] = new Vector2(MAX_SOCIAL_FORCE * 0.2f, MAX_SOCIAL_RANGE);
          }
        }

        //All the other species like A a lot but hate B
        //they also hate their own species but don't care much about distance
        for (int i = 2; i < 10; i++) {
          _socialData[i * 10 + 0] = new Vector4(MAX_SOCIAL_FORCE * 0.01f, MAX_SOCIAL_RANGE);
          _socialData[i * 10 + 1] = new Vector4(-MAX_SOCIAL_FORCE * 0.6f, (i / 2) / (float)5 * MAX_SOCIAL_RANGE);
          _socialData[i * 10 + i] = new Vector4(-MAX_SOCIAL_FORCE * 0.5f, MAX_SOCIAL_RANGE * 0.1f);
        }
      }

      //Planets
      {
        for (var t = 0; t < MAX_SPECIES; t++) {
          for (var f = 0; f < MAX_SPECIES; f++) {
            _socialData[t * 10 + f] = new Vector2(-MAX_SOCIAL_FORCE * 0.1f, MAX_SOCIAL_RANGE * 0.3f);
          }

          _socialData[t * 10 + t] = new Vector4(MAX_SOCIAL_FORCE * 0.1f, MAX_SOCIAL_RANGE * 0.4f);
        }
      }


      /*
      for (var t = 0; t < MAX_SPECIES; t++) {
        for (var f = 0; f < MAX_SPECIES; f++) {
          _socialData[t * 10 + f] = new Vector2(-MAX_SOCIAL_FORCE * 0.1f, MAX_SOCIAL_RANGE);
        }
      }

      for (var t = 2; t < MAX_SPECIES; t++) {
        for (var f = 0; f < MAX_SPECIES; f++) {
          _socialData[t * 10 + f] = new Vector2(f * MAX_SOCIAL_FORCE * 2.0f / MAX_SPECIES - t * MAX_SOCIAL_FORCE * 0.7f / MAX_SPECIES,
                                                MAX_SOCIAL_RANGE * 0.5f);
        }
      }
      */

      mat.SetVectorArray("_SocialData", _socialData);
    }
  }

  int count = 1;

  public void SetCount(float value) {
    count = Mathf.RoundToInt(value * 10);
  }

  public void ResetTheSim() {
    Graphics.Blit(positions, positions, mat, 4);
  }


  private Vector4[] _capsuleA = new Vector4[64];
  private Vector4[] _capsuleB = new Vector4[64];

  private Vector4[] _spheres = new Vector4[2];
  private Vector4[] _sphereVels = new Vector4[2];

  private Vector4 _leftSphere, _leftVel, _prevLeft;
  private Vector4 _rightSphere, _rightVel, _prevRight;
  bool hadLeft = false;
  bool hadRight = false;
  void Update() {
    int capsuleCount = 0;
    foreach (var hand in provider.CurrentFrame.Hands) {
      foreach (var finger in hand.Fingers) {
        foreach (var bone in finger.bones) {
          _capsuleA[capsuleCount] = bone.PrevJoint.ToVector3();
          _capsuleB[capsuleCount] = bone.NextJoint.ToVector3();
          capsuleCount++;
        }
      }

      if (hand.GrabStrength > grabThreshold) {
        Vector4 pos = (hand.PalmPosition + hand.PalmNormal * normalOffset).ToVector3() + hand.DistalAxis() * forwardOffset;
        pos.w = influenceRadius;
        Graphics.DrawMesh(sphereMesh, Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one * influenceRadius * 2), sphereMat, 0);

        if (hand.IsLeft) {
          _prevLeft = pos;
          _leftVel = Vector3.zero;
          if (hadLeft) {
            _leftVel = pos - _leftSphere;
            _prevLeft = _leftSphere;
          }
          _leftSphere = pos;
        } else {
          _prevRight = pos;
          _rightVel = Vector3.zero;
          if (hadRight) {
            _prevRight = _rightSphere;
            _rightVel = pos - _rightSphere;
          }
          _rightSphere = pos;
        }
      }
    }

    hadLeft = provider.CurrentFrame.Hands.Query().Any(h => h.IsLeft && h.GrabStrength > grabThreshold);
    hadRight = provider.CurrentFrame.Hands.Query().Any(h => h.IsRight && h.GrabStrength > grabThreshold);

    int sphereCount = 0;
    if (hadLeft) {
      _spheres[sphereCount] = _prevLeft;
      _sphereVels[sphereCount] = _leftVel;
      sphereCount++;
    }

    if (hadRight) {
      _spheres[sphereCount] = _prevRight;
      _sphereVels[sphereCount] = _rightVel;
      sphereCount++;
    }

    mat.SetInt("_CapsuleCount", capsuleCount);
    mat.SetVectorArray("_CapsuleA", _capsuleA);
    mat.SetVectorArray("_CapsuleB", _capsuleB);

    mat.SetInt("_SphereCount", sphereCount);
    mat.SetVectorArray("_Spheres", _spheres);
    mat.SetVectorArray("_SphereVelocities", _sphereVels);



    if (Input.GetKeyDown(KeyCode.Space)) {
      Random.InitState(Time.realtimeSinceStartup.GetHashCode());

      var gen = GetComponent<NameGenerator>();
      string name = gen.GenerateName();
      Debug.Log(name);

      genWith(name);
    }

    if (Input.GetKeyDown(KeyCode.L)) {
      genWith(seed);
    }


    mat.SetFloat("_Offset", (Time.frameCount % 2) / (float)velocities.width);

    for (int i = 0; i < count; i++) {
      Graphics.Blit(positions, velocities, mat, 2);

      Graphics.Blit(positions, velocities, mat, 1);

      Graphics.Blit(positions, velocities, mat, 3);

      Graphics.Blit(velocities, positions, mat, 0);
    }

    foreach (var mesh in _meshes) {
      Graphics.DrawMesh(mesh, Matrix4x4.identity, displayMat, 0);
    }
  }

  private void genWith(string name) {
    const int MAX_SPECIES = 10;
    const float MAX_SOCIAL_FORCE = 0.003f;
    const float MAX_SOCIAL_RANGE = 0.5f;

    Random.InitState(name.GetHashCode());

    Color[] colors = new Color[MAX_SPECIES];
    for (int i = 0; i < colors.Length; i++) {
      colors[i] = Color.HSVToRGB(Random.value, Random.Range(0.5f, 1), Random.Range(0.3f, 1));
    }
    displayMat.SetColorArray("_Colors", colors);

    Vector4[] _socialData = new Vector4[MAX_SPECIES * MAX_SPECIES];

    for (int s = 0; s < MAX_SPECIES; s++) {
      for (int o = 0; o < MAX_SPECIES; o++) {
        _socialData[s * 10 + o] = new Vector2(Random.Range(-MAX_SOCIAL_FORCE, MAX_SOCIAL_FORCE), Random.value * MAX_SOCIAL_RANGE);
      }
    }

    mat.SetVectorArray("_SocialData", _socialData);
  }

}