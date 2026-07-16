using UnityEngine;

public class RoomGenerator : MonoBehaviour
{
    public GameObject cubePrefab;
    public Transform spawnParent;

    private void Start()
    {
        if (GameManager.Instance == null || GameManager.Instance.currentRoomData == null) return;
        
        // Calculate count based on difficulty from the ScriptableObject
        int count = GameManager.Instance.currentRoomData.difficulty * 5; 
        for (int i = 0; i < count; i++)
        {
            Vector3 randomPos = new Vector3(Random.Range(-5f, 5f), 1f, Random.Range(-5f, 5f));
            Instantiate(cubePrefab, randomPos, Quaternion.identity, spawnParent);
        }
    }
}
