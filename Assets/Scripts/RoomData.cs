[System.Serializable]
public class RoomData
{
    public string sceneName;
    public string roomName;
    public int difficulty;
    public enum Direction { North, East, South, West }
    public Direction direction;
}
