using Leap.Unity;
using Leap.Unity.RuntimeGizmos;
using UnityEngine;

public class LeapTRS2 : MonoBehaviour, IRuntimeGizmoComponent {

  #region Inspector

  [Header("Transform To Manipulate")]

  public Transform target;

  [Header("Grab Switches")]

  [SerializeField]
  private GrabSwitch _switchA;

  [SerializeField]
  private  GrabSwitch _switchB;

  [Header("Scale")]

  [SerializeField]
  private bool _allowScale = true;

  [SerializeField]
  private float _minScale = 0.01f;

  [SerializeField]
  private float _maxScale = 500f;

  [Header("Position Constraint")]
  
  [SerializeField]
  private bool _constrainPosition = false;

  [SerializeField]
  private float _constraintStrength = 1f;

  #endregion

  #region Unity Events

  void Update() {
    updateTRS();
  }

  #endregion

  #region TRS

  //private KabschSolver _kabsch = new KabschSolver();

  private RingBuffer<Pose> _aPoses = new RingBuffer<Pose>(2);
  private RingBuffer<Pose> _bPoses = new RingBuffer<Pose>(2);

  private void updateTRS() {
    int numGrasping = (_switchA != null && _switchA.grasped ? 1 : 0)
                    + (_switchB != null && _switchB.grasped ? 1 : 0);

    // Clear two-handed TRS state when not using two hands.
    if (numGrasping != 2) {
      _aPoses.Clear();
      _bPoses.Clear();
    }

    // Declare information for applying the TRS.
    var targetScale = target.localScale.x;
    var origCentroid = Vector3.zero;
    var nextCentroid = Vector3.zero;

    // Fill information based on the number of elements in the TRS.
    if (numGrasping == 0) {

    }
    else if (numGrasping == 1) {

    }
    else {
      _aPoses.Add(_switchA.pose);
      _bPoses.Add(_switchB.pose);

      if (_aPoses.IsFull && _bPoses.IsFull) {

        // Scale changes.
        float dist0 = Vector3.Distance(_aPoses[0].position, _bPoses[0].position);
        float dist1 = Vector3.Distance(_aPoses[1].position, _bPoses[1].position);

        float scaleChange = dist1 / dist0;

        targetScale *= scaleChange;

        // Translation.
        origCentroid = (_aPoses[0].position + _bPoses[0].position) / 2f;

        nextCentroid = (_aPoses[1].position + _bPoses[1].position) / 2f;

        // Twist.

      }
    }

    // Apply constraints.
    targetScale = Mathf.Clamp(targetScale, _minScale, _maxScale);


    // Apply TRS.
    var centroid = nextCentroid;

    // Translation.
    target.transform.position += (nextCentroid - origCentroid);

    // Scale from centroid pivot; remember local offset, scale, then correct.
    var centroidFromTarget = centroid.From(target.position);
    var centroidFromTarget_local = target.worldToLocalMatrix
                                         .MultiplyPoint3x4(centroidFromTarget);
    if (targetScale != target.localScale.x) {
      target.localScale = Vector3.one * targetScale;
    }
    var scaledCentroidFromTarget = target.localToWorldMatrix
                                         .MultiplyPoint3x4(centroidFromTarget_local);
    target.position += (centroidFromTarget - scaledCentroidFromTarget);
  }

  public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
    if (target == null) return;

    drawer.PushMatrix();
    //drawer.matrix = _prevTargetToAnchorMatrix;
    drawer.matrix = target.localToWorldMatrix;

    drawer.color = LeapColor.coral;
    drawer.DrawWireCube(Vector3.zero, Vector3.one * 0.20f);

    drawer.color = LeapColor.jade;
    drawer.DrawWireCube(Vector3.zero, Vector3.one * 0.10f);

    drawer.PopMatrix();
  }

  #endregion

  #region KabschSolver (with scaling)

  public class KabschSolver {
    Vector3[] QuatBasis = new Vector3[3];
    Vector3[] DataCovariance = new Vector3[3];
    Quaternion OptimalRotation = Quaternion.identity;
    public float scaleRatio = 1f;
    public Matrix4x4 SolveKabsch(Vector3[] inPoints, Vector4[] refPoints, bool solveRotation = true, bool solveScale = false) {
      if (inPoints.Length != refPoints.Length) { return Matrix4x4.identity; }

      //Calculate the centroid offset and construct the centroid-shifted point matrices
      Vector3 inCentroid = Vector3.zero; Vector3 refCentroid = Vector3.zero;
      float inTotal = 0f, refTotal = 0f;
      for (int i = 0; i < inPoints.Length; i++) {
        inCentroid += new Vector3(inPoints[i].x, inPoints[i].y, inPoints[i].z) * refPoints[i].w;
        inTotal += refPoints[i].w;
        refCentroid += new Vector3(refPoints[i].x, refPoints[i].y, refPoints[i].z) * refPoints[i].w;
        refTotal += refPoints[i].w;
      }
      inCentroid /= inTotal;
      refCentroid /= refTotal;

      //Calculate the scale ratio
      if (solveScale) {
        float inScale = 0f, refScale = 0f;
        for (int i = 0; i < inPoints.Length; i++) {
          inScale += (new Vector3(inPoints[i].x, inPoints[i].y, inPoints[i].z) - inCentroid).magnitude;
          refScale += (new Vector3(refPoints[i].x, refPoints[i].y, refPoints[i].z) - refCentroid).magnitude;
        }
        scaleRatio = (refScale / inScale);
      }

      //Calculate the 3x3 covariance matrix, and the optimal rotation
      if (solveRotation) {
        extractRotation(TransposeMultSubtract(inPoints, refPoints, inCentroid, refCentroid, DataCovariance), ref OptimalRotation);
      }

      return Matrix4x4.TRS(refCentroid, Quaternion.identity, Vector3.one * scaleRatio) *
             Matrix4x4.TRS(Vector3.zero, OptimalRotation, Vector3.one) *
             Matrix4x4.TRS(-inCentroid, Quaternion.identity, Vector3.one);
    }

    //https://animation.rwth-aachen.de/media/papers/2016-MIG-StableRotation.pdf
    //Iteratively apply torque to the basis using Cross products (in place of SVD)
    void extractRotation(Vector3[] A, ref Quaternion q) {
      for (int iter = 0; iter < 9; iter++) {
        q.FillMatrixFromQuaternion(ref QuatBasis);
        Vector3 omega = (Vector3.Cross(QuatBasis[0], A[0]) +
                       Vector3.Cross(QuatBasis[1], A[1]) +
                       Vector3.Cross(QuatBasis[2], A[2])) *
       (1f / Mathf.Abs(Vector3.Dot(QuatBasis[0], A[0]) +
                       Vector3.Dot(QuatBasis[1], A[1]) +
                       Vector3.Dot(QuatBasis[2], A[2]) + 0.000000001f));

        float w = omega.magnitude;
        if (w < 0.000000001f)
          break;
        q = Quaternion.AngleAxis(w * Mathf.Rad2Deg, omega / w) * q;
        q = Quaternion.Lerp(q, q, 0f); //Normalizes the Quaternion; critical for error suppression
      }
    }

    //Calculate Covariance Matrices --------------------------------------------------
    public static Vector3[] TransposeMultSubtract(Vector3[] vec1, Vector4[] vec2, Vector3 vec1Centroid, Vector3 vec2Centroid, Vector3[] covariance) {
      for (int i = 0; i < 3; i++) { //i is the row in this matrix
        covariance[i] = Vector3.zero;
      }

      for (int k = 0; k < vec1.Length; k++) {//k is the column in this matrix
        Vector3 left = (vec1[k] - vec1Centroid) * vec2[k].w;
        Vector3 right = (new Vector3(vec2[k].x, vec2[k].y, vec2[k].z) - vec2Centroid) * Mathf.Abs(vec2[k].w);

        covariance[0][0] += left[0] * right[0];
        covariance[1][0] += left[1] * right[0];
        covariance[2][0] += left[2] * right[0];
        covariance[0][1] += left[0] * right[1];
        covariance[1][1] += left[1] * right[1];
        covariance[2][1] += left[2] * right[1];
        covariance[0][2] += left[0] * right[2];
        covariance[1][2] += left[1] * right[2];
        covariance[2][2] += left[2] * right[2];
      }

      return covariance;
    }
  }

  #endregion

  //private bool _hasPrev;
  //private Matrix4x4 _prevTargetToAnchorMatrix;
  //private Pose _prevA, _prevB;
  //private float _prevDistance;

  //private void updateTRS() {

  //  var A = new Pose(_switchA.Position, _switchA.Rotation);
  //  var B = new Pose(_switchB.Position, _switchB.Rotation);

  //  var translation = Vector3.zero;
  //  if (_hasPrev) {
  //    var centroid = (A.position + B.position) / 2f;
  //    var prevCentroid = (_prevA.position + _prevB.position) / 2f;
  //    translation = centroid - prevCentroid;
  //  }

  //  var rotation = Quaternion.identity;
  //  if (_hasPrev) {
  //    var axis = A.position - B.position;
  //    var perpendicular = Utils.Perpendicular(axis);

  //    var deltaA = A.rotation * Quaternion.Inverse(_prevA.rotation) * perpendicular;
  //    var deltaAngleA = Vector3.SignedAngle(perpendicular, deltaA, axis);

  //    var deltaB = B.rotation * Quaternion.Inverse(_prevB.rotation) * perpendicular;
  //    var deltaAngleB = Vector3.SignedAngle(perpendicular, deltaB, axis);

  //    var totalDeltaTwist = (deltaAngleA + deltaAngleB) * 0.5f;

  //    var deltaRotation = Quaternion.FromToRotation(_prevB.position - _prevA.position,
  //                                                  A.position - B.position);

  //    rotation = Quaternion.AngleAxis(totalDeltaTwist, axis) * deltaRotation;
  //  }

  //  var distance = Vector3.Distance(A.position, B.position);
  //  var scale = 1f;
  //  if (_hasPrev) {
  //    var prevScale = _prevTargetToAnchorMatrix;

  //  }

  //  var deltaMatrix = Matrix4x4.TRS(translation, rotation, Vector3.one * scale);

  //  var targetToAnchorMatrix = target.localToWorldMatrix;

  //  _hasPrev = true;
  //  _prevA = A;
  //  _prevB = B;
  //  _prevDistance = distance;
  //  _prevTargetToAnchorMatrix = targetToAnchorMatrix;
  //}
}

public static class FromMatrixExtension {
  public static Vector3 GetVector3(this Matrix4x4 m) { return m.GetColumn(3); }
  public static Quaternion GetQuaternion(this Matrix4x4 m) {
    if (m.GetColumn(2) == m.GetColumn(1)) { return Quaternion.identity; }
    return Quaternion.LookRotation(m.GetColumn(2), m.GetColumn(1));
  }
  public static void FillMatrixFromQuaternion(this Quaternion q, ref Vector3[] covariance) {
    covariance[0] = q * Vector3.right;
    covariance[1] = q * Vector3.up;
    covariance[2] = q * Vector3.forward;
  }
}
