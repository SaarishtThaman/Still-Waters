using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public GameObject player;
    private float playerInitialX, cameraInitialX;

    public float smoothTime = 0.001f;
    private Vector3 velocity = Vector3.zero;

    void Start()
    {
        playerInitialX = player.transform.position.x;
        cameraInitialX = transform.position.x;
    }

    // void LateUpdate()
    // {
    //     float deltaX = player.transform.position.x - playerInitialX;
    //     if (deltaX >= 0)
    //     {
    //         Vector3 targetPos = new Vector3(cameraInitialX + deltaX, transform.position.y, transform.position.z);
    //         transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 5f);
    //     }
    // }
    void LateUpdate()
    {
        float deltaX = player.transform.position.x - playerInitialX;

        if (deltaX >= 0)
        {
            Vector3 targetPos = new Vector3(cameraInitialX + deltaX, transform.position.y, transform.position.z);
            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, smoothTime);
        }
    }

}
