using Leap.Unity;
using Leap.Unity.Attributes;
using Leap.Unity.RuntimeGizmos;
using UnityEngine;

public class LeapTRS2 : MonoBehaviour, IRuntimeGizmoComponent {

  #region Inspector

  [Header("Transform To Manipulate")]

  public Transform objectTransform;

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

  [Header("Debug Runtime Gizmos")]

  [SerializeField]
  private bool _drawDebug = false;

  [Header("Momentum (when not pinching)")]

  [SerializeField]
  private bool _allowMomentum = false;

  [SerializeField]
  private float      _linearFriction = 1f;

  [SerializeField]
  private float      _angularFriction = 1f;

  [SerializeField]
  private float      _scaleFriction = 1f;

  [SerializeField, Disable]
  private Vector3    _positionMomentum;

  [SerializeField, Disable]
  private Vector3    _rotationMomentum;

  [SerializeField, Disable, MinValue(0.001f)]
  private float      _scaleMomentum = 1f;

  [SerializeField]
  private Vector3   _lastKnownCentroid = Vector3.zero;

  #endregion

  #region Unity Events

  void Update() {
    updateTRS();
  }

  #endregion

  #region TRS

  private RingBuffer<Pose> _aPoses = new RingBuffer<Pose>(2);
  private RingBuffer<Pose> _bPoses = new RingBuffer<Pose>(2);

  private void updateTRS() {
    var aGrasped = _switchA != null && _switchA.grasped;
    var bGrasped = _switchB != null && _switchB.grasped;

    int numGrasping = (aGrasped? 1 : 0) + (bGrasped ? 1 : 0);

    if (!aGrasped) {
      _aPoses.Clear();
    }
    else {
      _aPoses.Add(_switchA.pose);
    }

    if (!bGrasped) {
      _bPoses.Clear();
    }
    else {
      _bPoses.Add(_switchB.pose);
    }

    // Declare information for applying the TRS.
    var objectScale = objectTransform.localScale.x;
    var origCentroid = Vector3.zero;
    var nextCentroid = Vector3.zero;
    var origAxis = Vector3.zero;
    var nextAxis = Vector3.zero;
    var twist = 0f;
    var applyPositionalMomentum = false;
    var applyRotateScaleMomentum = false;

    // Fill information based on the number of elements in the TRS.
    if (numGrasping == 0) {
      applyPositionalMomentum  = true;
      applyRotateScaleMomentum = true;
    }
    else if (numGrasping == 1) {

      var poses = aGrasped ? _aPoses : (bGrasped ? _bPoses : null);

      if (poses != null && poses.IsFull) {

        // Translation.
        origCentroid = poses[0].position;
        nextCentroid = poses[1].position;

      }

      applyRotateScaleMomentum = true;

      _lastKnownCentroid = nextCentroid;
    }
    else {

      if (_aPoses.IsFull && _bPoses.IsFull) {

        // Scale changes.
        float dist0 = Vector3.Distance(_aPoses[0].position, _bPoses[0].position);
        float dist1 = Vector3.Distance(_aPoses[1].position, _bPoses[1].position);

        float scaleChange = dist1 / dist0;

        objectScale *= scaleChange;

        // Translation.
        origCentroid = (_aPoses[0].position + _bPoses[0].position) / 2f;
        nextCentroid = (_aPoses[1].position + _bPoses[1].position) / 2f;

        // Axis rotation.
        origAxis = (_bPoses[0].position - _aPoses[0].position);
        nextAxis = (_bPoses[1].position - _aPoses[1].position);

        // Twist.
        var perp = Utils.Perpendicular(nextAxis);
        
        var aRotatedPerp = perp.Rotate(_aPoses[1].rotation.From(_aPoses[0].rotation));
        //aRotatedPerp = (_aPoses[1].rotation * Quaternion.Inverse(_aPoses[0].rotation))
        //               * perp;
        var aTwist = Vector3.SignedAngle(perp, aRotatedPerp, nextAxis);

        var bRotatedPerp = perp.Rotate(_bPoses[1].rotation.From(_bPoses[0].rotation));
        //bRotatedPerp = (_bPoses[1].rotation * Quaternion.Inverse(_bPoses[0].rotation))
        //               * perp;
        var bTwist = Vector3.SignedAngle(perp, bRotatedPerp, nextAxis);

        twist = (aTwist + bTwist) * 0.5f;

        _lastKnownCentroid = nextCentroid;
      }
    }


    // Calculate TRS.
    Vector3    origTargetPos = objectTransform.transform.position;
    Quaternion origTargetRot = objectTransform.transform.rotation;
    float      origTargetScale = objectTransform.transform.localScale.x;

    // Declare delta properties.
    Vector3    finalPosDelta;
    Quaternion finalRotDelta;
    float      finalScaleRatio;

    // Translation.

    // Apply momentum, or just apply the transformations and record momentum.
    finalPosDelta = (nextCentroid - origCentroid);

    if (_allowMomentum && applyPositionalMomentum) {
      // Apply (and decay) momentum only.
      objectTransform.position += _positionMomentum;

      _positionMomentum = Vector3.Lerp(_positionMomentum, Vector3.zero, _linearFriction * Time.deltaTime);
    }
    else {
      // Apply transformation.
      objectTransform.position = objectTransform.position.Then(finalPosDelta);

      // Measure momentum only.
      _positionMomentum = Vector3.Lerp(_positionMomentum, finalPosDelta, 20f * Time.deltaTime);
    }

    // Remember last known centroid as pivot; remember local offset, scale, rotation,
    // then correct.
    var centroid = _lastKnownCentroid;
    var centroid_local = objectTransform.worldToLocalMatrix.MultiplyPoint3x4(centroid);
    
    // Scale.
    finalScaleRatio = objectScale / objectTransform.localScale.x;

    // Rotation.
    var axis = nextAxis;
    var poleRotation = Quaternion.FromToRotation(origAxis, nextAxis);
    var poleTwist = Quaternion.AngleAxis(twist, nextAxis);
    finalRotDelta = objectTransform.rotation
                                   .Then(poleRotation)
                                   .Then(poleTwist)
                                   .From(objectTransform.rotation);

    // Apply scale and rotation, or use momentum for these properties.
    if (_allowMomentum && applyRotateScaleMomentum) {
      // Apply (and decay) momentum only.
      objectTransform.rotation = objectTransform.rotation.Then(
                                   Quaternion.AngleAxis(_rotationMomentum.magnitude,
                                                        _rotationMomentum.normalized));
      objectTransform.localScale *= _scaleMomentum;

      // Apply scale constraints.
      if (objectTransform.localScale.x < _minScale) {
        objectTransform.localScale = _minScale * Vector3.one;
        _scaleMomentum = 1f;
      }
      else if (objectTransform.localScale.x > _maxScale) {
        objectTransform.localScale = _maxScale * Vector3.one;
        _scaleMomentum = 1f;
      }

      _rotationMomentum = Vector3.Lerp(_rotationMomentum, Vector3.zero, _angularFriction * Time.deltaTime);
      _scaleMomentum = Mathf.Lerp(_scaleMomentum, 1f, _scaleFriction * Time.deltaTime);
    }
    else {
      // Apply transformations.
      objectTransform.rotation = objectTransform.rotation.Then(finalRotDelta);
      objectTransform.localScale = Vector3.one * (objectTransform.localScale.x
                                                  * finalScaleRatio);

      // Measure momentum only.
      _rotationMomentum = Vector3.Lerp(_rotationMomentum, finalRotDelta.ToAngleAxisVector(), 40f * Time.deltaTime);
      _scaleMomentum = Mathf.Lerp(_scaleMomentum, finalScaleRatio, 20f * Time.deltaTime);
    }

    // Restore centroid pivot.
    var movedCentroid = objectTransform.localToWorldMatrix.MultiplyPoint3x4(centroid_local);
    objectTransform.position += (centroid - movedCentroid);
  }

  public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
    if (!_drawDebug) return;

    if (objectTransform == null) return;

    drawer.PushMatrix();
    drawer.matrix = objectTransform.localToWorldMatrix;

    drawer.color = LeapColor.coral;
    drawer.DrawWireCube(Vector3.zero, Vector3.one * 0.20f);

    drawer.color = LeapColor.jade;
    drawer.DrawWireCube(Vector3.zero, Vector3.one * 0.10f);

    drawer.PopMatrix();
  }

  #endregion

  // TODO: Put this somewhere else -- Kabsch with scaling is useful, but it's unused here!!
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
}

// TODO: Part of KabschSolver implementation that includes scaling
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
