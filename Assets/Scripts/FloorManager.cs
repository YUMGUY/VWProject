using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class FloorManager : MonoBehaviour
{
    public static FloorManager Instance;

    // A pool of possible scenes for this floor
    public List<string> availableRoomScenes;

    // This will hold the dynamic data for the current floor's minimap
    public List<RoomData> currentFloorRooms { get; private set; }

    // The room the player started the game in (captured on first GenerateFloor call)
    public RoomData startingRoomData { get; private set; }

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);
    }

    public void GenerateFloor()
    {
        startingRoomData = new RoomData();
        startingRoomData.sceneName = SceneManager.GetActiveScene().name;
        startingRoomData.roomName = "Starting Room";

        currentFloorRooms = new List<RoomData>();

        // Randomly pick rooms from the pool (example: 4 rooms)
        for (int i = 0; i < 4; i++)
        {
            RoomData room = new RoomData();
            room.sceneName = availableRoomScenes[Random.Range(0, availableRoomScenes.Count)];
            room.roomName = "Room " + (i + 1);
            room.difficulty = Random.Range(1, 5);
            room.direction = (RoomData.Direction)i;
            currentFloorRooms.Add(room);
        }
    }

    // All known rooms on this floor: the Starting Room plus its 4 neighbors.
    public List<RoomData> GetAllRooms()
    {
        List<RoomData> all = new List<RoomData> { startingRoomData };
        all.AddRange(currentFloorRooms);
        return all;
    }

    // Rooms reachable from the given room. For now, the Starting Room's neighbors
    // are the 4 generated rooms, and each generated room's only neighbor is the
    // Starting Room (a back-link) — no further neighbor graph exists yet.
    public List<RoomData> GetNeighbors(RoomData current)
    {
        if (current == null || current == startingRoomData)
        {
            return currentFloorRooms;
        }

        return new List<RoomData> { startingRoomData };
    }
}
