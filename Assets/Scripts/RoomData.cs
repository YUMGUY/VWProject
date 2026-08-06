using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A single room node in the floor tree. Links to other rooms are stored by id
/// (not object references) so the graph never gets serialized recursively through
/// a MonoBehaviour field such as GameManager.currentRoomData.
///
/// The floor is a strict tree: neighborIds holds this room's parent plus its own
/// children only — never a sibling or cross-branch room, even if grid-adjacent.
/// </summary>
[System.Serializable]
public class RoomData
{
    [Header("Identity")]
    public int id = -1;
    public int parentId = -1;              // -1 for the start room
    public string sceneName;
    public string roomName;

    [Header("Layout")]
    public Vector2Int cell;                // grid coordinate; start room = (0,0)
    public int depth;                      // distance in rooms from the start
    public List<int> neighborIds = new List<int>(); // tree links: parent + children only

    [Header("Content")]
    public RoomType roomType;
    public int difficulty;
    public int enemyCount;
}
