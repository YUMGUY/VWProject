using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public GameObject player;
    public GameObject cameraRig;
    public GameObject mainCamera;
    public RoomData currentRoomData; // Added this to store data

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            DontDestroyOnLoad(player);
            if (cameraRig != null) DontDestroyOnLoad(cameraRig);
            if (mainCamera != null) DontDestroyOnLoad(mainCamera);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadRoom(RoomData data)
    {
        currentRoomData = data; // Update current data
        StartCoroutine(LoadRoomRoutine(data));
    }

    private IEnumerator LoadRoomRoutine(RoomData data)
    {
        Debug.Log($"Loading scene: {data.sceneName}");

        AsyncOperation op = SceneManager.LoadSceneAsync(data.sceneName, LoadSceneMode.Single);
        if (op == null)
        {
            Debug.LogError($"Failed to initiate load for scene: {data.sceneName}");
            yield break;
        }

        yield return op;

        Debug.Log($"Successfully loaded: {data.sceneName}");

        // Align position
        GameObject spawnPoint = GameObject.FindGameObjectWithTag("SpawnPoint");
        if (spawnPoint != null)
        {
            player.transform.position = spawnPoint.transform.position;
            player.transform.rotation = spawnPoint.transform.rotation;
        }
        else
        {
            Debug.LogWarning("No SpawnPoint found in the new scene!");
        }
    }
}
