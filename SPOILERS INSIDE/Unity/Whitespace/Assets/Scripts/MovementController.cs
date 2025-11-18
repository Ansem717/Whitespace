using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementController : MonoBehaviour {

    public Vector3 LocalOffset;
    [Tooltip("Movement speed along the path (units/sec).")]
    public float moveSpeed = 4f;

    private bool LeftInput() => Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow);
    private bool ForwardInput() => Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
    private bool RightInput() => Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow);

    //TODO:
    //Movement doesn't work because the rooms need to be repurposed for the new dynamic system
    //And RoomSpawner.cs needs an update loop.

    public GameObject startRoom;
    public GameObject leftRoom;
    public GameObject forwardRoom;
    public GameObject rightRoom;

    //persistance
    private GameObject target = null;
    private Coroutine transitionCoroutine = null; // movement coroutine
    private Coroutine rotationCoroutine = null;   // rotation coroutine
    private GameObject currentRoom = null;

    void Start() => currentRoom = startRoom;

    void Update() {
        if (Input.GetKeyDown(KeyCode.R)) {
            Debug.Log("Resetting");
            ResetToStart();
        } else if (target == null) {
            string chosenWaypoint = "none";
            if (LeftInput()) {
                chosenWaypoint = "Left";
                Debug.Log("moving left!");
                target = leftRoom;
            } else if (ForwardInput()) {
                chosenWaypoint = "Center";
                Debug.Log("moving forward!");
                target = forwardRoom;
            } else if (RightInput()) {
                chosenWaypoint = "Right";
                Debug.Log("moving right!");
                target = rightRoom;
            }

            if (target != null) {
                // get the entry/exit transforms under each room's Waypoints container
                Transform entry = GetWaypointTransform(currentRoom, chosenWaypoint);
                Transform exit = GetWaypointTransform(target, "End");

                // If either waypoint is missing, fall back to center of room (no clipping guarantee)
                if (entry == null) entry = currentRoom != null ? currentRoom.transform : this.transform;
                if (exit == null) exit = target != null ? target.transform : this.transform;

                // If a transition is already running, stop it (we replace with the new one).
                if (transitionCoroutine != null) {
                    StopCoroutine(transitionCoroutine);
                    transitionCoroutine = null;
                }
                if (rotationCoroutine != null) {
                    StopCoroutine(rotationCoroutine);
                    rotationCoroutine = null;
                }

                // start movement and rotation coroutines in parallel
                transitionCoroutine = StartCoroutine(MoveAlongPath(entry, exit, target));
                // rotation coroutine will be started from MoveAlongPath with correct timing
            }
        }
    }

    // Find a waypoint child under room: "Waypoints/<name>"
    private Transform GetWaypointTransform(GameObject room, string name) {
        if (room == null) return null;
        Transform waypoints = room.transform.Find("Waypoints");
        if (waypoints == null) return null;
        Transform t = waypoints.Find(name);
        return t;
    }

    private IEnumerator MoveAlongPath(Transform entry, Transform exit, GameObject destRoom) {
        // Build path points
        Vector3 startPos = transform.position;
        Vector3 entryPos = entry.position;
        Vector3 exitPos = exit.position;
        Quaternion yawOnly = Quaternion.Euler(0f, destRoom.transform.eulerAngles.y, 0f);
        Vector3 finalPos = destRoom.transform.position + yawOnly * LocalOffset;

        Vector3[] points = new Vector3[] { startPos, entryPos, exitPos, finalPos };

        // Precompute segment lengths and total length
        int segCount = points.Length - 1;
        float[] segLen = new float[segCount];
        float totalLen = 0f;
        for (int i = 0; i < segCount; ++i) {
            segLen[i] = Vector3.Distance(points[i], points[i + 1]);
            totalLen += segLen[i];
        }

        // If there's nowhere to go, snap and finish
        if (totalLen <= Mathf.Epsilon) {
            SnapToFinal(destRoom, finalPos, destRoom.transform.eulerAngles.y);
            yield break;
        }

        // Compute times for rotation interpolation:
        // totalTime = totalLen / moveSpeed
        // entryTime = distance(start->entry) / moveSpeed
        float totalTime = totalLen / Mathf.Max(0.0001f, moveSpeed);
        float entryDist = segLen.Length > 0 ? segLen[0] : 0f;
        float entryTime = entryDist / Mathf.Max(0.0001f, moveSpeed);

        // prepare yaw values
        float startYaw = transform.eulerAngles.y;
        // IMPORTANT: Waypoints are NOT rotated. Use the ENDPOINT yaw (exit) for rotating toward the door.
        // This ensures the camera rotates to face into the next room by the time it reaches the ENTRY waypoint.
        float entryYaw = exit.eulerAngles.y; // <-- changed: use exit (End) yaw, not entry yaw
        float finalYaw = destRoom.transform.eulerAngles.y;

        // Start rotation coroutine that interpolates yaw over the timeline.
        if (rotationCoroutine != null) {
            StopCoroutine(rotationCoroutine);
            rotationCoroutine = null;
        }
        rotationCoroutine = StartCoroutine(RotateOverTimeline(startYaw, entryYaw, finalYaw, entryTime, totalTime));
        
        float traveled = 0f;

        // Movement loop: move at constant speed along polyline
        while (traveled < totalLen) {
            traveled += moveSpeed * Time.deltaTime;
            if (traveled > totalLen) traveled = totalLen;

            // find current segment index
            float acc = 0f;
            int segIndex = 0;
            while (segIndex < segCount && acc + segLen[segIndex] < traveled) {
                acc += segLen[segIndex];
                segIndex++;
            }

            // Local t on the active segment
            float localT = segLen[segIndex] > Mathf.Epsilon ? (traveled - acc) / segLen[segIndex] : 1f;

            // Position interpolation
            Vector3 pos = Vector3.Lerp(points[segIndex], points[segIndex + 1], localT);
            transform.position = pos;

            yield return null;
        }

        // finalize exact placement and cleanup
        transform.SetPositionAndRotation(finalPos, Quaternion.Euler(-3f, finalYaw, 0f));
        currentRoom = destRoom;
        target = null;

        // stop movement coroutine reference
        transitionCoroutine = null;
    }

    // Rotation coroutine: interpolate yaw over the movement timeline.
    // - from t=0..entryTime: startYaw -> entryYaw (entryYaw is taken from END waypoint now)
    // - from t=entryTime..totalTime: entryYaw -> finalYaw
    private IEnumerator RotateOverTimeline(float startYaw, float entryYaw, float finalYaw, float entryTime, float totalTime) {
        float elapsed = 0f;

        // Edge cases: if totalTime is zero (or nearly), snap immediately
        if (totalTime <= Mathf.Epsilon) {
            transform.rotation = Quaternion.Euler(-3f, finalYaw, 0f);
            rotationCoroutine = null;
            yield break;
        }

        while (elapsed < totalTime) {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / totalTime);

            float yaw;
            if (entryTime <= 0f) {
                // no entry segment time: interpolate straight from start -> final over totalTime
                yaw = Mathf.LerpAngle(startYaw, finalYaw, t);
            } else if (elapsed <= entryTime) {
                // phase 1: start -> entry (we rotate toward the END yaw during this phase)
                float tEntry = Mathf.Clamp01(elapsed / entryTime);
                yaw = Mathf.LerpAngle(startYaw, entryYaw, tEntry);
            } else {
                // phase 2: entry -> final
                float tAfterEntry = Mathf.Clamp01((elapsed - entryTime) / Mathf.Max(0.0001f, (totalTime - entryTime)));
                yaw = Mathf.LerpAngle(entryYaw, finalYaw, tAfterEntry);
            }

            transform.rotation = Quaternion.Euler(-3f, yaw, 0f);
            yield return null;
        }

        // Ensure final yaw exactly matches finalYaw
        transform.rotation = Quaternion.Euler(-3f, finalYaw, 0f);
        rotationCoroutine = null;
    }

    private void SnapToFinal(GameObject destRoom, Vector3 finalPos, float finalYaw) {
        transform.SetPositionAndRotation(finalPos, Quaternion.Euler(-3f, finalYaw, 0f));
        currentRoom = destRoom;
        target = null;
        transitionCoroutine = null;
        rotationCoroutine = null;
    }

    private void ResetToStart() {
        // Cancel any running transition/rotation
        if (transitionCoroutine != null) {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }
        if (rotationCoroutine != null) {
            StopCoroutine(rotationCoroutine);
            rotationCoroutine = null;
        }

        // Clear target so Update won't resume transitioning
        target = null;

        // Set current room to startRoom
        currentRoom = startRoom;

        // Snap camera to start position/rotation (no smoothing)
        if (currentRoom != null) {
            Quaternion yawOnly = Quaternion.Euler(0f, currentRoom.transform.eulerAngles.y, 0f);
            Vector3 snapPos = currentRoom.transform.position + yawOnly * LocalOffset;
            Quaternion snapRot = Quaternion.Euler(-3f, currentRoom.transform.eulerAngles.y, 0f);

            transform.SetPositionAndRotation(snapPos, snapRot);
        }
    }
}