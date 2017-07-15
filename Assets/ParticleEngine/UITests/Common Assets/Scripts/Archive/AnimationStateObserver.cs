using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Leap.Unity.Animation {

  public class AnimationStateObserver : MonoBehaviour {

    [Tooltip("The Animator behaviour to observe.")]
    public Animator animator;
    [Tooltip("The layer containing the state to watch for. This is 0 unless you have set "
           + "up custom layers in the AnimatorController.")]
    public int stateLayer = 0;
    [Tooltip("The name of the animation state to watch for.")]
    public string stateName;

    private int _stateShortNameHash;
    private bool _inStateLastFrame;

    public UnityEvent OnEnterState;
    public UnityEvent OnExitState;

    void Reset() {
      animator = GetComponent<Animator>();
      stateName = "";
      stateLayer = 0;
    }

    void Start() {
      _stateShortNameHash = Animator.StringToHash(stateName);
    }

    void Update() {
      bool inState = animator.GetCurrentAnimatorStateInfo(0).shortNameHash == _stateShortNameHash;

      if (inState != _inStateLastFrame) {
        if (inState == true) {
          OnEnterState.Invoke();
        }
        else {
          OnExitState.Invoke();
        }

        _inStateLastFrame = inState;
      }
    }

  }
  
}
