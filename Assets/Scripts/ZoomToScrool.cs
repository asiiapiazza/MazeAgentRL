using UnityEngine;

public class ZoomToScrool : MonoBehaviour
{
    float zoomTarget;
    new Camera camera;

    float velocity = 0f;
    float multiplier = 15f, minZoom =0f, maxZoom = 60f, smoothTime = 0.1f;

    void Start()
    {
        camera = GetComponent<Camera>();
        zoomTarget = camera.orthographic ? camera.orthographicSize : camera.fieldOfView;
    }

    void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        zoomTarget -= scroll * multiplier;

        if (camera.orthographic)
        {
            zoomTarget = Mathf.Clamp(zoomTarget, minZoom, maxZoom);
            camera.orthographicSize = Mathf.SmoothDamp(camera.orthographicSize, zoomTarget, ref velocity, smoothTime);
        }
        else
        {
            zoomTarget = Mathf.Clamp(zoomTarget, minZoom, maxZoom);
            camera.fieldOfView = Mathf.SmoothDamp(camera.fieldOfView, zoomTarget, ref velocity, smoothTime);
        }
    }

}
