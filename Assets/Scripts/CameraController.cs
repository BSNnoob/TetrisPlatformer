using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    public Transform target;
    public float followSpeed = 5f;

    // orthographic zoom
    public float tetrisSize = 10f;
    public float platformerSize = 5f;
    public float zoomSpeed = 3f;

    private Camera cam;
    private bool enabledFollowAndZoom = false;
    public Vector3 tetrisPosition = Vector3.zero;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
        cam.orthographic = true;
        cam.orthographicSize = tetrisSize;

        if (tetrisPosition == Vector3.zero)
        {
            tetrisPosition = transform.position;
        }
    }

    void LateUpdate()
    {
        if (!enabledFollowAndZoom || target == null) return;

        Vector3 desired = new Vector3(target.position.x, target.position.y, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, desired, Time.deltaTime * followSpeed);

        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, platformerSize, Time.deltaTime * zoomSpeed);
    }

    public void EnablePlatformerMode(Transform playerTransform)
    {
        target = playerTransform;
        enabledFollowAndZoom = true;
    }

    public void DisablePlatformerMode()
    {
        enabledFollowAndZoom = false;
        StopAllCoroutines();
        StartCoroutine(ResetToTetris());
    }

    System.Collections.IEnumerator ResetToTetris()
    {
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
