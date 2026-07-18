using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class MinimapManager : MonoBehaviour
{
    public static MinimapManager Instance;
    public GameObject minimapCanvas;
    public GameObject buttonPrefab; // Assign your RoomButton prefab
    public Transform container;     // Parent for buttons
    public Button startButton;      // Assign your StartButton
    
    private RoomData selectedRoom;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            // minimapCanvas lives as a separate root object in the scene. Parent it under
            // this persistent manager so DontDestroyOnLoad carries the whole UI with it,
            // instead of needing a separate DontDestroyOnLoad call per root object.
            if (minimapCanvas != null)
            {
                minimapCanvas.transform.SetParent(transform, false);
            }

            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // A newly loaded scene's own MinimapManager is a duplicate of the persisted one.
            // Its canvas is a sibling, not a child, so it won't be cleaned up by Destroy(gameObject) alone.
            if (minimapCanvas != null)
            {
                Destroy(minimapCanvas);
            }
            Destroy(gameObject);
            return;
        }

        if (startButton != null)
        {
            startButton.interactable = false;
            startButton.onClick.AddListener(OnStartClicked);
        }
    }

    public void ToggleMinimap(bool show)
    {
        minimapCanvas.SetActive(show);
        
        // Handle Cursor visibility
        Cursor.visible = show;
        Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;

        if (show) 
        {
            // Dynamically generate the floor if needed or just use current
            if (FloorManager.Instance.currentFloorRooms == null || FloorManager.Instance.currentFloorRooms.Count == 0) 
            {
                FloorManager.Instance.GenerateFloor();
            }
            GenerateMap();
        }
    }

    private void GenerateMap()
    {
        // Clear old buttons, skipping the Start button
        foreach (Transform child in container)
        {
            if (child.gameObject != startButton.gameObject)
            {
                Destroy(child.gameObject);
            }
        }

        RoomData current = GameManager.Instance != null ? GameManager.Instance.currentRoomData : null;
        // Unity can't serialize a real null for an embedded [System.Serializable] class field,
        // so an untraveled GameManager.currentRoomData is a non-null placeholder with no sceneName.
        if (current != null && string.IsNullOrEmpty(current.sceneName)) current = null;

        List<RoomData> neighbors = FloorManager.Instance.GetNeighbors(current);

        // Spawn a button for every known room (Starting Room + its 4 neighbors)
        foreach (var room in FloorManager.Instance.GetAllRooms())
        {
            GameObject btnObj = Instantiate(buttonPrefab, container);

            // Set Text
            TMP_Text buttonText = btnObj.GetComponentInChildren<TMP_Text>();
            if (buttonText != null) buttonText.text = room.sceneName;

            // Button prefab is 160x30, so horizontal neighbors need at least 160px of
            // offset to clear the center button's width without overlapping it.
            Vector2 offset = room == FloorManager.Instance.startingRoomData
                ? Vector2.zero
                : room.direction switch {
                    RoomData.Direction.North => new Vector2(0, 180),
                    RoomData.Direction.East => new Vector2(180, 0),
                    RoomData.Direction.South => new Vector2(0, -180),
                    RoomData.Direction.West => new Vector2(-180, 0),
                    _ => Vector2.zero
                };
            btnObj.GetComponent<RectTransform>().anchoredPosition = offset;

            Button roomBtn = btnObj.GetComponent<Button>();
            bool isCurrent = current == null ? room == FloorManager.Instance.startingRoomData : room == current;
            roomBtn.interactable = !isCurrent && neighbors.Contains(room);

            btnObj.GetComponent<RoomButton>().Initialize(room, OnRoomTravel);
        }
    }

    private void OnRoomTravel(RoomData data)
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager.Instance is NULL! Is the GameManager in the scene?");
            return;
        }

        Debug.Log($"Traveling to: {data.sceneName}");
        GameManager.Instance.LoadRoom(data);
        ToggleMinimap(false);
    }

    public void OnStartClicked()
    {
        Debug.Log("Start button clicked!");
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager.Instance is NULL! Is the GameManager in the scene?");
            return;
        }

        if (selectedRoom != null)
        {
            Debug.Log($"Calling GameManager to load: {selectedRoom.sceneName}");
            GameManager.Instance.LoadRoom(selectedRoom);
            ToggleMinimap(false);
        }
        else
        {
            Debug.LogWarning("No room selected!");
        }
    }
}
