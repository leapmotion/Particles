using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IAppearVanishController {

  bool GetVisible();

  void Appear();

  bool GetAppearingOrAppeared();

  void Vanish();

  bool GetVanishingOrVanished();

  void AppearNow();

  void VanishNow();

}
