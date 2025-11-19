using Unity.VisualScripting;
using UnityEngine;

public enum RoomName {Current, Left, Center, Right };

public class RoomSpawner : MonoBehaviour {

    public GameObject EmptyRoomPrefab;
    public Transform RoomContainer;

    private GameObject CurrentRoom;

    void Awake() {
        ResetRooms();
    }

    public void ResetRooms() {
        foreach (Transform room in RoomContainer) room.name = "Old";
        DestroyOldRooms();
        CurrentRoom = Instantiate(EmptyRoomPrefab, RoomContainer);
        CurrentRoom.name = "Room_Current";
        SpawnNeighbors();
    }

    public void BeginChangeRooms(Transform destRoom) {
        foreach (Transform room in RoomContainer) room.name = "Old";
        CurrentRoom = destRoom.gameObject;
        CurrentRoom.name = "Room_Current";
        SpawnNeighbors();
    }

    private void SpawnNeighbors() {
        _ = SpawnLeftRoom();
        _ = SpawnCenterRoom();
        _ = SpawnRightRoom();
    }

    public void DestroyOldRooms() {
        foreach (Transform t in RoomContainer.transform) {
            if (t.name.StartsWith("Old")) Destroy(t.gameObject);
        }
    }

    private GameObject SpawnLeftRoom() => SpawnRoom(RoomName.Left, new(-7f, 0f, 1f), Quaternion.Euler(0f, -90f, 0f));
    private GameObject SpawnCenterRoom() => SpawnRoom(RoomName.Center, new(0f, 0f, 8f), Quaternion.Euler(0f, 0f, 0f));
    private GameObject SpawnRightRoom() => SpawnRoom(RoomName.Right, new(7f, 0f, 1f), Quaternion.Euler(0f, 90f, 0f));


    private GameObject SpawnRoom(RoomName rd, Vector3 offset, Quaternion rot) {
        // convert to world-space position/rotation relative to CurrentRoom
        Vector3 worldPos = CurrentRoom.transform.TransformPoint(offset);
        Quaternion worldRot = CurrentRoom.transform.rotation * rot;

        // instantiate with explicit world position/rotation and parent
        GameObject room = Instantiate(EmptyRoomPrefab, worldPos, worldRot, RoomContainer);
        room.name = "Room_"+rd.ToString();
        return room;
    }

}

