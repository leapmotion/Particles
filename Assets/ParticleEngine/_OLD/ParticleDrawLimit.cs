using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Leap.Unity.Query;

public class ParticleDrawLimit : MonoBehaviour {
  public const int MAX_VERTS_PER_MESH = 65535;

  [SerializeField]
  private int _count;

  [SerializeField]
  private float _scale;

  [SerializeField]
  private float _radius;

  [SerializeField]
  private int _elementIndex;

  [SerializeField]
  private Mesh[] _elements;

  [SerializeField]
  private int _materialIndex;

  [SerializeField]
  private Material[] _materials;

  [SerializeField]
  private Method _method;

  [Header("Labels")]
  [SerializeField]
  private Text _countLabel;

  [SerializeField]
  private Text _scaleLabel;

  [SerializeField]
  private Text _radiusLabel;

  [SerializeField]
  private Text _fpsLabel;

  public float count {
    set {
      _count = Mathf.RoundToInt(20000 * value);
      Reload();
    }
  }

  public float scale {
    set {
      _scale = value;
      Reload();
    }
  }

  public float radius {
    set {
      _radius = value;
      Reload();
    }
  }

  public int elementIndex {
    set {
      _elementIndex = value;
      Reload();
    }
  }

  public int materialIndex {
    set {
      _materialIndex = value;
      Reload();
    }
  }

  public int method {
    set {
      _method = (Method)value;
      Reload();
    }
  }

  public enum Method {
    DrawMesh,
    DrawInstanced
  }

  private Matrix4x4[] _matrices = new Matrix4x4[1023];

  private Mesh _fullMesh;
  private int _elementsPerFullMesh;
  private Mesh _partialMesh;
  private int _elementsPerPartialMesh;

  public Mesh chosenElement {
    get {
      return _elements[_elementIndex];
    }
  }

  public void OnValidate() {
    if (Application.isPlaying) {
      Reload();
    }
  }

  private void Awake() {
    Reload();
  }

  private IEnumerator Start() {
    while (true) {
      yield return new WaitForSeconds(0.5f);
      _fpsLabel.text = "FPS: " + Mathf.RoundToInt(1.0f / Time.smoothDeltaTime);
    }
  }

  public void Reload() {
    _fullMesh = new Mesh();
    _partialMesh = new Mesh();

    _countLabel.text = "Count: " + _count;
    _scaleLabel.text = "Scale: " + _scale;
    _radiusLabel.text = "Radius: " + _radius;

    switch (_method) {
      case Method.DrawInstanced:
        for (int i = 0; i < 1023; i++) {
          _matrices[i] = Matrix4x4.TRS(Random.insideUnitSphere * _radius, Quaternion.identity, Vector3.one * _scale);
        }
        break;
      case Method.DrawMesh:
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();

        _elementsPerFullMesh = MAX_VERTS_PER_MESH / chosenElement.vertexCount;
        _elementsPerPartialMesh = _count;
        while (_elementsPerPartialMesh > _elementsPerFullMesh) {
          _elementsPerPartialMesh -= _elementsPerFullMesh;
        }

        for (int i = 0; i < _elementsPerFullMesh; i++) {
          chosenElement.triangles.Query().Select(t => t + verts.Count).AppendList(tris);
          chosenElement.vertices.Query().Select(v => v * _scale).AppendList(verts);
        }

        _fullMesh.Clear();
        _fullMesh.SetVertices(verts);
        _fullMesh.SetTriangles(tris, 0);
        _fullMesh.RecalculateNormals();
        _fullMesh.RecalculateBounds();

        verts.Clear();
        tris.Clear();
        for (int i = 0; i < _elementsPerPartialMesh; i++) {
          Vector3 offset = Random.insideUnitSphere * _radius;
          chosenElement.triangles.Query().Select(t => t + verts.Count).AppendList(tris);
          chosenElement.vertices.Query().Select(v => v * _scale + offset).AppendList(verts);
        }

        _partialMesh.Clear();
        _partialMesh.SetVertices(verts);
        _partialMesh.SetTriangles(tris, 0);
        _partialMesh.RecalculateNormals();
        _partialMesh.RecalculateBounds();
        break;
    }
  }

  private void Update() {
    int totalLeft = _count;

    switch (_method) {
      case Method.DrawInstanced:
        while (totalLeft > 0) {
          int toDraw = Mathf.Min(1023, totalLeft);
          totalLeft -= toDraw;

          Graphics.DrawMeshInstanced(chosenElement, 0, _materials[_materialIndex], _matrices, toDraw);
        }
        break;

      case Method.DrawMesh:
        while (totalLeft > 0) {
          if (totalLeft >= _elementsPerFullMesh) {
            Graphics.DrawMesh(_fullMesh, Matrix4x4.Translate(Random.insideUnitSphere * _radius), _materials[_materialIndex], 0);
            totalLeft -= _elementsPerFullMesh;
          } else {
            Graphics.DrawMesh(_partialMesh, Matrix4x4.Translate(Random.insideUnitSphere * _radius), _materials[_materialIndex], 0);
            totalLeft -= _elementsPerFullMesh;
          }
        }
        break;
    }
  }
}
