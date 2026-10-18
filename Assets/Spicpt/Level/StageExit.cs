using UnityEngine;

/// <summary>
/// ทางออกท้ายด่าน เดินเข้าไปแล้วไปด่านถัดไป
///
/// ล็อกไว้จนกว่าจะเก็บบอสได้ ตรงกับต้นฉบับที่
///   ด่าน 1 ยิงแกนประตูแตกแล้วทางเข้าฐานถึงจะเปิด
///   ด่าน 3 บอสระเบิดแล้วทางเข้าฐานที่สองถึงจะโผล่
///
/// ปลดล็อกได้สามทาง เลือกอันที่ตรงกับบอสของด่านนั้น
///   - BossGate       สำหรับบอสประตูแบบด่าน 1
///   - EnemyHealth    สำหรับบอสที่มีตัวเดียวจบ เช่นปากมังกรด่าน 3
///   - เรียก Unlock() เอง จากสคริปต์อื่น
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class StageExit : MonoBehaviour
{
    [Header("Unlock")]
    /// <summary>บอสประตูแบบด่าน 1 ใส่ไว้แล้วจะรอ event OnDefeated ของมัน</summary>
    [SerializeField] private BossGate bossGate;

    /// <summary>บอสตัวเดียวจบ เช่นปากมังกร ใส่ EnemyHealth ของมันไว้</summary>
    [SerializeField] private EnemyHealth bossHealth;

    /// <summary>เปิดไว้ตั้งแต่ต้น ใช้ตอนเทสหรือด่านที่ไม่มีบอส</summary>
    [SerializeField] private bool startUnlocked;

    [Header("Look")]
    /// <summary>ของที่จะโผล่มาตอนปลดล็อก เช่นประตูหรือทางเข้า</summary>
    [SerializeField] private GameObject exitVisual;

    /// <summary>ซ่อนทางออกไว้จนกว่าจะปลดล็อก</summary>
    [SerializeField] private bool hideUntilUnlocked = true;

    [Header("Transition")]
    /// <summary>หน่วงหลังเดินเข้าทางออก ให้เห็นว่าตัวละครเข้าไปแล้วจริงๆ</summary>
    [SerializeField] private float delayBeforeLoad = 0.6f;

    [SerializeField] private float fadeDuration = 0.8f;

    /// <summary>ว่างไว้ = ไปฉากถัดไปตามลำดับใน Build Settings</summary>
    [SerializeField] private string overrideSceneName;

    /// <summary>จบด่านสุดท้ายแล้ววนกลับด่านแรก</summary>
    [SerializeField] private bool loopAtEnd;

    [Header("Player")]
    [SerializeField] private string playerTag = "Player";

    private Collider2D zone;
    private bool unlocked;
    private bool used;

    public bool IsUnlocked
    {
        get { return unlocked; }
    }

    private void Awake()
    {
        zone = GetComponent<Collider2D>();
        zone.isTrigger = true;
    }

    private void Start()
    {
        if (bossGate != null)
        {
            bossGate.OnDefeated += Unlock;
        }

        if (bossHealth != null)
        {
            bossHealth.OnDied += Unlock;
        }

        if (startUnlocked || (bossGate == null && bossHealth == null))
        {
            Unlock();
            return;
        }

        SetLocked();
    }

    private void OnDestroy()
    {
        if (bossGate != null)
        {
            bossGate.OnDefeated -= Unlock;
        }

        if (bossHealth != null)
        {
            bossHealth.OnDied -= Unlock;
        }
    }

    private void SetLocked()
    {
        unlocked = false;

        // ปิดทริกเกอร์ไว้ด้วย กันเดินทะลุไปก่อนเก็บบอสได้
        if (zone != null)
        {
            zone.enabled = false;
        }

        if (hideUntilUnlocked && exitVisual != null)
        {
            exitVisual.SetActive(false);
        }
    }

    /// <summary>เรียกเองได้จากสคริปต์อื่นถ้าเงื่อนไขจบด่านเป็นแบบอื่น</summary>
    public void Unlock()
    {
        if (unlocked)
            return;

        unlocked = true;

        if (zone != null)
        {
            zone.enabled = true;
        }

        if (exitVisual != null)
        {
            exitVisual.SetActive(true);
        }

        Debug.Log("ทางออกเปิดแล้ว เดินเข้าไปเพื่อไปด่านถัดไป");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (used || !unlocked || SceneFlow.IsLoading)
            return;

        if (!other.CompareTag(playerTag))
            return;

        used = true;

        SceneFlow.GoToNext(
            this, delayBeforeLoad, fadeDuration, overrideSceneName, loopAtEnd
        );
    }

    private void OnDrawGizmos()
    {
        // วาดกรอบทางออกให้เห็นตอนจัดฉาก สีเขียวคือปลดแล้ว แดงคือยังล็อก
        Collider2D preview = GetComponent<Collider2D>();

        if (preview == null)
            return;

        Gizmos.color = Application.isPlaying && unlocked
            ? new Color(0.3f, 1f, 0.3f, 0.35f)
            : new Color(1f, 0.4f, 0.3f, 0.35f);

        Bounds box = preview.bounds;

        Gizmos.DrawCube(box.center, box.size);
    }
}
