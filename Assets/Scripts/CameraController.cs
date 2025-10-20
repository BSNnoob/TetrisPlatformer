using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    public Transform target; // player transform
    public float followSpeed = 5f;

    // orthographic zoom
    public float tetrisSize = 10f;    // default for tetris view
    public float platformerSize = 5f; // zoomed-in size for platformer
    public float zoomSpeed = 3f;

    private Camera cam;
    private bool enabledFollowAndZoom = false;
    // Remember the camera position used for Tetris view so we can return to it
    [Tooltip("Camera world position for the Tetris view. If left (0,0,0) it will default to the camera's start position.")]
    public Vector3 tetrisPosition = Vector3.zero;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
        // prefer orthographic
        cam.orthographic = true;
        cam.orthographicSize = tetrisSize;

        // If the designer didn't set a specific tetris position, capture current camera position
        if (tetrisPosition == Vector3.zero)
        {
            tetrisPosition = transform.position;
        }
    }

    void LateUpdate()
    {
        if (!enabledFollowAndZoom || target == null) return;

        // smooth follow
        Vector3 desired = new Vector3(target.position.x, target.position.y, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, desired, Time.deltaTime * followSpeed);

        // smooth zoom
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, platformerSize, Time.deltaTime * zoomSpeed);
    }

    // Public control methods
    public void EnablePlatformerMode(Transform playerTransform)
    {
        target = playerTransform;
        enabledFollowAndZoom = true;
    }

    public void DisablePlatformerMode()
    {
        // stop following immediately and start a smooth return to the Tetris framing (position + size)
        enabledFollowAndZoom = false;
        StopAllCoroutines();
        StartCoroutine(ResetToTetris());
    }

    System.Collections.IEnumerator ResetToTetris()
    {
        // Smoothly lerp both position and orthographic size back to the Tetris framing
        while (Vector3.Distance(transform.position, tetrisPosition) > 0.01f || Mathf.Abs(cam.orthographicSize - tetrisSize) > 0.01f)
        {
            transform.position = Vector3.Lerp(transform.position, tetrisPosition, Time.deltaTime * zoomSpeed);
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, tetrisSize, Time.deltaTime * zoomSpeed);
            yield return null;
        }

        transform.position = tetrisPosition;
        cam.orthographicSize = tetrisSize;
    }
}
