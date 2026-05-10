using UnityEngine;

public class SceneryCameraDummy : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Speed and direction (X = Left/Right, Y = Up/Down, Z = Forward/Backward)")]
    public Vector3 moveSpeed = new Vector3(0f, 0f, 2f);

    [Tooltip("Space.Self moves relative to camera view. Space.World moves globally.")]
    public Space moveSpace = Space.Self;

    [Header("Rotation Settings")]
    [Tooltip("Slow rotation for cinematic panning (X = Tilt, Y = Pan, Z = Roll)")]
    public Vector3 rotationSpeed = new Vector3(0f, 0.5f, 0f);

    [Header("Recording Controls")]
    [Tooltip("Uncheck this if you want the camera to wait until you press the Toggle Key")]
    public bool isMoving = false;
    public KeyCode toggleKey = KeyCode.Space;

    void Update()
    {
        // Press Spacebar (or your chosen key) to start/stop the camera movement
        if (Input.GetKeyDown(toggleKey))
        {
            isMoving = !isMoving;
        }

        // Apply movement and rotation smoothly using Time.deltaTime
        if (isMoving)
        {
            transform.Translate(moveSpeed * Time.deltaTime, moveSpace);
            transform.Rotate(rotationSpeed * Time.deltaTime, Space.Self);
        }
    }
}