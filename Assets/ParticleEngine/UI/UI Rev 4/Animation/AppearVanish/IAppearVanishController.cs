

namespace Leap.Unity.Animation {

  public interface IAppearVanishController {

    bool GetVisible();

    void Appear();

    bool GetAppearingOrAppeared();

    void Vanish();

    bool GetVanishingOrVanished();

    void AppearNow();

    void VanishNow();

  }

}
