using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Offset")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);

    [Header("Smoothing")]
    [SerializeField] private float smoothSpeed = 5f;

    public enum HorizontalMode
    {
        /// <summary>กล้องไม่ขยับแกน X เลย ใช้กับด่านปีนขึ้นแนวตั้ง</summary>
        Fixed,

        /// <summary>ตามผู้เล่นซ้ายขวาอิสระ</summary>
        Follow,

        /// <summary>เลื่อนไปขวาอย่างเดียว ไม่ถอยกลับ ใช้กับด่านแนวนอน</summary>
        ScrollRightOnly
    }

    public enum VerticalMode
    {
        /// <summary>กล้องไม่ขยับแกน Y เลย ใช้กับด่านแนวนอน</summary>
        Fixed,

        /// <summary>ตามผู้เล่นขึ้นลงอิสระ</summary>
        Follow,

        /// <summary>เลื่อนขึ้นอย่างเดียว ไม่ถอยลง ใช้กับด่านปีนขึ้นแบบ Waterfall</summary>
        ScrollUpOnly
    }

    [Header("Horizontal")]
    [SerializeField] private HorizontalMode horizontalMode =
        HorizontalMode.ScrollRightOnly;

    [Header("Vertical")]
    [SerializeField] private VerticalMode verticalMode = VerticalMode.Fixed;

    [Header("Invisible Wall")]
    [SerializeField] private float boundaryMargin = 0.5f;

    [Header("Boss Lock")]
    /// <summary>ความเร็วเลื่อนกล้องเข้าหาฉากบอส ช้ากว่าปกติให้เห็นว่ากำลังเลื่อน</summary>
    [SerializeField] private float bossPanSpeed = 2.5f;

    private float lockedX;
    private float lockedY;
    private bool initialized;
    private Camera cam;

    private bool bossLocked;
    private float bossLockX;

    /// <summary>ล็อกฉากบอสอยู่ไหม ตอนล็อกกล้องจะไม่ตามผู้เล่นแล้ว</summary>
    public bool IsBossLocked => bossLocked;

    /// <summary>
    /// กล้องด่านนี้ไต่ตามผู้เล่นขึ้นไปเรื่อยๆ หรือเปล่า
    ///
    /// สำคัญกับการเกิดใหม่ ถ้าไปเกิดเหนือขอบจอกับกล้องแบบนี้
    /// กล้องจะไต่ตามขึ้นไปอีก ตายซ้ำก็ไต่ขึ้นอีก ไม่มีวันจบ
    /// </summary>
    public bool FollowsUpward =>
        verticalMode == VerticalMode.ScrollUpOnly ||
        verticalMode == VerticalMode.Follow;

    // ขอบซ้ายสุดของจอ ณ ตอนนี้ ใช้เป็น "กำแพงล่องหน" กันเดินย้อนกลับเกินขอบจอ
    // ต้องวัดจากตำแหน่งกล้องจริง ไม่ใช่ lockedX เพราะกล้องเลื่อนตามแบบ Lerp
    // ตอนวิ่งไปขวาเร็วๆ lockedX จะพุ่งนำหน้าจอไปก่อน กำแพงจะไปโผล่กลางจอ
    public float LeftBoundaryX =>
        transform.position.x - GetCameraHalfWidth() + boundaryMargin;

    /// <summary>ขอบขวาของจอ ใช้กันผู้เล่นเดินเลยฉากบอสออกไป</summary>
    public float RightBoundaryX =>
        transform.position.x + GetCameraHalfWidth() - boundaryMargin;

    /// <summary>ขอบล่างของจอ ด่านปีนขึ้นใช้เป็นเส้นตกตาย</summary>
    public float BottomBoundaryY =>
        transform.position.y - GetCameraHalfHeight();

    /// <summary>
    /// สั่งให้กล้องเลื่อนไปจอดที่ฉากบอสแล้วหยุดตามผู้เล่น
    /// เรียกจาก BossCameraTrigger ตอนผู้เล่นเดินถึง
    /// </summary>
    public void LockToBoss(float centerX)
    {
        bossLocked = true;
        bossLockX = centerX;

        // ดัน lockedX ตามไปด้วย เผื่อปลดล็อกทีหลังกล้องจะได้ไม่กระตุกถอยกลับ
        if (centerX > lockedX)
        {
            lockedX = centerX;
        }
    }

    /// <summary>ปลดล็อก กลับไปตามผู้เล่นเหมือนเดิม (ใช้ตอนบอสตาย)</summary>
    public void ReleaseBossLock()
    {
        bossLocked = false;
    }

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
            lockedX = horizontalMode == HorizontalMode.Fixed
                ? transform.position.x
                : target.position.x + offset.x;

            lockedY = verticalMode == VerticalMode.Fixed
                ? transform.position.y
                : target.position.y + offset.y;

            transform.position = new Vector3(lockedX, lockedY, offset.z);

            initialized = true;
            return;
        }

        if (bossLocked)
        {
            // จอดนิ่งที่ฉากบอส ไม่สนว่าผู้เล่นขยับไปไหน
            transform.position = Vector3.Lerp(
                transform.position,
                new Vector3(bossLockX, transform.position.y, offset.z),
                bossPanSpeed * Time.deltaTime
            );

            return;
        }

        float desiredX = target.position.x + offset.x;

        if (horizontalMode == HorizontalMode.Follow)
        {
            lockedX = desiredX;
        }
        else if (horizontalMode == HorizontalMode.ScrollRightOnly)
        {
            // เลื่อนไปทางขวาได้อย่างเดียว เดินถอยหลังกล้องจะไม่ถอยตาม
            if (desiredX > lockedX)
            {
                lockedX = desiredX;
            }
        }

        float desiredY = target.position.y + offset.y;

        if (verticalMode == VerticalMode.Follow)
        {
            lockedY = desiredY;
        }
        else if (verticalMode == VerticalMode.ScrollUpOnly)
        {
            // ขึ้นแล้วขึ้นเลย ตกลงมากล้องไม่ตามลง ผู้เล่นจะหลุดจอล่างแล้วตาย
            if (desiredY > lockedY)
            {
                lockedY = desiredY;
            }
        }

        Vector3 desiredPosition = new Vector3(
            lockedX,
            lockedY,
            offset.z
        );

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            smoothSpeed * Time.deltaTime
        );
    }

    private float GetCameraHalfHeight()
    {
        if (cam == null || !cam.orthographic)
            return 0f;

        return cam.orthographicSize;
    }

    private float GetCameraHalfWidth()
    {
        if (cam == null || !cam.orthographic)
            return 0f;

        return cam.orthographicSize * cam.aspect;
    }
}
