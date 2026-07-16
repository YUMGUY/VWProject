using UnityEngine;
using System.Collections.Generic;

public class FloorManager : MonoBehaviour
{
    public static FloorManager Instance;
    
    // A pool of possible scenes for this floor
    public List<string> availableRoomScenes;

    // This will hold the dynamic data for the current floor's minimap
    public List<RoomData> currentFloorRooms { get; private set; }

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);
    }

    public void GenerateFloor()
    {
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
}
