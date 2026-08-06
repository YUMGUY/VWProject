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

    [Tooltip("Pixels between adjacent grid cells on the minimap. Button prefab is 160x30, so keep this above 160.")]
    public float cellSpacing = 200f;

    private RoomData selectedRoom;

    // The layout (buttons + connectors) is built once per generated floor and reused on every
    // subsequent open - only which room is "current"/interactable changes between opens, so
    // there's no need to destroy and re-instantiate everything each time.
    private Floor _builtForFloor;
    private readonly Dictionary<int, Button> _roomButtons = new Dictionary<int, Button>();

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
            if (!FloorManager.Instance.IsGenerated)
            {
                FloorManager.Instance.GenerateFloor();
            }
            GenerateMap();
        }
    }

    private void GenerateMap()
    {
        Floor floor = FloorManager.Instance.CurrentFloor;
        if (floor == null) return; // nothing generated (e.g. no FloorConfig assigned) - nothing to build/refresh

        if (floor != _builtForFloor)
        {
            BuildMap(floor);
            _builtForFloor = floor;
        }
        RefreshMapState();
    }

    // Instantiates every room button and connector line for the given floor. Positions,
    // labels, colors, and click handlers are all fixed once a floor is generated, so this only
    // needs to run once per floor - not on every minimap open.
    private void BuildMap(Floor floor)
    {
        // Clear old buttons/connectors, skipping the Start button
        foreach (Transform child in container)
        {
            if (child.gameObject != startButton.gameObject)
            {
                Destroy(child.gameObject);
            }
        }
        _roomButtons.Clear();

        List<RoomData> rooms = FloorManager.Instance.GetAllRooms();
        if (rooms.Count == 0) return;

        // Auto-fit: scale the grid so the whole tree fits inside the container while keeping
        // the start room (cell 0,0) centered. Without this, deep rooms at cellSpacing get
        // pushed off the panel and appear "missing". cellSpacing acts as the maximum spacing.
        // X and Y are fit INDEPENDENTLY (not a single shared spacing value) so a wide-but-short
        // panel doesn't let the tight axis choke spacing on both axes at once.
        int maxAbsX = 0, maxAbsY = 0;
        foreach (var r in rooms)
        {
            maxAbsX = Mathf.Max(maxAbsX, Mathf.Abs(r.cell.x));
            maxAbsY = Mathf.Max(maxAbsY, Mathf.Abs(r.cell.y));
        }

        // Padding is sized off the actual button prefab, not a flat guess - the prefab is
        // 160x30, so a flat padding big enough for its width was wasting most of the panel's
        // vertical room.
        RectTransform buttonRect = buttonPrefab != null ? buttonPrefab.GetComponent<RectTransform>() : null;
        Vector2 buttonSize = buttonRect != null ? buttonRect.sizeDelta : new Vector2(160f, 30f);
        const float margin = 20f;
        float paddingX = buttonSize.x * 0.5f + margin;
        float paddingY = buttonSize.y * 0.5f + margin;

        RectTransform containerRect = container as RectTransform;
        // The container was just enabled; force a layout pass so rect.width/height are current
        // rather than stale (which would make the auto-fit fall back to cellSpacing).
        Canvas.ForceUpdateCanvases();
        float halfW = containerRect != null ? containerRect.rect.width * 0.5f - paddingX : 0f;
        float halfH = containerRect != null ? containerRect.rect.height * 0.5f - paddingY : 0f;

        float spacingX = cellSpacing;
        float spacingY = cellSpacing;
        if (maxAbsX > 0 && halfW > 0) spacingX = Mathf.Min(spacingX, halfW / maxAbsX);
        if (maxAbsY > 0 && halfH > 0) spacingY = Mathf.Min(spacingY, halfH / maxAbsY);
        if (spacingX <= 0f) spacingX = cellSpacing;
        if (spacingY <= 0f) spacingY = cellSpacing;

        // Draw parent->child connector lines first so buttons render on top of them.
        foreach (var room in rooms)
        {
            foreach (int nId in room.neighborIds)
            {
                RoomData child = FloorManager.Instance.GetRoom(nId);
                if (child == null || child.parentId != room.id) continue; // each edge once
                CreateConnector(CellToPos(room.cell, spacingX, spacingY), CellToPos(child.cell, spacingX, spacingY));
            }
        }

        // Spawn a button for every known room on the floor.
        foreach (var room in rooms)
        {
            GameObject btnObj = Instantiate(buttonPrefab, container);

            // Label: room type if assigned, otherwise the scene layout name.
            TMP_Text buttonText = btnObj.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
                buttonText.text = room.roomType != null ? room.roomType.typeName : room.sceneName;

            btnObj.GetComponent<RectTransform>().anchoredPosition = CellToPos(room.cell, spacingX, spacingY);

            Button roomBtn = btnObj.GetComponent<Button>();
            Color? tint = room.roomType != null ? room.roomType.minimapColor : (Color?)null;
            btnObj.GetComponent<RoomButton>().Initialize(room, OnRoomTravel, tint);

            _roomButtons[room.id] = roomBtn;
        }
    }

    // Updates which button is interactable based on the current room - the only thing that
    // actually changes between minimap opens once a floor's layout is already built.
    private void RefreshMapState()
    {
        RoomData current = GameManager.Instance != null ? GameManager.Instance.currentRoomData : null;
        // Unity can't serialize a real null for an embedded [System.Serializable] class field,
        // so an untraveled GameManager.currentRoomData is a non-null placeholder with no sceneName.
        // In that case the player is still in the start room.
        if (current == null || string.IsNullOrEmpty(current.sceneName) || current.id < 0)
            current = FloorManager.Instance.startingRoomData;

        // Ids of rooms directly connected to the current room (its parent + children).
        HashSet<int> neighborIds = new HashSet<int>();
        if (current != null)
            foreach (int id in current.neighborIds) neighborIds.Add(id);

        foreach (var kvp in _roomButtons)
        {
            int roomId = kvp.Key;
            Button roomBtn = kvp.Value;
            bool isCurrent = current != null && roomId == current.id;
            roomBtn.interactable = !isCurrent && neighborIds.Contains(roomId);

            // Setting `interactable` re-triggers Unity's Button ColorTint transition, which
            // overwrites Image.color with the ColorBlock's normal/disabled color - repaint the
            // room-type tint afterward so it survives repeated opens, not just the first one.
            RoomData room = FloorManager.Instance.GetRoom(roomId);
            if (room?.roomType != null)
            {
                Image img = roomBtn.GetComponent<Image>();
                if (img != null) img.color = room.roomType.minimapColor;
            }
        }
    }

    // Grid cell -> anchored position on the minimap (start cell 0,0 stays centered).
    private static Vector2 CellToPos(Vector2Int cell, float spacingX, float spacingY)
    {
        return new Vector2(cell.x * spacingX, cell.y * spacingY);
    }

    // Creates a thin line connecting two points, parented under the container behind buttons.
    private void CreateConnector(Vector2 a, Vector2 b)
    {
        var line = new GameObject("MapLink", typeof(RectTransform), typeof(Image));
        var rt = line.GetComponent<RectTransform>();
        rt.SetParent(container, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);

        Vector2 delta = b - a;
        rt.sizeDelta = new Vector2(delta.magnitude, 4f);
        rt.anchoredPosition = (a + b) * 0.5f;
        rt.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

        var img = line.GetComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.35f);
        img.raycastTarget = false;
        rt.SetAsFirstSibling(); // stay behind the room buttons
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
