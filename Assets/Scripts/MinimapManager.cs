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
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
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

        // Spawn room buttons based on FloorManager
        foreach (var room in FloorManager.Instance.currentFloorRooms)
        {
            GameObject btnObj = Instantiate(buttonPrefab, container);
            
            // Set Text
            TMP_Text buttonText = btnObj.GetComponentInChildren<TMP_Text>();
            if (buttonText != null) buttonText.text = room.sceneName;

            Vector2 offset = room.direction switch {
                RoomData.Direction.North => new Vector2(0, 100),
                RoomData.Direction.East => new Vector2(100, 0),
                RoomData.Direction.South => new Vector2(0, -100),
                RoomData.Direction.West => new Vector2(-100, 0),
                _ => Vector2.zero
            };
            btnObj.GetComponent<RectTransform>().anchoredPosition = offset;
            btnObj.GetComponent<RoomButton>().Initialize(room, OnRoomSelected);
        }
    }

    private void OnRoomSelected(RoomData data)
    {
        selectedRoom = data;
        Debug.Log($"Room {data.sceneName} highlighted.");
        if (startButton != null) startButton.interactable = true;
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
