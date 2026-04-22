using UnityEngine;

// Smooth rightward-only camera follow. Accepts a Transform reference so it
// is not coupled to any specific player implementation.
public class CameraMovement : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] float     smoothTime = 0.001f;

    private float   targetInitialX;
    private float   cameraInitialX;
    private Vector3 velocity;

    void Start()
    {
        targetInitialX = target.position.x;
        cameraInitialX = transform.position.x;
    }

    void LateUpdate()
    {
        float deltaX = target.position.x - targetInitialX;
        if (deltaX < 0) return;

        Vector3 dest = new Vector3(cameraInitialX + deltaX, transform.position.y, transform.position.z);
        transform.position = Vector3.SmoothDamp(transform.position, dest, ref velocity, smoothTime);
    }
}
