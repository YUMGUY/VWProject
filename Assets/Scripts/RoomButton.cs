using UnityEngine;
using UnityEngine.UI;

public class RoomButton : MonoBehaviour
{
    private RoomData _roomData;
    private Button _button;

    public void Initialize(RoomData data, System.Action<RoomData> onClickAction)
    {
        _roomData = data;
        _button = GetComponent<Button>();
        _button.onClick.AddListener(() => 
        {
            Debug.Log($"Room Selected: {_roomData.roomName} (Scene: {_roomData.sceneName})");
            onClickAction(_roomData);
        });
    }
}
