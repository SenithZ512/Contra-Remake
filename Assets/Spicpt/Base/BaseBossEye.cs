using System.Collections;
using UnityEngine;

/// <summary>
/// บอสท้ายด่าน Base 1 - Garmakilma (独炎眼 ガルマキルマ "ตาไฟเดี่ยว")
///
/// ตัวเลขทั้งหมดลอกจาก boss_eye_routine ของ ROM ตรงๆ
///   - โผล่ก็ต่อเมื่อพัง Shoud ครบ 4 ลูก แล้วรออีก 64 เฟรม
///   - เลือด 16
///   - วิ่งไปมา 1.25 พิกเซลต่อเฟรม กลับทิศที่พิกเซล 80 กับ 176
///   - ยิงวงแหวนไฟลงมา ซึ่งยิงแตกได้
///
/// จุดที่คนไม่ค่อยรู้: จังหวะยิงของบอสขึ้นกับว่าเราถือปืนอะไรอยู่
/// ถือปืนแรงมา มันยิงถี่ขึ้นเกือบสามเท่า เป็นการปรับความยากซ่อนไว้ตั้งแต่ปี 1987
///
/// อีกอย่างคือต้นฉบับตรึงเลือดที่แสดงไว้ที่ 1 ตลอด เลือดจริงเก็บแยก
/// เพื่อให้ทุกนัดที่โดนเล่นเสียง "ติ๊ง" โลหะ เป็นฟีดแบ็กล้วนๆ
/// </summary>
public class BaseBossEye : MonoBehaviour, IBaseDamageable
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 16;

    [Header("Wake Up")]
    /// <summary>รอกี่วินาทีหลัง Shoud พังครบถึงจะโผล่ ต้นฉบับ 64 เฟรม</summary>
    [SerializeField] private float wakeDelay = 1.07f;

    [Header("Patrol")]
    /// <summary>ต้นฉบับ 1.25 พิกเซลต่อเฟรม = 75 พิกเซลต่อวินาที</summary>
    [SerializeField] private float moveSpeed = 3.125f;

    /// <summary>ขอบที่กลับทิศ เป็นพิกเซลแนวนอนแบบ NES</summary>
    [SerializeField] private float leftTurnPx = 80f;

    [SerializeField] private float rightTurnPx = 176f;

    [Header("Fire Ring")]
    [SerializeField] private GameObject fireRingPrefab;

    /// <summary>หน่วงก่อนยิงนัดแรก ต้นฉบับ 32 เฟรม</summary>
    [SerializeField] private float firstShotDelay = 0.53f;

    /// <summary>
    /// จังหวะยิงตามความแรงของปืนที่ผู้เล่นถืออยู่
    /// ต้นฉบับ boss_eye_attack_delay_tbl = 112, 80, 64, 40 เฟรม
    /// เรียงจากปืนอ่อนไปปืนแรง
    /// </summary>
    [SerializeField] private float[] fireIntervalByWeapon =
        { 1.87f, 1.33f, 1.07f, 0.67f };

    [Header("Sprites")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float frameRate = 8f;

    [Header("Hit Feedback")]
    [SerializeField] private Color hitFlashColor = Color.white;
    [SerializeField] private float hitFlashDuration = 0.06f;

    [Header("Death")]
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private int deathExplosionCount = 10;
    [SerializeField] private float deathExplosionSpread = 1.6f;
    [SerializeField] private float deathExplosionInterval = 0.09f;

    private BaseRoom room;
    private Collider2D hitBox;

    private int health;
    private bool awake;
    private bool dead;
    private float direction = 1f;
    private float fireTimer;
    private float animTime;
    private float flashTimer;
    private float leftLimit;
    private float rightLimit;

    public bool CanBeHit
    {
        get { return awake && !dead; }
    }

    public bool IsAwake
    {
        get { return awake; }
    }

    private void Awake()
    {
        room = GetComponentInParent<BaseRoom>();
        hitBox = GetComponent<Collider2D>();

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        leftLimit = BaseStageGeometry.ToWorldX(leftTurnPx);
        rightLimit = BaseStageGeometry.ToWorldX(rightTurnPx);

        Sleep();
    }

    private void Sleep()
    {
        health = maxHealth;
        awake = false;
        dead = false;

        if (hitBox != null)
        {
            hitBox.enabled = false;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }
    }

    /// <summary>ห้องเรียกเมื่อ Shoud พังครบทุกลูก</summary>
    public void Awaken()
    {
        if (awake || dead)
            return;

        StartCoroutine(WakeRoutine());
    }

    private IEnumerator WakeRoutine()
    {
        yield return new WaitForSeconds(wakeDelay);

        awake = true;
        fireTimer = firstShotDelay;

        if (hitBox != null)
        {
            hitBox.enabled = true;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }
    }

    private void Update()
    {
        if (!awake || dead)
            return;

        Patrol();
        Animate();
        UpdateFire();
        UpdateFlash();
    }

    private void Patrol()
    {
        float x = transform.position.x + direction * moveSpeed * Time.deltaTime;

        if (x >= rightLimit)
        {
            x = rightLimit;
            direction = -1f;
        }
        else if (x <= leftLimit)
        {
            x = leftLimit;
            direction = 1f;
        }

        transform.position = new Vector3(
            x,
            transform.position.y,
            transform.position.z
        );
    }

    private void Animate()
    {
        if (spriteRenderer == null || frames == null || frames.Length == 0)
            return;

        animTime += Time.deltaTime;

        int index = Mathf.FloorToInt(animTime * frameRate) % frames.Length;

        spriteRenderer.sprite = frames[index];
    }

    private void UpdateFire()
    {
        if (fireRingPrefab == null)
            return;

        fireTimer -= Time.deltaTime;

        if (fireTimer > 0f)
            return;

        fireTimer = GetFireInterval();

        Instantiate(fireRingPrefab, transform.position, Quaternion.identity);
    }

    /// <summary>ปืนที่ผู้เล่นถืออยู่ยิ่งแรง บอสยิ่งยิงถี่</summary>
    private float GetFireInterval()
    {
        if (fireIntervalByWeapon == null || fireIntervalByWeapon.Length == 0)
            return 1.2f;

        return fireIntervalByWeapon[
            Mathf.Clamp(
                BaseStage.PlayerWeaponStrength,
                0,
                fireIntervalByWeapon.Length - 1
            )
        ];
    }

    private void UpdateFlash()
    {
        if (spriteRenderer == null)
            return;

        if (flashTimer <= 0f)
        {
            spriteRenderer.color = Color.white;
            return;
        }

        flashTimer -= Time.deltaTime;

        spriteRenderer.color = hitFlashColor;
    }

    public void TakeHit(int damage, Vector2 from)
    {
        if (!CanBeHit)
            return;

        health -= damage;
        flashTimer = hitFlashDuration;

        if (health > 0)
            return;

        dead = true;

        if (hitBox != null)
        {
            hitBox.enabled = false;
        }

        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        for (int i = 0; i < deathExplosionCount; i++)
        {
            if (explosionPrefab != null)
            {
                Vector3 offset = new Vector3(
                    Random.Range(-deathExplosionSpread, deathExplosionSpread),
                    Random.Range(-deathExplosionSpread, deathExplosionSpread),
                    0f
                );

                Instantiate(
                    explosionPrefab,
                    transform.position + offset,
                    Quaternion.identity
                );
            }

            yield return new WaitForSeconds(deathExplosionInterval);
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        if (room != null)
        {
            room.NotifyBossDefeated();
        }
    }
}
