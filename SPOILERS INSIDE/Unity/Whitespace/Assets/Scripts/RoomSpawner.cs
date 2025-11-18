using UnityEngine;

public enum RoomDir { Left, Center, Right };

public class RoomSpawner : MonoBehaviour {

    public GameObject EmptyRoomPrefab;
    public Transform RoomContainer;

    private GameObject CurrentRoom;

    void Start() {
        CurrentRoom = Instantiate(EmptyRoomPrefab, RoomContainer);
        CurrentRoom.name = "Room_Current";
        SpawnNeighbors();
    }

    void Update() {

    }

    private void SpawnNeighbors() {
        _ = SpawnLeftRoom();
        _ = SpawnCenterRoom();
        _ = SpawnRightRoom();
    }

    private GameObject SpawnLeftRoom() => SpawnRoom(RoomDir.Left, new(-7f, 0f, 1f), Quaternion.Euler(0f, -90f, 0f));
    private GameObject SpawnCenterRoom() => SpawnRoom(RoomDir.Center, new(0f, 0f, 8f), Quaternion.Euler(0f, 0f, 0f));
    private GameObject SpawnRightRoom() => SpawnRoom(RoomDir.Right, new(7f, 0f, 1f), Quaternion.Euler(0f, 90f, 0f));


    private GameObject SpawnRoom(RoomDir rd, Vector3 offset, Quaternion rot) {
        Debug.Log($"Spawning {rd} room.");
        // convert to world-space position/rotation relative to CurrentRoom
        Vector3 worldPos = CurrentRoom.transform.TransformPoint(offset);
        Quaternion worldRot = CurrentRoom.transform.rotation * rot;

        // instantiate with explicit world position/rotation and parent
        GameObject room = Instantiate(EmptyRoomPrefab, worldPos, worldRot, RoomContainer);
        room.name = "Room_"+rd.ToString();
        return room;
    }

}

