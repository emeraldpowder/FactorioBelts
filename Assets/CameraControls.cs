using UnityEngine;

public class CameraControls : MonoBehaviour
{
    private Camera cameraComponent;
    private Vector3 touchStart;

    private void Start()
    {
        cameraComponent = GetComponent<Camera>();
    }

    private void Update()
    {
        if (Input.mouseScrollDelta != Vector2.zero)
        {
            cameraComponent.orthographicSize -= 2 * Input.mouseScrollDelta.y;
        }

        Vector3 worldPoint = cameraComponent.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10));
        if (Input.GetMouseButtonDown(0))
        {
            touchStart = worldPoint;
        }
        else if (Input.GetMouseButton(0))
        {
            transform.Translate(touchStart - worldPoint);
        }
    }
}