using System.Linq;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class EquivilanceTests : MonoBehaviour {

  public bool resetValues = false;
  public int spawnCount = 100;

  [Range(0, 10)]
  public float checkRange = 0.5f;
  public float gridSize = 0.1f;

  [Header("Actual")]
  public Vector3 actualForce;
  public int actualCount;

  [Header("Method 1")]
  public Vector3 method1Force;
  public float method1Count;

  [Header("Method 2")]
  public int method2Select = 5;
  public Vector3 method2Force;
  public float method2Count;

  [Header("Method 3")]
  public int method3Threshold = 5;
  public Vector3 method3Force;
  public float method3Count;

  private List<Vector3> points = new List<Vector3>();

  private void Update() {
    if (resetValues) {
      resetValues = false;
      points.Clear();
      for (int i = 0; i < spawnCount; i++) {
        points.Add(Random.insideUnitSphere);
      }
    }
  }

  private void OnDrawGizmos() {
    {
      actualForce = Vector3.zero;
      actualCount = 0;
      foreach (var point in points) {
        Vector3 toPoint = point - transform.position;
        if (toPoint.magnitude < checkRange) {
          Gizmos.color = Color.green;
          Gizmos.DrawWireSphere(point, 0.02f);
          Vector3 dir = toPoint.normalized;
          actualForce += dir;
          actualCount++;
        }
      }
      actualForce /= actualCount;

      Dictionary<Point, Vector3> pointCenter = new Dictionary<Point, Vector3>();
      Dictionary<Point, List<Vector3>> pointList = new Dictionary<Point, List<Vector3>>();
      Dictionary<Point, int> pointCount = new Dictionary<Point, int>();
      foreach (var point in points) {
        Vector3 gridPoint = point / gridSize;
        Point p = new Point(gridPoint);

        Vector3 center;
        if (!pointCenter.TryGetValue(p, out center)) {
          center = Vector3.zero;
        }
        center += point;
        pointCenter[p] = center;

        int count;
        if (!pointCount.TryGetValue(p, out count)) {
          count = 0;
        }
        count++;
        pointCount[p] = count;

        List<Vector3> list;
        if (!pointList.TryGetValue(p, out list)) {
          list = new List<Vector3>();
        }
        list.Add(point);
        pointList[p] = list;
      }

      method1Force = Vector3.zero;
      method1Count = 0;
      foreach (var pair in pointCenter) {
        int count = pointCount[pair.Key];
        Vector3 center = pair.Value / count;
        float averageRadius = pointList[pair.Key].Select(v => Vector3.Distance(center, v)).
                                                  //Max();
                                                  Sum() / count;

        Vector3 toCenter = center - transform.position;
        float nearDist = Mathf.Max(0, toCenter.magnitude - averageRadius);
        float farDist = toCenter.magnitude + averageRadius;
        float percent = Mathf.Clamp01(Mathf.InverseLerp(nearDist, farDist, checkRange));

        if (percent > 0) {
          float effectiveCount = percent * count;
          Gizmos.color = Color.red;
          Gizmos.DrawWireSphere(center, 0.015f);
          Vector3 dir = toCenter.normalized;
          method1Force += dir * effectiveCount;
          method1Count += effectiveCount;
        }
      }
      method1Force /= method1Count;

      #region AAA
      method2Force = Vector3.zero;
      method2Count = 0;
      foreach (var pair in pointList) {
        var list = pair.Value;
        var count = pointCount[pair.Key];

        int inRange = 0;
        int maxChoose = Mathf.Min(list.Count, method2Select);

        HashSet<int> chosen = new HashSet<int>();
        for (int i = 0; i < maxChoose; i++) {
          int index;
          do {
            index = Random.Range(0, list.Count);
          } while (chosen.Contains(index));
          chosen.Add(index);

          Vector3 point = list[index];
          Vector3 toPoint = point - transform.position;
          if (toPoint.magnitude < checkRange) {
            inRange++;
            method2Force += toPoint.normalized * count / maxChoose;
            method2Count += count / (float)maxChoose;
          }
        }
      }
      method2Force /= method2Count;
      #endregion

      method3Force = Vector3.zero;
      method3Count = 0;
      foreach (var pair in pointList) {
        var list = pair.Value;
        var count = pointCount[pair.Key];
        var center = pointCenter[pair.Key] / count;

        if (count > method3Threshold) {
          Vector3 toCenter = center - transform.position;
          float nearDist = Mathf.Max(0, toCenter.magnitude - gridSize * 0.5f);
          float farDist = toCenter.magnitude + gridSize * 0.5f;
          float percent = Mathf.Clamp01(Mathf.InverseLerp(nearDist, farDist, checkRange));

          if (percent > 0) {
            float effectiveCount = percent * count;
            Vector3 dir = toCenter.normalized;
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(center, 0.017f);
            method3Force += dir * effectiveCount;
            method3Count += effectiveCount;
          }
        } else {
          foreach (var point in list) {
            Vector3 toPoint = point - transform.position;
            if (toPoint.magnitude < checkRange) {
              Gizmos.color = Color.magenta;
              Gizmos.DrawWireSphere(point, 0.017f);
              method3Force += toPoint.normalized;
              method3Count += 1;
            }
          }
        }
      }
      method3Force /= method3Count;
    }

    /*
    {
      Vector3 center = Vector3.zero;
      foreach (var point in points) {
        center += point;
      }
      center /= points.Count;

      float averageDistToCenter = 0;
      foreach (var point in points) {
        averageDistToCenter += (point - center).magnitude;
      }
      averageDistToCenter /= points.Count;

      float stdv = 0;
      foreach (var point in points) {
        stdv += Mathf.Pow((point - center).magnitude - averageDistToCenter, 2);
      }
      stdv = Mathf.Sqrt(stdv / (points.Count - 1));

      Gizmos.color = new Color(0, 1, 0, 1f);
      Gizmos.DrawSphere(center, averageDistToCenter - stdv);
      Gizmos.DrawWireSphere(center, averageDistToCenter + stdv);
    }
    */

    {
      HashSet<Point> drawn = new HashSet<Point>();

      foreach (var point in points) {
        Gizmos.color = Color.black;
        Gizmos.DrawSphere(point, 0.01f);

        Vector3 gridPoint = point / gridSize;
        Point p = new Point(gridPoint);

        if (drawn.Contains(p)) {
          continue;
        }
        drawn.Add(p);

        gridPoint = p;
        gridPoint *= gridSize;

        Gizmos.color = new Color(0, 0, 1, 0.1f);
        Gizmos.DrawWireCube(gridPoint + Vector3.one * gridSize * 0.5f, Vector3.one * gridSize);
      }

      Gizmos.color = Color.red;
      Gizmos.DrawRay(transform.position, method1Force);

      //Gizmos.color = Color.blue;
      //Gizmos.DrawRay(transform.position, method2Force);

      Gizmos.color = Color.magenta;
      Gizmos.DrawRay(transform.position, method3Force);

      Gizmos.color = Color.green;
      Gizmos.DrawRay(transform.position, actualForce);
    }
  }

  private struct Point {
    public int x, y, z;

    public Point(Vector3 p) {
      x = Mathf.FloorToInt(p.x);
      y = Mathf.FloorToInt(p.y);
      z = Mathf.FloorToInt(p.z);
    }

    public static implicit operator Vector3(Point p) {
      return new Vector3(p.x, p.y, p.z);
    }
  }
}
