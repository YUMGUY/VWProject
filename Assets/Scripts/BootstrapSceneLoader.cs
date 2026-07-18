using UnityEngine;
using UnityEngine.SceneManagement;

public class BootstrapSceneLoader : MonoBehaviour
{
    public string startingRoomSceneName = "Playground";

    private void Start()
    {
        // Runs after every Awake() in the Bootstrap scene has finished, so the
        // persistent objects (Player, Camera, GameManager, etc.) are already
        // marked DontDestroyOnLoad before this Single-mode load unloads Bootstrap.
        SceneManager.LoadScene(startingRoomSceneName, LoadSceneMode.Single);
    }
}
