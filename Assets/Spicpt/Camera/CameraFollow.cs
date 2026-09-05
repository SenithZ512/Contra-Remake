using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Offset")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);

    [Header("Smoothing")]
    [SerializeField] private float smoothSpeed = 5f;

    [Header("Vertical Follow")]
    [SerializeField] private bool followVertical = false;

    [Header("Invisible Wall")]
    [SerializeField] private float boundaryMargin = 0.5f;

    private float lockedX;
    private bool initialized;
    private Camera cam;

    // ขอบซ้ายสุดของจอ ณ ตอนนี้ ใช้เป็น "กำแพงล่องหน" กันเดินย้อนกลับเกินขอบจอ
    public float LeftBoundaryX =>
        lockedX - GetCameraHalfWidth() + boundaryMargin;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        if (!initialized)
        {
            lockedX = target.position.x + offset.x;
            transform.position = new Vector3(
                lockedX,
                followVertical ? target.position.y + offset.y : transform.position.y,
                offset.z
            );

            initialized = true;
            return;
        }

        float desiredX = target.position.x + offset.x;

        // ล็อคกล้องให้เลื่อนไปทางขวาได้อย่างเดียว เดินถอยหลังกล้องจะไม่ถอยตาม
        if (desiredX > lockedX)
        {
            lockedX = desiredX;
        }

        float targetY = followVertical
            ? target.position.y + offset.y
            : transform.position.y;

        Vector3 desiredPosition = new Vector3(
            lockedX,
            targetY,
            offset.z
        );

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            smoothSpeed * Time.deltaTime
        );
    }

    private float GetCameraHalfWidth()
    {
        if (cam == null || !cam.orthographic)
            return 0f;

        return cam.orthographicSize * cam.aspect;
    }
}
