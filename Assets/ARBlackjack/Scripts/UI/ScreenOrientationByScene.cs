using UnityEngine;
using UnityEngine.SceneManagement;
using ARBlackjack.Network;

namespace ARBlackjack.UI
{

    public static class ScreenOrientationByScene
    {
        private const ScreenOrientation MenuOrientation = ScreenOrientation.Portrait;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            SceneManager.sceneLoaded += (scene, _) => Apply(scene.name);
        }

        private static void Apply(string sceneName)
        {
            if (sceneName == NetworkConfig.GameScene)
            {
                Screen.autorotateToPortrait = true;
                Screen.autorotateToPortraitUpsideDown = true;
                Screen.autorotateToLandscapeLeft = true;
                Screen.autorotateToLandscapeRight = true;
                Screen.orientation = ScreenOrientation.AutoRotation;
            }
            else
            {
                Screen.orientation = MenuOrientation;
            }
        }
    }
}
