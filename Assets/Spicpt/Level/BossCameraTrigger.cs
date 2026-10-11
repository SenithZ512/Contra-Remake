using UnityEngine;

/// <summary>
/// ด่านบอสแบบ Contra — เดินถึงจุดนี้แล้วจอหยุดเลื่อน
/// กล้องค่อยๆ เลื่อนไปจอดที่ฉากบอส แล้วปิดทางถอยกลับ
///
/// วางเป็น Collider2D แบบ Is Trigger ขวางทางตรงปากทางเข้าฉากบอส
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BossCameraTrigger : MonoBehaviour
{
    [Header("Target")]
    /// <summary>จุดที่อยากให้กล้องไปจอด ปล่อยว่างจะใช้ตำแหน่งของตัว trigger เอง</summary>
    [SerializeField] private Transform cameraStopPoint;

    [Header("Boss")]
    /// <summary>ใส่ไว้แล้วบอสตายกล้องจะปลดล็อกให้เดินต่อได้เอง</summary>
    [SerializeField] private BossGate bossGate;

    [SerializeField] private bool releaseWhenDefeated = true;

    [Header("Player")]
    [SerializeField] private string playerTag = "Player";

    private CameraFollow cameraFollow;
    private Collider2D zone;
    private bool triggered;

    private void Awake()
    {
        zone = GetComponent<Collider2D>();

        if (zone != null && !zone.isTrigger)
        {
            Debug.LogWarning(
                "BossCameraTrigger: Collider ของ \"" + name +
                "\" ต้องติ๊ก Is Trigger ไม่งั้นผู้เล่นจะเดินชนกำแพงล่องหน"
            );
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered)
            return;

        if (!other.CompareTag(playerTag))
            return;

        cameraFollow = Object.FindObjectOfType<CameraFollow>();

        if (cameraFollow == null)
        {
            Debug.LogError("BossCameraTrigger: หา CameraFollow ในฉากไม่เจอ");
            return;
        }

        triggered = true;

        cameraFollow.LockToBoss(GetStopX());

        if (releaseWhenDefeated && bossGate != null)
        {
            bossGate.OnDefeated += Release;
        }
    }

    private void OnDestroy()
    {
        if (bossGate != null)
        {
            bossGate.OnDefeated -= Release;
        }
    }

    private void Release()
    {
        if (cameraFollow != null)
        {
            cameraFollow.ReleaseBossLock();
        }
    }

    private float GetStopX()
    {
        return cameraStopPoint != null
            ? cameraStopPoint.position.x
            : transform.position.x;
    }

    private void OnDrawGizmosSelected()
    {
        Collider2D preview = GetComponent<Collider2D>();

        if (preview != null)
        {
            Gizmos.DrawWireCube(preview.bounds.center, preview.bounds.size);
        }

        // เส้นตั้งตรงจุดที่กล้องจะไปจอด
        float x = GetStopX();

        Gizmos.DrawLine(
            new Vector3(x, transform.position.y - 10f, 0f),
            new Vector3(x, transform.position.y + 10f, 0f)
        );
    }
}
