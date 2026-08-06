using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class FloorManager : MonoBehaviour
{
    public static FloorManager Instance;

    [Tooltip("Assign a FloorConfig asset that defines size, depth, branching, seed, scene pool and room types.")]
    public FloorConfig floorConfig;

    // The procedurally generated floor tree for this run.
    public Floor CurrentFloor { get; private set; }

    // The room the player started in (root of the tree).
    public RoomData startingRoomData => CurrentFloor != null ? CurrentFloor.Start : null;

    public bool IsGenerated => CurrentFloor != null && CurrentFloor.nodes.Count > 0;

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);
    }

    /// <summary>
    /// Builds the floor tree using the assigned FloorConfig. The currently active scene
    /// becomes the start room's layout, so this should run while the player is in the
    /// first room (matching the previous lazy-generation behavior).
    /// </summary>
    public void GenerateFloor()
    {
        if (floorConfig == null)
        {
            Debug.LogError("FloorManager: no FloorConfig assigned; cannot generate floor.");
            return;
        }

        string startScene = SceneManager.GetActiveScene().name;
        CurrentFloor = FloorGenerator.Generate(floorConfig, startScene);
    }

    /// <summary>Look up a node by id.</summary>
    public RoomData GetRoom(int id)
    {
        if (CurrentFloor != null && CurrentFloor.nodes.TryGetValue(id, out RoomData room))
            return room;
        return null;
    }

    /// <summary>Every room on the floor (start room + all generated rooms).</summary>
    public List<RoomData> GetAllRooms()
    {
        var all = new List<RoomData>();
        if (CurrentFloor != null) all.AddRange(CurrentFloor.nodes.Values);
        return all;
    }
}
