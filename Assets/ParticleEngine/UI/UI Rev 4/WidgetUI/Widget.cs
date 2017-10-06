using Leap.Unity;
using Leap.Unity.Animation;
using Leap.Unity.Attributes;
using Leap.Unity.Layout;
using Leap.Unity.PhysicalInterfaces;
using UnityEngine;

public class Widget : MonoBehaviour {

  #region Inspector

  [Header("Widget Handle")]

  [SerializeField]
  [ImplementsInterface(typeof(IHandle))]
  [Tooltip("This is what the user grabs and places to open a panel.")]
  private MonoBehaviour _handle;
  public IHandle handle {
    get {
      return _handle as IHandle;
    }
  }

  [Header("State Changes")]

  [Tooltip("This controller opens and closes the panel.")]
  public ZZOLD_SwitchStateController stateController;

  [Tooltip("Switch to this state when the widget panel should be closed.")]
  public string panelClosedState = "Ball";
  [Tooltip("Switch to this state when the widget panel should be open.")]
  public string panelOpenState   = "Panel";

  // TODO: Arrgh this is not general...
  public Transform currentlyFollowing = null;
  public Transform ballTransform;
  public Transform panelTransform;

  [Header("Widget Placement")]

  [SerializeField]
  [ImplementsInterface(typeof(IPoseProvider))]
  [Tooltip("This component handles where the widget determines its target pose when "
         + "placed or thrown by the user.")]
  private MonoBehaviour _targetPoseProvider;
  public IPoseProvider targetPoseProvider {
    get { return _targetPoseProvider as IPoseProvider; }
  }

  [SerializeField]
  [ImplementsInterface(typeof(IMoveToPose))]
  [Tooltip("This component handles how the widget moves to its target pose when placed or "
         + "thrown by the user.")]
  private MonoBehaviour _movementToPose;
  public IMoveToPose movementToPose {
    get { return _movementToPose as IMoveToPose; }
  }

  #endregion

  #region Unity Events

  void Start() {
    handle.OnPickedUp          += onHandlePickedUp;
    handle.OnMoved             += onHandleMoved;
    handle.OnPlaced            += onHandlePlaced;
    handle.OnThrown            += onHandleThrown;
    handle.OnPlacedInContainer += onHandlePlacedInContainer;

    movementToPose.OnMovementUpdate += onMovementUpdate;
    movementToPose.OnReachTarget += onPlacementTargetReached;
  }

  #endregion

  #region Handle Events

  private void onHandlePickedUp() {
    movementToPose.Cancel();
  }

  private void onHandleMoved() {
    // (no logic)
  }

  private void onHandlePlaced() {
    initiateMovementToTarget(duration: 0.0f);
  }

  private void onHandleThrown(Vector3 velocity) {
    initiateMovementToTarget(duration: velocity.magnitude.Map(0f, 3f, 0f, 1f));
  }

  private void onHandlePlacedInContainer() {
    // (no logic)
  }

  #endregion

  private void initiateMovementToTarget(float duration) {
    var targetPose = targetPoseProvider.GetTargetPose();

    movementToPose.MoveToTarget(targetPose, duration);
  }

  private void onMovementUpdate() {
    movementToPose.targetPose = new Pose() {
      position = movementToPose.targetPose.position,
      rotation = targetPoseProvider.GetTargetPose().rotation
    };
  }

  private void onPlacementTargetReached() {
    stateController.SetState(panelOpenState);
  }

  #region Move To Child // TODO: Needs generalization

  public void MoveToBall() {
    MoveTo(ballTransform);
  }

  public void MoveToPanel() {
    MoveTo(panelTransform);
  }

  public void MoveTo(Transform t) {
    Pose followingPose = t.ToWorldPose();

    this.transform.SetWorldPose(followingPose);

    t.SetWorldPose(followingPose);
  }

  #endregion

}
