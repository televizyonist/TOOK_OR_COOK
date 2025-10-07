using UnityEngine;

namespace PnceHarekat
{
    /// <summary>
    /// Ensures that the runtime-only scene has the gameplay systems initialised
    /// even when the scene file inside the repository is nearly empty.
    /// </summary>
    public static class GameBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Object.FindObjectOfType<GameController>() != null)
            {
                return;
            }

            var go = new GameObject("GameController");
            go.AddComponent<GameController>();
        }
    }
}
