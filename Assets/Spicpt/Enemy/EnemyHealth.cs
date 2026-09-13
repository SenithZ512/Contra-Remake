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

    public bool IsDead => isDead;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (isDead)
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
