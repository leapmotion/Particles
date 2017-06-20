using System.Collections.Generic;
using UnityEngine;
using Leap;
using Leap.Unity;
using Leap.Unity.Query;
using Leap.Unity.Attributes;

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

  [Range(1, 20)]
  [SerializeField]
  private int _grabDelay = 5;

  [Range(0, 0.2f)]
  [SerializeField]
  private float _influenceNormalOffset = 0.1f;

  [Range(0, 0.2f)]
  [SerializeField]
  private float _influenceForwardOffset = 0.03f;

  [Header("Field")]
  [SerializeField]
  private Transform _fieldCenter;

  [Range(0, 2)]
  [SerializeField]
  private float _fieldRadius = 1;

  [Range(0, 0.001f)]
  [SerializeField]
  private float _fieldForce = 0.0005f;

  [Header("Simulation")]
  [SerializeField]
  private EcosystemPreset _startingEcosystem = EcosystemPreset.Fluidy;

  [SerializeField]
  private string _seed;

  [MinValue(8)]
  [SerializeField]
  private int _maxParticles = 4096;

  [Range(1, 1024)]
  [SerializeField]
  private int _maxSocialSteps = 8;

  [SerializeField]
  private RenderTextureFormat _textureFormat = RenderTextureFormat.ARGBFloat;

  [SerializeField]
  private Material _simulationMat;

  [Header("Display")]
  [SerializeField]
  private Mesh _particleMesh;

  [SerializeField]
  private Material _particleMat;

  [Header("Debug")]
  [SerializeField]
  private Renderer _positionDebug;

  [SerializeField]
  private Renderer _velocityDebug;

  [SerializeField]
  private Renderer _socialDebug;

  //Simulation
  private int stepsPerFrame = 1;
  private RenderTexture _frontPos, _frontVel, _backPos, _backVel;
  private RenderTexture _frontSocial, _backSocial;
  private RenderTexture _socialTemp;

  //Display
  private List<Mesh> _meshes = new List<Mesh>();

  //Hand interaction
  private Vector4[] _capsuleA = new Vector4[64];
  private Vector4[] _capsuleB = new Vector4[64];

  private Vector4[] _spheres = new Vector4[2];
  private Vector4[] _sphereVels = new Vector4[2];

  private HandActor[] _handActors = new HandActor[2];

  #region PUBLIC API

  public void SetStepsPerFrame(float value) {
    stepsPerFrame = Mathf.RoundToInt(value * 10);
  }

  public void ResetPositions() {
    GL.LoadPixelMatrix(0, 1, 1, 0);
    blitPos(4);
  }

  #endregion

  #region UNITY MESSAGES
  void Start() {
    _frontPos = createTexture();
    _frontVel = createTexture();
    _backPos = createTexture();
    _backVel = createTexture();
    _socialTemp = createTexture();
    _frontSocial = createTexture(_maxSocialSteps);
    _backSocial = createTexture(_maxSocialSteps);

    _simulationMat.SetTexture("_SocialTemp", _socialTemp);
    _simulationMat.SetTexture("_Position", _frontPos);
    _simulationMat.SetTexture("_Velocity", _frontVel);
    _simulationMat.SetTexture("_SocialForce", _frontSocial);

    generateMeshes();

    LoadPresetEcosystem(_startingEcosystem);

    ResetPositions();

    _handActors.Fill(() => new HandActor() { sim = this });
  }

  void Update() {
    doHandCollision();

    doHandInfluence();

    if (Input.GetKeyDown(KeyCode.Space)) {
      Random.InitState(Time.realtimeSinceStartup.GetHashCode());

      var gen = GetComponent<NameGenerator>();
      string name;
      if (gen == null) {
        name = Random.Range(0, 1000).ToString();
      } else {
        name = gen.GenerateName();
      }
      Debug.Log(name);

      LoadRandomEcosystem(name);
    }

    if (Input.GetKeyDown(KeyCode.L)) {
      LoadRandomEcosystem(_seed);
    }

    _simulationMat.SetVector("_FieldCenter", _fieldCenter.localPosition);
    _simulationMat.SetFloat("_FieldRadius", _fieldRadius);
    _simulationMat.SetFloat("_FieldForce", _fieldForce);

    GL.LoadPixelMatrix(0, 1, 1, 0);
    for (int i = 0; i < stepsPerFrame; i++) {
      blitVel(2);

      doParticleInteraction();

      blit("_SocialForce", ref _frontSocial, ref _backSocial, 5, 1);

      blitVel(3);
      blitPos(0);
    }

    _particleMat.mainTexture = _frontPos;
    _particleMat.SetTexture("_Velocity", _frontVel);
    foreach (var mesh in _meshes) {
      Graphics.DrawMesh(mesh, transform.localToWorldMatrix, _particleMat, 0);
    }

    _positionDebug.material.mainTexture = _frontPos;
    _velocityDebug.material.mainTexture = _frontVel;
    _socialDebug.material.mainTexture = _backSocial;
  }
  #endregion

  #region ECOSYSTEMS
  public const int MAX_SPECIES = 10;
  public const float MAX_SOCIAL_FORCE = 0.003f;
  public const float MAX_SOCIAL_RANGE = 0.5f;
  private const float MIN_DRAG = 0.05f;
  private const float MAX_DRAG = 0.3f;

  public enum EcosystemPreset {
    RedMennace,
    Chase,
    Mitosis,
    Fluidy
  }

  public void LoadPresetEcosystem(EcosystemPreset preset) {
    Color[] colors = new Color[10];
    Vector4[] socialData = new Vector4[MAX_SPECIES * MAX_SPECIES];
    Vector4[] speciesData = new Vector4[MAX_SPECIES];

    //Default colors are greyscale 0 to 1
    for (int i = 0; i < MAX_SPECIES; i++) {
      float p = i / (MAX_SPECIES - 1.0f);
      colors[i] = new Color(p, p, p, 1);
    }

    //Default social interactions are zero force with max range
    for (int i = 0; i < MAX_SPECIES; i++) {
      for (int j = 0; j < MAX_SPECIES; j++) {
        socialData[i * 10 + j] = new Vector2(0, MAX_SOCIAL_RANGE);
      }
    }

    //Default species always have max drag and 0 extra social steps
    for (int i = 0; i < MAX_SPECIES; i++) {
      speciesData[i] = new Vector2(MIN_DRAG, 0);
    }

    switch (preset) {
      case EcosystemPreset.RedMennace:
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

        int redSpecies = 0;

        float normalLove = MAX_SOCIAL_FORCE * 0.04f;
        float fearOfRed = MAX_SOCIAL_FORCE * -1.0f;
        float redLoveOfOthers = MAX_SOCIAL_FORCE * 2.0f;
        float redLoveOfSelf = MAX_SOCIAL_FORCE * 0.9f;

        float normalRange = MAX_SOCIAL_RANGE * 0.4f;
        float fearRange = MAX_SOCIAL_RANGE * 0.3f;
        float loveRange = MAX_SOCIAL_RANGE * 0.3f;
        float redSelfRange = MAX_SOCIAL_RANGE * 0.4f;

        for (int s = 0; s < MAX_SPECIES; s++) {
          speciesData[s] = new Vector2(Mathf.Lerp(MIN_DRAG, MAX_DRAG, 0.1f), 0);

          for (int o = 0; o < MAX_SPECIES; o++) {
            socialData[s * 10 + o] = new Vector2(normalLove, normalRange);
          }

          //------------------------------------
          // everyone fears red except for red
          // and red loves everyone
          //------------------------------------
          socialData[s * 10 + redSpecies] = new Vector2(fearOfRed, fearRange * ((float)s / (float)MAX_SPECIES));

          socialData[redSpecies * 10 + redSpecies] = new Vector2(redLoveOfSelf, redSelfRange);

          socialData[redSpecies * 10 + s] = new Vector2(redLoveOfOthers, loveRange);
        }
        break;
      case EcosystemPreset.Chase:
        for (int i = 0; i < MAX_SPECIES; i++) {
          speciesData[i] = new Vector2(MIN_DRAG, 0);
          socialData[i * 10 + i] = new Vector2(MAX_SOCIAL_FORCE * 0.1f, MAX_SOCIAL_RANGE);
        }

        colors[0] = new Color(0.7f, 0.0f, 0.0f);
        colors[1] = new Color(0.7f, 0.3f, 0.0f);
        colors[2] = new Color(0.7f, 0.7f, 0.0f);
        colors[3] = new Color(0.0f, 0.7f, 0.0f);
        colors[4] = new Color(0.0f, 0.0f, 0.7f);
        colors[5] = new Color(0.4f, 0.0f, 0.7f);
        colors[6] = new Color(1.0f, 0.3f, 0.3f);
        colors[7] = new Color(1.0f, 0.6f, 0.3f);
        colors[8] = new Color(1.0f, 1.0f, 0.3f);
        colors[9] = new Color(0.3f, 1.0f, 0.3f);

        float chase = 0.9f;
        socialData[0 * 10 + 1] = new Vector2(MAX_SOCIAL_FORCE * chase, MAX_SOCIAL_RANGE);
        socialData[1 * 10 + 2] = new Vector2(MAX_SOCIAL_FORCE * chase, MAX_SOCIAL_RANGE);
        socialData[2 * 10 + 3] = new Vector2(MAX_SOCIAL_FORCE * chase, MAX_SOCIAL_RANGE);
        socialData[3 * 10 + 4] = new Vector2(MAX_SOCIAL_FORCE * chase, MAX_SOCIAL_RANGE);
        socialData[4 * 10 + 5] = new Vector2(MAX_SOCIAL_FORCE * chase, MAX_SOCIAL_RANGE);
        socialData[5 * 10 + 6] = new Vector2(MAX_SOCIAL_FORCE * chase, MAX_SOCIAL_RANGE);
        socialData[6 * 10 + 7] = new Vector2(MAX_SOCIAL_FORCE * chase, MAX_SOCIAL_RANGE);
        socialData[7 * 10 + 8] = new Vector2(MAX_SOCIAL_FORCE * chase, MAX_SOCIAL_RANGE);
        socialData[8 * 10 + 9] = new Vector2(MAX_SOCIAL_FORCE * chase, MAX_SOCIAL_RANGE);
        socialData[9 * 10 + 0] = new Vector2(MAX_SOCIAL_FORCE * chase, MAX_SOCIAL_RANGE);

        float flee = -0.6f;
        float range = 0.8f * MAX_SOCIAL_RANGE;
        socialData[0 * 10 + 9] = new Vector2(MAX_SOCIAL_FORCE * flee, range);
        socialData[1 * 10 + 0] = new Vector2(MAX_SOCIAL_FORCE * flee, range);
        socialData[2 * 10 + 1] = new Vector2(MAX_SOCIAL_FORCE * flee, range);
        socialData[3 * 10 + 2] = new Vector2(MAX_SOCIAL_FORCE * flee, range);
        socialData[4 * 10 + 3] = new Vector2(MAX_SOCIAL_FORCE * flee, range);
        socialData[5 * 10 + 4] = new Vector2(MAX_SOCIAL_FORCE * flee, range);
        socialData[6 * 10 + 5] = new Vector2(MAX_SOCIAL_FORCE * flee, range);
        socialData[7 * 10 + 6] = new Vector2(MAX_SOCIAL_FORCE * flee, range);
        socialData[8 * 10 + 7] = new Vector2(MAX_SOCIAL_FORCE * flee, range);
        socialData[9 * 10 + 8] = new Vector2(MAX_SOCIAL_FORCE * flee, range);
        break;
      case EcosystemPreset.Mitosis:
        for (int i = 0; i < MAX_SPECIES; i++) {
          speciesData[i] = new Vector2(Mathf.Lerp(MIN_DRAG, MAX_DRAG, 0.1f), 0);

          for (var j = 0; j < MAX_SPECIES; j++) {
            float a = (j / (float)MAX_SPECIES * 0.9f) * MAX_SOCIAL_FORCE * 1.0f;
            float b = (i / (float)MAX_SPECIES * 1.2f) * MAX_SOCIAL_FORCE * 0.4f;

            socialData[i * 10 + j] = new Vector2(a - b, MAX_SOCIAL_RANGE * 0.7f);
          }
        }

        colors[9] = new Color(0.9f, 0.9f, 0.9f);
        colors[8] = new Color(0.9f, 0.7f, 0.3f);
        colors[7] = new Color(0.9f, 0.4f, 0.2f);
        colors[6] = new Color(0.9f, 0.3f, 0.3f);
        colors[5] = new Color(0.6f, 0.3f, 0.6f);
        colors[4] = new Color(0.5f, 0.3f, 0.7f);
        colors[3] = new Color(0.2f, 0.2f, 0.3f);
        colors[2] = new Color(0.1f, 0.1f, 0.3f);
        colors[1] = new Color(0.0f, 0.0f, 0.3f);
        colors[0] = new Color(0.0f, 0.0f, 0.0f);
        break;
      case EcosystemPreset.Fluidy:
        for (var i = 0; i < MAX_SPECIES; i++) {
          for (var j = 0; j < MAX_SPECIES; j++) {
            socialData[i * 10 + j] = new Vector2(0, 0);
          }

          socialData[i * 10 + i] = new Vector2(0.2f * MAX_SOCIAL_FORCE, MAX_SOCIAL_RANGE * 0.1f);
        }

        for (var i = 0; i < MAX_SPECIES; i++) {
          for (var j = i + 1; j < MAX_SPECIES; j++) {
            socialData[i * 10 + j] = new Vector2(0.15f * MAX_SOCIAL_FORCE, MAX_SOCIAL_RANGE);
            socialData[j * 10 + i] = new Vector2(-0.1f * MAX_SOCIAL_FORCE, MAX_SOCIAL_RANGE * 0.3f);
          }
        }
        break;
    }

    //Invert drag before we upload to the GPU
    for (int i = 0; i < MAX_SPECIES; i++) {
      Vector2 species = speciesData[i];
      species.x = 1 - species.x;
      speciesData[i] = species;
    }

    _simulationMat.SetVectorArray("_SocialData", socialData);
    _simulationMat.SetVectorArray("_SpeciesData", speciesData);
    _particleMat.SetColorArray("_Colors", colors);
  }

  public void LoadRandomEcosystem(string seed) {
    Random.InitState(seed.GetHashCode());

    Color[] colors = new Color[MAX_SPECIES];
    for (int i = 0; i < colors.Length; i++) {
      colors[i] = Color.HSVToRGB(Random.value, Random.Range(0.5f, 1), Random.Range(0.3f, 1));
    }


    Vector4[] _socialData = new Vector4[MAX_SPECIES * MAX_SPECIES];

    for (int s = 0; s < MAX_SPECIES; s++) {
      for (int o = 0; o < MAX_SPECIES; o++) {
        _socialData[s * 10 + o] = new Vector2(Random.Range(-MAX_SOCIAL_FORCE, MAX_SOCIAL_FORCE), Random.value * MAX_SOCIAL_RANGE);
      }
    }

    Vector4[] speciesData = new Vector4[MAX_SPECIES];
    for (int i = 0; i < MAX_SPECIES; i++) {
      Vector4 data = new Vector4();
      data.x = Random.Range(MIN_DRAG, MAX_DRAG);
      data.y = Random.Range(0, _maxSocialSteps);
      speciesData[i] = data;
    }

    //Invert drag before we upload to the GPU
    for (int i = 0; i < MAX_SPECIES; i++) {
      Vector2 species = speciesData[i];
      species.x = 1 - species.x;
      speciesData[i] = species;
    }

    _particleMat.SetColorArray("_Colors", colors);
    _simulationMat.SetVectorArray("_SpeciesData", speciesData);
    _simulationMat.SetVectorArray("_SocialData", _socialData);
  }
  #endregion

  #region HAND INTERACTION

  private void doHandInfluence() {
    _handActors[0].Update(Hands.Left);
    _handActors[1].Update(Hands.Right);

    int sphereCount = 0;
    if (_handActors[0].active) {
      _spheres[sphereCount] = _handActors[0].sphere;
      _sphereVels[sphereCount] = _handActors[0].velocity;
      sphereCount++;
    }

    if (_handActors[1].active) {
      _spheres[sphereCount] = _handActors[1].sphere;
      _sphereVels[sphereCount] = _handActors[1].velocity;
      sphereCount++;
    }

    _simulationMat.SetInt("_SphereCount", sphereCount);
    _simulationMat.SetVectorArray("_Spheres", _spheres);
    _simulationMat.SetVectorArray("_SphereVelocities", _sphereVels);
  }

  private void doHandCollision() {
    int capsuleCount = 0;
    foreach (var hand in _provider.CurrentFrame.Hands) {
      foreach (var finger in hand.Fingers) {
        foreach (var bone in finger.bones) {
          _capsuleA[capsuleCount] = bone.PrevJoint.ToVector3();
          _capsuleB[capsuleCount] = bone.NextJoint.ToVector3();
          capsuleCount++;
        }
      }
    }

    _simulationMat.SetInt("_CapsuleCount", capsuleCount);
    _simulationMat.SetVectorArray("_CapsuleA", _capsuleA);
    _simulationMat.SetVectorArray("_CapsuleB", _capsuleB);
  }

  private class HandActor {
    public TextureSimulator sim;

    public Vector3 position, prevPosition;
    public int frameCount = 0;
    public bool active;

    public Vector4 sphere {
      get {
        Vector4 s = prevPosition;
        s.w = sim._influenceRadius;
        return s;
      }
    }

    public Vector3 velocity {
      get {
        return position - prevPosition;
      }
    }

    public void Update(Hand hand) {
      if (hand != null && hand.GrabAngle > sim._grabThreshold) {
        frameCount = Mathf.Min(sim._grabDelay, frameCount + 1);
      } else {
        frameCount = Mathf.Max(0, frameCount - 1);
      }

      if (hand != null) {
        prevPosition = position;
        position = hand.PalmPosition.ToVector3() + hand.PalmarAxis() * sim._influenceNormalOffset + hand.DistalAxis() * sim._influenceForwardOffset;
      }

      if (active && frameCount == 0) {
        active = false;
      } else if (!active && frameCount == sim._grabDelay) {
        active = true;
        prevPosition = position;
      }

      if (active) {
        Graphics.DrawMesh(sim._influenceMesh, Matrix4x4.TRS(position, Quaternion.identity, Vector3.one * sim._influenceRadius * 2), sim._influenceMat, 0);
      }
    }
  }

  #endregion

  #region PRIVATE IMPLEMENTATION
  private void generateMeshes() {
    var sourceVerts = _particleMesh.vertices;
    var sourceTris = _particleMesh.triangles;

    List<Vector3> bakedVerts = new List<Vector3>();
    List<int> bakedTris = new List<int>();
    List<Vector2> bakedUvs = new List<Vector2>();

    Mesh bakedMesh = null;
    for (int i = 0; i < _maxParticles; i++) {
      if (bakedVerts.Count + sourceVerts.Length > 60000) {
        bakedMesh.SetVertices(bakedVerts);
        bakedMesh.SetTriangles(bakedTris, 0);
        bakedMesh.SetUVs(0, bakedUvs);
        bakedMesh.RecalculateNormals();
        bakedMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 10000);
        bakedMesh = null;

        bakedVerts.Clear();
        bakedTris.Clear();
        bakedUvs.Clear();
      }

      if (bakedMesh == null) {
        sourceTris = _particleMesh.triangles;
        bakedMesh = new Mesh();
        bakedMesh.hideFlags = HideFlags.HideAndDontSave;
        _meshes.Add(bakedMesh);
      }

      bakedVerts.AddRange(sourceVerts);
      bakedTris.AddRange(sourceTris);

      for (int k = 0; k < sourceVerts.Length; k++) {
        bakedUvs.Add(new Vector2((i + 0.5f) / _maxParticles, 0));
      }

      for (int k = 0; k < sourceTris.Length; k++) {
        sourceTris[k] += sourceVerts.Length;
      }
    }

    bakedMesh.hideFlags = HideFlags.HideAndDontSave;
    bakedMesh.SetVertices(bakedVerts);
    bakedMesh.SetTriangles(bakedTris, 0);
    bakedMesh.SetUVs(0, bakedUvs);
    bakedMesh.RecalculateNormals();
    bakedMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 10000);
  }

  private RenderTexture createTexture(int height = 1) {
    RenderTexture tex = new RenderTexture(_maxParticles, height, 0, _textureFormat, RenderTextureReadWrite.Linear);
    tex.wrapMode = TextureWrapMode.Clamp;
    tex.filterMode = FilterMode.Point;

    RenderTexture.active = tex;
    GL.Clear(clearDepth: false, clearColor: true, backgroundColor: Color.blue);
    RenderTexture.active = null;
    return tex;
  }

  private void blit(string propertyName, ref RenderTexture front, ref RenderTexture back, int pass, float height) {
    RenderTexture.active = front;
    GL.Clear(clearDepth: false, clearColor: true, backgroundColor: Color.black);

    _simulationMat.SetPass(pass);

    GL.Begin(GL.QUADS);

    GL.TexCoord2(0, 1);
    GL.Vertex3(0, 0, 0);

    GL.TexCoord2(1, 1);
    GL.Vertex3(1, 0, 0);

    GL.TexCoord2(1, 0);
    GL.Vertex3(1, height, 0);

    GL.TexCoord2(0, 0);
    GL.Vertex3(0, height, 0);

    GL.End();

    _simulationMat.SetTexture(propertyName, front);

    Utils.Swap(ref front, ref back);
  }

  private RenderBuffer[] _colorBuffers = new RenderBuffer[2];
  private void doParticleInteraction() {
    _colorBuffers[0] = _frontVel.colorBuffer;
    _colorBuffers[1] = _socialTemp.colorBuffer;

    Graphics.SetRenderTarget(_colorBuffers, _frontVel.depthBuffer);
    GL.Clear(clearDepth: false, clearColor: true, backgroundColor: Color.black);

    _simulationMat.SetPass(1);

    quad();

    _simulationMat.SetTexture("_Velocity", _frontVel);

    Utils.Swap(ref _frontVel, ref _backVel);
  }

  private void blitVel(int pass) {
    blit("_Velocity", ref _frontVel, ref _backVel, pass, 1);
  }

  private void blitPos(int pass) {
    blit("_Position", ref _frontPos, ref _backPos, pass, 1);
  }

  private void quad(float height = 1) {
    GL.Begin(GL.QUADS);

    GL.TexCoord2(0, 0);
    GL.Vertex3(0, 0, 0);

    GL.TexCoord2(1, 0);
    GL.Vertex3(1, 0, 0);

    GL.TexCoord2(1, 1);
    GL.Vertex3(1, height, 0);

    GL.TexCoord2(0, 1);
    GL.Vertex3(0, height, 0);

    GL.End();
  }
  #endregion

}