using UnityEngine;

public class RoomGenerator : MonoBehaviour
{
    public GameObject cubePrefab;
    public Transform spawnParent;

    private void Start()
    {
        if (GameManager.Instance == null || GameManager.Instance.currentRoomData == null) return;

        RoomData room = GameManager.Instance.currentRoomData;

        // Enemy count is decided at generation time from the room's type.
        // TODO: swap cubePrefab for real enemy prefabs chosen from room.roomType,
        // and scale their stats by room.difficulty.
        int count = room.enemyCount;
        for (int i = 0; i < count; i++)
        {
            Vector3 randomPos = new Vector3(Random.Range(-5f, 5f), 1f, Random.Range(-5f, 5f));
            Instantiate(cubePrefab, randomPos, Quaternion.identity, spawnParent);
        }
    }
}
