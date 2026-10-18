using UnityEngine;

public class EnemyContactDamage : MonoBehaviour
{
    [Header("Contact Damage")]
    [SerializeField] private int damage = 1;

    [Header("Barrier (ไอเทม B ของผู้เล่น)")]
    /// <summary>
    /// ตัวนี้ตายไหมถ้ามาโดนผู้เล่นที่มีเกราะ
    ///
    /// ต้นฉบับ: ทหารเดินชนแล้วตายทันที แต่ยานพาหนะกับป้อมปืนไม่เป็นอะไร
    /// เปิดไว้สำหรับศัตรูที่เป็นคน ปิดสำหรับรถถังและป้อม
    /// </summary>
    [SerializeField] private bool diesToBarrier = true;

    [Header("Player")]
    [SerializeField] private string playerTag = "Player";

    private Collider2D bodyCollider;
    private PlayerHealth playerHealth;
    private Collider2D playerCollider;
    private EnemyHealth health;

    private void Awake()
    {
        bodyCollider = GetComponent<Collider2D>();
        health = GetComponent<EnemyHealth>();

        if (bodyCollider == null)
        {
            Debug.LogError(
                "EnemyContactDamage: ต้องมี Collider2D"
            );
        }
    }

    private bool ignoreApplied;

    private void Start()
    {
        FindPlayer();
    }

    /// <summary>
    /// หาผู้เล่นแล้วจำไว้
    ///
    /// ต้องเรียกซ้ำได้ ไม่ใช่หาครั้งเดียวตอน Start แล้วเลิก
    /// เพราะศัตรูที่เกิดกลางเกม (เช่นหินที่ร่วงจากรู) อาจเกิดในจังหวะที่หาไม่เจอ
    /// ถ้าไม่ลองใหม่ ตัวนั้นจะไม่ทำดาเมจไปตลอดชีวิตโดยไม่มีอะไรฟ้อง
    /// </summary>
    private void FindPlayer()
    {
        GameObject playerObject =
            GameObject.FindGameObjectWithTag(playerTag);

        if (playerObject == null)
            return;

        playerHealth = playerObject.GetComponent<PlayerHealth>();
        playerCollider = playerObject.GetComponent<Collider2D>();
    }

    /// <summary>
    /// ศัตรูเดินทะลุตัวผู้เล่นได้ ไม่ผลักกันไปมา แต่ยังชนพื้นตามปกติ
    ///
    /// ต้องรอให้คอลไลเดอร์เปิดอยู่ก่อน ไม่งั้น Unity จะโยน exception
    /// แล้ว Start ส่วนที่เหลือจะไม่ทำงาน ซึ่งเงียบมากเวลาไล่หาสาเหตุ
    ///
    /// Unity ล้างค่านี้ทิ้งทุกครั้งที่คอลไลเดอร์ถูกปิดแล้วเปิดใหม่ จึงต้องคอยเช็ค
    /// </summary>
    private void ApplyIgnoreCollision()
    {
        if (bodyCollider == null || playerCollider == null)
            return;

        if (!bodyCollider.enabled || !bodyCollider.gameObject.activeInHierarchy)
        {
            ignoreApplied = false;
            return;
        }

        if (ignoreApplied)
            return;

        Physics2D.IgnoreCollision(bodyCollider, playerCollider, true);

        ignoreApplied = true;
    }

    private void Update()
    {
        // ยังไม่เจอผู้เล่นก็ลองใหม่ทุกเฟรม จนกว่าจะเจอ
        if (playerHealth == null || playerCollider == null)
        {
            FindPlayer();

            if (playerHealth == null || playerCollider == null)
                return;
        }

        ApplyIgnoreCollision();

        if (bodyCollider == null || playerHealth.IsDead)
            return;

        // คอลไลเดอร์ปิดอยู่ก็ยังไม่ทำดาเมจ
        // เช่นหินที่กำลังส่ายรอร่วง ต้นฉบับก็ยังไม่เปิดการชนในช่วงนั้น
        if (!bodyCollider.enabled)
            return;

        // เช็คการซ้อนทับด้วยกรอบ bounds เพราะปิดการชนทางฟิสิกส์กับผู้เล่นไปแล้ว
        if (!bodyCollider.bounds.Intersects(playerCollider.bounds))
            return;

        Hit();
    }

    /// <summary>
    /// ทางตรวจที่สอง สำหรับศัตรูที่เป็น Trigger
    ///
    /// การเทียบ bounds อย่างเดียวพึ่งพาว่าระบบฟิสิกส์อัปเดตตำแหน่งทัน
    /// ซึ่งไม่แน่นอนกับของที่ขยับด้วย transform ทุกเฟรม
    /// มีสองทางไว้ก่อน เรียกซ้ำก็ไม่เป็นไรเพราะ TakeDamage กันตายซ้ำอยู่แล้ว
    /// </summary>
    private void OnTriggerStay2D(Collider2D other)
    {
        TryHitPlayer(other);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHitPlayer(other);
    }

    private void TryHitPlayer(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
            return;

        if (bodyCollider == null || !bodyCollider.enabled)
            return;

        if (playerHealth == null)
        {
            FindPlayer();
        }

        if (playerHealth == null || playerHealth.IsDead)
            return;

        Hit();
    }

    private void Hit()
    {
        // ผู้เล่นมีเกราะอยู่ คนที่มาชนตายแทน ส่วนรถถังกับป้อมไม่เป็นอะไร
        if (playerHealth.HasBarrier)
        {
            if (diesToBarrier && health != null && !health.IsDead)
            {
                health.KillInstantly(true);
            }

            return;
        }

        playerHealth.TakeDamage(damage);
    }
}
