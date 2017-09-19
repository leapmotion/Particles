using Leap.Unity.Attributes;
using Leap.Unity.Query;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Animation {

  public class ObjectAppearVanishController : MonoBehaviour, IAppearVanishController {

    private List<IAppearVanishController> _appearVanishControllers = new List<IAppearVanishController>();

    // TODO: Remove.
    //[Tooltip("If non-null, this IAppearVanishController will receive Vanish calls when "
    //       + "this object receives Appear calls and Vanish calls when this object "
    //       + "receives Appear calls.")]
    //[SerializeField]
    //[ImplementsInterface(typeof(IAppearVanishController))]
    //private MonoBehaviour _inverseAppearVanishObject;
    //public IAppearVanishController inverseAppearVanishObject {
    //  get { return _inverseAppearVanishObject as IAppearVanishController; }
    //}

    public ReadonlyList<IAppearVanishController> appearVanishControllers {
      get { return _appearVanishControllers; }
    }

    private bool _refreshed = false;

    protected virtual void Start() {
      refreshAppearVanishControllers();
    }

    protected virtual void OnValidate() {
      refreshAppearVanishControllers();
    }

    private void refreshAppearVanishControllers() {
      GetComponents<Component>().Query()
                                .Where(c => c is IAppearVanishController && !(c == this))
                                .Select(c => c as IAppearVanishController)
                                .FillList(_appearVanishControllers);

      _refreshed = true;
    }

    public void Appear() {
      if (!_refreshed) refreshAppearVanishControllers();

      foreach (var appearVanishController in _appearVanishControllers) {
        appearVanishController.Appear();
      }

      //if (inverseAppearVanishObject != null) inverseAppearVanishObject.Vanish();
    }

    public void AppearNow() {
      foreach (var appearVanishController in _appearVanishControllers) {
        appearVanishController.AppearNow();
      }

      //if (inverseAppearVanishObject != null) inverseAppearVanishObject.VanishNow();
    }

    public bool GetAppearingOrAppeared() {
      throw new NotImplementedException(
        "GetAppearingOrAppeared not implemented for ObjectAppearVanishController."
      );
    }

    public bool GetVanishingOrVanished() {
      throw new NotImplementedException(
        "GetVanishingOrVanished not implemented for ObjectAppearVanishController."
      );
    }

    public bool GetVisible() {
      return _appearVanishControllers.Query().Any(a => a.GetVisible());
    }

    public void Vanish() {
      foreach (var appearVanishController in _appearVanishControllers) {
        appearVanishController.Vanish();
      }

      //if (inverseAppearVanishObject != null) inverseAppearVanishObject.Appear();
    }

    public void VanishNow() {
      foreach (var appearVanishController in _appearVanishControllers) {
        appearVanishController.VanishNow();
      }

      //if (inverseAppearVanishObject != null) inverseAppearVanishObject.AppearNow();
    }

  }

}
