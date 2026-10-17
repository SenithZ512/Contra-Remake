using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 1;

    [Header("Falcon (ไอเทมล้างจอ)")]
    /// <summary>
    /// ไอเทมนกเหยี่ยวข้ามตัวนี้ไป
    /// ต้นฉบับข้าม Pill Box Sensor กับแคปซูลที่บินผ่านจอ
    /// </summary>
    [SerializeField] private bool ignoreFalcon;

    [Header("Death")]
    [SerializeField] private GameObject deathEffectPrefab;
    [SerializeField] private Vector2 deathEffectOffset = new Vector2(0f, 0.85f);

    [Header("กระเด็นตอนตาย (เปิดเฉพาะศัตรูที่เป็นคน)")]
    [SerializeField] private bool knockbackOnDeath;

    /// <summary>แรงถีบไปตามทิศกระสุน</summary>
    [SerializeField] private float knockbackForce = 5f;

    /// <summary>แรงดีดขึ้น ทำให้ลอยเป็นเส้นโค้งแบบต้นฉบับ</summary>
    [SerializeField] private float knockbackUpForce = 7f;

    [SerializeField] private float knockbackGravity = 4f;

    /// <summary>องศาต่อวินาที ติดลบ/บวกตามทิศที่ปลิว</summary>
    [SerializeField] private float knockbackSpin = 420f;

    /// <summary>ลอยนานเท่าไหร่ก่อนระเบิด</summary>
    [SerializeField] private float knockbackDuration = 0.45f;

    private int currentHealth;
    private bool isDead;
    private bool isInvulnerable;
    private Vector2 lastHitDirection;

    public bool IsDead => isDead;
    public bool IsInvulnerable => isInvulnerable;
    public bool IgnoreFalcon => ignoreFalcon;

    /// <summary>ใช้ตอนป้อมปืนปิดฝา หรือช่วงที่ไม่ควรรับดาเมจ</summary>
    public void SetInvulnerable(bool value)
    {
        isInvulnerable = value;
    }

    /// <summary>ยิงตอนตาย ก่อนวัตถุถูกทำลาย ใครอยากดรอปของตอนตายมาดักตรงนี้ได้</summary>
    public event System.Action OnDied;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        TakeDamage(damage, Vector2.zero);
    }

    /// <summary>hitDirection คือทิศที่กระสุนวิ่งอยู่ ใช้กำหนดทางที่ศพจะปลิวไป</summary>
    public void TakeDamage(int damage, Vector2 hitDirection)
    {
        if (isDead || isInvulnerable)
            return;

        if (hitDirection.sqrMagnitude > 0.0001f)
        {
            lastHitDirection = hitDirection;
        }

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// ตายทันทีไม่กระเด็น ใช้ตอนจมน้ำหรือตกเหว
    /// ท่ากระเด็นถอยหลังไม่เข้ากับการจมน้ำ
    /// </summary>
    public void KillInstantly(bool useDeathEffect)
    {
        if (isDead)
            return;

        isDead = true;

        if (OnDied != null)
        {
            OnDied();
        }

        if (useDeathEffect)
        {
            SpawnDeathEffect();
        }

        Destroy(gameObject);
    }

    private void Die()
    {
        isDead = true;

        if (OnDied != null)
        {
            OnDied();
        }

        if (knockbackOnDeath && isActiveAndEnabled)
        {
            StartCoroutine(KnockbackDeathRoutine());
            return;
        }

        SpawnDeathEffect();

        Destroy(gameObject);
    }

    /// <summary>
    /// ตายแบบ Contra: โดนยิงแล้วเด้งถอยหลังลอยเป็นเส้นโค้ง หมุนตีลังกา
    /// แล้วค่อยระเบิดกลางอากาศ
    /// </summary>
    private IEnumerator KnockbackDeathRoutine()
    {
        StopEverythingElse();

        Vector2 push = lastHitDirection;
        push.y = 0f;

        if (push.sqrMagnitude < 0.0001f)
        {
            // ไม่รู้ว่าโดนยิงมาจากทางไหน ก็ให้ถอยหลังจากทางที่มันหันอยู่
            push = transform.localScale.x >= 0f
                ? Vector2.left
                : Vector2.right;
        }

        push.Normalize();

        Rigidbody2D body = GetComponent<Rigidbody2D>();

        if (body != null)
        {
            body.bodyType = RigidbodyType2D.Dynamic;
            body.constraints = RigidbodyConstraints2D.None;
            body.gravityScale = knockbackGravity;

            body.velocity =
                push * knockbackForce + Vector2.up * knockbackUpForce;

            body.angularVelocity = -push.x * knockbackSpin;
        }

        yield return new WaitForSeconds(knockbackDuration);

        SpawnDeathEffect();

        Destroy(gameObject);
    }

    /// <summary>ศพต้องไม่คิด ไม่ยิง และไม่ชนอะไรอีก</summary>
    private void StopEverythingElse()
    {
        foreach (MonoBehaviour behaviour in
            GetComponentsInChildren<MonoBehaviour>())
        {
            if (behaviour != this)
            {
                behaviour.enabled = false;
            }
        }

        foreach (Collider2D bodyCollider in
            GetComponentsInChildren<Collider2D>())
        {
            bodyCollider.enabled = false;
        }
    }

    private void SpawnDeathEffect()
    {
        if (deathEffectPrefab == null)
            return;

        // จุดหมุนของสไปรท์อยู่ที่เท้า จึงต้องเลื่อนระเบิดขึ้นมากลางลำตัว
        Instantiate(
            deathEffectPrefab,
            (Vector2)transform.position +
                deathEffectOffset * Mathf.Abs(transform.lossyScale.y),
            Quaternion.identity
        );
    }
}
