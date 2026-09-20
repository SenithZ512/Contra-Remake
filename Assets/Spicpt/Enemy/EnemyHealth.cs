using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 1;

    [Header("Death")]
    [SerializeField] private GameObject deathEffectPrefab;
    [SerializeField] private Vector2 deathEffectOffset = new Vector2(0f, 0.85f);

    private int currentHealth;
    private bool isDead;
    private bool isInvulnerable;

    public bool IsDead => isDead;
    public bool IsInvulnerable => isInvulnerable;

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
        if (isDead || isInvulnerable)
            return;

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;

        if (OnDied != null)
        {
            OnDied();
        }

        if (deathEffectPrefab != null)
        {
            // จุดหมุนของสไปรท์อยู่ที่เท้า จึงต้องเลื่อนระเบิดขึ้นมากลางลำตัว
            Instantiate(
                deathEffectPrefab,
                (Vector2)transform.position + deathEffectOffset,
                Quaternion.identity
            );
        }

        Destroy(gameObject);
    }
}
