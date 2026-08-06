using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Designer-facing knobs for procedurally generating one floor. Assign a FloorConfig
/// asset to FloorManager; tune everything from the Inspector.
/// Create assets via Assets > Create > Roguelike > Floor Config.
/// </summary>
[CreateAssetMenu(fileName = "FloorConfig", menuName = "Roguelike/Floor Config")]
public class FloorConfig : ScriptableObject
{
    [Header("Size")]
    [Tooltip("Upper bound on total rooms (including the start room). Branch isolation may fill fewer.")]
    public int maxRooms = 12;

    [Tooltip("Max distance (in rooms) from the start room. Depth 1 = start + its immediate neighbors only.")]
    public int maxDepth = 4;

    [Header("Branching (children per room)")]
    [Tooltip("Cardinal neighbors the START room spawns (1-4). Independent of the per-room branching below, so the hub keeps its classic N/E/S/W ring. Default 4.")]
    public int startRoomExits = 4;

    [Tooltip("Minimum children a non-leaf room attempts to spawn.")]
    public int minNeighborsPerRoom = 1;

    [Tooltip("Maximum children a room can spawn. A room has at most 4 (N/E/S/W).")]
    public int maxNeighborsPerRoom = 3;

    [Header("Randomness")]
    [Tooltip("0 = new random layout every run. Any non-zero value = reproducible layout for that seed.")]
    public int seed = 0;

    [Header("Room Scene Pool")]
    [Tooltip("Interchangeable room-layout scenes picked at random for each generated node (e.g. Room1, Room2).")]
    public List<string> availableRoomScenes = new List<string>();

    [Header("Room Types")]
    [Tooltip("All room types available on this floor. Guaranteed/dead-end types are placed first, then the rest fill in by weight.")]
    public List<RoomType> roomTypes = new List<RoomType>();

    [Tooltip("Type assigned to the start room (typically difficulty 0, no enemies).")]
    public RoomType startType;

    [Tooltip("Fallback type used for a normal node when no weighted type can be chosen.")]
    public RoomType defaultCombatType;
}
