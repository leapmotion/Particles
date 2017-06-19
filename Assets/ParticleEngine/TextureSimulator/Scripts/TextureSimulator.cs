using System.Collections.Generic;
using UnityEngine;
using Leap;
using Leap.Unity;
using Leap.Unity.Query;

public class TextureSimulator : MonoBehaviour {

  [SerializeField]
  public LeapProvider _provider;

  [Header("Hand Influence")]
  [SerializeField]
  private Material _influenceMat;

  [SerializeField]
  private Mesh _influenceMesh;

  [Range(0, 0.2f)]
  [SerializeField]
  private float _influenceRadius = 0.1f;

  [Range(0, 1)]
  [SerializeField]
  private float _grabThreshold = 0.35f;

  [Range(0, 0.2f)]
  [SerializeField]
  private float _influenceNormalOffset = 0.1f;

  [Range(0, 0.2f)]
  [SerializeField]
  private float _influenceForwardOffset = 0.03f;

  [Header("Simulation")]
  [SerializeField]
  private string _seed;

  [SerializeField]
  private RenderTexture _positionTex;

  [SerializeField]
  private RenderTexture _velocityTex;

  [SerializeField]
  private Material _simulationMat;

  [Header("Display")]
  [SerializeField]
  private Mesh _particleMesh;

  [SerializeField]
  private Material _particleMat;

  private List<Mesh> _meshes = new List<Mesh>();

  void Start() {
    Graphics.Blit(_positionTex, _positionTex, _simulationMat, 4);

    var verts = _particleMesh.vertices;
    for (int i = 0; i < verts.Length; i++) {
      verts[i] = verts[i] * 1;
    }

    var tris = _particleMesh.triangles;

    List<Vector3> pos = new List<Vector3>();
    List<int> tri = new List<int>();
    List<Vector2> uv = new List<Vector2>();

    Mesh _mesh = null;
    for (int i = 0; i < _positionTex.width; i++) {
      for (int j = 0; j < _positionTex.height; j++) {
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
          tris = _particleMesh.triangles;
          _mesh = new Mesh();
          _mesh.hideFlags = HideFlags.HideAndDontSave;
          _meshes.Add(_mesh);
        }

        pos.AddRange(verts);
        tri.AddRange(tris);

        for (int k = 0; k < verts.Length; k++) {
          uv.Add(new Vector2((i + 0.5f) / (float)_positionTex.width, (j + 0.5f) / (float)_positionTex.height));
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

    const int MAX_SPECIES = 10;
    const float MAX_SOCIAL_FORCE = 0.003f;
    const float MAX_SOCIAL_RANGE = 0.5f;

    Color[] colors = new Color[10];
    Vector4[] _socialData = new Vector4[MAX_SPECIES * MAX_SPECIES];

    for (int i = 0; i < MAX_SPECIES; i++) {
      float p = i / (MAX_SPECIES - 1.0f);
      colors[i] = new Color(p, p, p, 1);
    }

    //Red mennace
    //{
    //  int redSpecies = 0;

    //  float normalLove = MAX_SOCIAL_FORCE * 0.04f;
    //  float fearOfRed = MAX_SOCIAL_FORCE * -1.0f;
    //  float redLoveOfOthers = MAX_SOCIAL_FORCE * 2.0f;
    //  float redLoveOfSelf = MAX_SOCIAL_FORCE * 0.9f;

    //  float normalRange = MAX_SOCIAL_RANGE * 0.4f;
    //  float fearRange = MAX_SOCIAL_RANGE * 0.3f;
    //  float loveRange = MAX_SOCIAL_RANGE * 0.3f;
    //  float redSelfRange = MAX_SOCIAL_RANGE * 0.4f;

    //  colors[0] = new Color(1.0f, 0.0f, 0.0f);
    //  colors[1] = new Color(0.3f, 0.2f, 0.0f);
    //  colors[2] = new Color(0.3f, 0.3f, 0.0f);
    //  colors[3] = new Color(0.0f, 0.3f, 0.0f);
    //  colors[4] = new Color(0.0f, 0.0f, 0.3f);
    //  colors[5] = new Color(0.3f, 0.0f, 0.3f);
    //  colors[6] = new Color(0.3f, 0.3f, 0.3f);
    //  colors[7] = new Color(0.3f, 0.4f, 0.3f);
    //  colors[8] = new Color(0.3f, 0.4f, 0.3f);
    //  colors[9] = new Color(0.3f, 0.2f, 0.3f);


    //  for (int s = 0; s < MAX_SPECIES; s++) {
    //    for (int o = 0; o < MAX_SPECIES; o++) {
    //      _socialData[s * 10 + o] = new Vector2(normalLove, normalRange);
    //    }

    //    _socialData[s * 10 + redSpecies] = new Vector2(fearOfRed, fearRange * ((float)(s + 1) / (float)MAX_SPECIES));
    //    _socialData[redSpecies * 10 + redSpecies] = new Vector2(redLoveOfSelf, redSelfRange);
    //    _socialData[redSpecies * 10 + s] = new Vector2(redLoveOfOthers, loveRange);
    //  }
    //}

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

    //Chasers
    {
      for (var i = 0; i < MAX_SPECIES; i++) {
        for (var j = 0; j < MAX_SPECIES; j++) {
          _socialData[i * 10 + j] = new Vector2(0, 0);
        }

        _socialData[i * 10 + i] = new Vector2(0.2f * MAX_SOCIAL_FORCE, MAX_SOCIAL_RANGE * 0.1f);
      }

      for (var i = 0; i < MAX_SPECIES; i++) {
        for (var j = i + 1; j < MAX_SPECIES; j++) {
          _socialData[i * 10 + j] = new Vector2(0.15f * MAX_SOCIAL_FORCE, MAX_SOCIAL_RANGE);
          _socialData[j * 10 + i] = new Vector2(-0.1f * MAX_SOCIAL_FORCE, MAX_SOCIAL_RANGE * 0.3f);
        }
      }
    }

    _simulationMat.SetVectorArray("_SocialData", _socialData);
    _particleMat.SetColorArray("_Colors", colors);
  }

  int count = 1;

  public void SetCount(float value) {
    count = Mathf.RoundToInt(value * 10);
  }

  public void ResetTheSim() {
    Graphics.Blit(_positionTex, _positionTex, _simulationMat, 4);
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
    foreach (var hand in _provider.CurrentFrame.Hands) {
      foreach (var finger in hand.Fingers) {
        foreach (var bone in finger.bones) {
          _capsuleA[capsuleCount] = bone.PrevJoint.ToVector3();
          _capsuleB[capsuleCount] = bone.NextJoint.ToVector3();
          capsuleCount++;
        }
      }

      if (hand.GrabStrength > _grabThreshold) {
        Vector4 pos = (hand.PalmPosition + hand.PalmNormal * _influenceNormalOffset).ToVector3() + hand.DistalAxis() * _influenceForwardOffset;
        pos.w = _influenceRadius;
        Graphics.DrawMesh(_influenceMesh, Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one * _influenceRadius * 2), _influenceMat, 0);

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

    hadLeft = _provider.CurrentFrame.Hands.Query().Any(h => h.IsLeft && h.GrabStrength > _grabThreshold);
    hadRight = _provider.CurrentFrame.Hands.Query().Any(h => h.IsRight && h.GrabStrength > _grabThreshold);

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

    _simulationMat.SetInt("_CapsuleCount", capsuleCount);
    _simulationMat.SetVectorArray("_CapsuleA", _capsuleA);
    _simulationMat.SetVectorArray("_CapsuleB", _capsuleB);

    _simulationMat.SetInt("_SphereCount", sphereCount);
    _simulationMat.SetVectorArray("_Spheres", _spheres);
    _simulationMat.SetVectorArray("_SphereVelocities", _sphereVels);



    if (Input.GetKeyDown(KeyCode.Space)) {
      Random.InitState(Time.realtimeSinceStartup.GetHashCode());

      var gen = GetComponent<NameGenerator>();
      string name = gen.GenerateName();
      Debug.Log(name);

      genWith(name);
    }

    if (Input.GetKeyDown(KeyCode.L)) {
      genWith(_seed);
    }


    _simulationMat.SetFloat("_Offset", (Time.frameCount % 2) / (float)_velocityTex.width);

    for (int i = 0; i < count; i++) {
      using (new ProfilerSample("A")) {
        Graphics.Blit(_positionTex, _velocityTex, _simulationMat, 2);
      }

      using (new ProfilerSample("B")) {
        Graphics.Blit(_positionTex, _velocityTex, _simulationMat, 1);
      }

      using (new ProfilerSample("C")) {
        Graphics.Blit(_positionTex, _velocityTex, _simulationMat, 3);
      }

      using (new ProfilerSample("D")) {
        Graphics.Blit(_velocityTex, _positionTex, _simulationMat, 0);
      }

      using (new ProfilerSample("E")) {
        GL.Flush();
      }
    }

    Graphics.DrawMeshInstanced(_particleMesh, 0, _particleMat, )
    foreach (var mesh in _meshes) {
      Graphics.DrawMesh(mesh, Matrix4x4.identity, _particleMat, 0);
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
    _particleMat.SetColorArray("_Colors", colors);

    Vector4[] _socialData = new Vector4[MAX_SPECIES * MAX_SPECIES];

    for (int s = 0; s < MAX_SPECIES; s++) {
      for (int o = 0; o < MAX_SPECIES; o++) {
        _socialData[s * 10 + o] = new Vector2(Random.Range(-MAX_SOCIAL_FORCE, MAX_SOCIAL_FORCE), Random.value * MAX_SOCIAL_RANGE);
      }
    }

    _simulationMat.SetVectorArray("_SocialData", _socialData);
  }

}