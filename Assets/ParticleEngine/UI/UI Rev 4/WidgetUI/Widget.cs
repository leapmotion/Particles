using Leap.Unity;
using Leap.Unity.Animation;
using Leap.Unity.Attributes;
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
  public StateSwitchController stateController;

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
  [ImplementsInterface(typeof(IMoveToTarget))]
  [Tooltip("This component handles how the widget moves to its target position "
         + "when the user lets go of it.")]
  private MonoBehaviour _placementMoveToTarget;
  public IMoveToTarget placementMoveToTarget {
    get {
      return _placementMoveToTarget as IMoveToTarget;
    }
  }

  #endregion

  #region Unity Events

  void Start() {
    handle.OnPickedUp          += onHandlePickedUp;
    handle.OnMoved             += onHandleMoved;
    handle.OnPlaced            += onHandlePlaced;
    handle.OnThrown            += onHandleThrown;
    handle.OnPlacedInContainer += onHandlePlacedInContainer;

    placementMoveToTarget.OnReachTarget += onPlacementTargetReached;
  }

  #endregion

  #region Handle Events

  private void onHandlePickedUp() {
    placementMoveToTarget.Cancel();
  }

  private void onHandleMoved() {

  }

  private void onHandlePlaced() {
    stateController.SetState(panelOpenState);

    placementMoveToTarget.MoveToTarget(handle.pose.position,
                                       duration: 0.0f);
  }

  private void onHandleThrown(Vector3 velocity) {
    placementMoveToTarget.MoveToTarget(handle.pose.position,
                                       duration: velocity.magnitude.Map(0f, 3f,
                                                                        0f, 1f));
  }

  private void onHandlePlacedInContainer() {

  }

  #endregion

  #region Placement Events

  private void onPlacementTargetReached() {
    stateController.SetState(panelOpenState);
  }

  #endregion

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

}
