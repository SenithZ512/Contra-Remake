using UnityEngine;

/// <summary>
/// ป้อมอาวุธแบบ Contra — ติดอยู่กับที่ ไม่ยิงสวน
/// ยิงจนพังแล้วปล่อยไอเท็มอาวุธออกมา เหมือน WeaponDrone แต่ไม่บิน
///
/// วางตามกำแพงหรือบนพื้นได้เลย เป็นของรางวัลที่ผู้เล่นต้องสังเกตเห็นเอง
/// </summary>
[RequireComponent(typeof(EnemyHealth))]
public class WeaponPod : MonoBehaviour
{
    [Header("Weapon Carried")]
    [SerializeField] private WeaponType weaponType = WeaponType.SpreadGun;
    [SerializeField] private bool randomWeapon = true;

    [Header("Drop")]
    [SerializeField] private GameObject weaponPickupPrefab;

    /// <summary>เลื่อนจุดปล่อยไอเท็มให้พ้นตัวป้อม จะได้ไม่เกิดจมอยู่ในกำแพง</summary>
    [SerializeField] private Vector2 dropOffset = new Vector2(0f, 0.5f);

    [Header("Launch (แรงดีดไอเทมตอนป้อมแตก)")]
    [SerializeField] private bool launchPickup = true;

    /// <summary>แรงดีดขึ้น ยิ่งมากยิ่งลอยสูง</summary>
    [SerializeField] private float launchUpForce = 6f;

    /// <summary>แรงดีดออกข้าง 0 = พุ่งขึ้นตรงๆ</summary>
    [SerializeField] private float launchSideForce = 2f;

    /// <summary>สุ่มว่าจะกระเด็นไปซ้ายหรือขวา</summary>
    [SerializeField] private bool launchRandomSide = true;

    /// <summary>ใช้ตอนปิดสุ่ม 1 = ขวา, -1 = ซ้าย</summary>
    [SerializeField] private float launchSide = 1f;

    [Header("Rapid Fire (ไอเทม R)")]
    /// <summary>บังคับให้ชิ้นนี้เป็นไอเทมยิงรัวแทนอาวุธ</summary>
    [SerializeField] private bool dropRapidFire;

    /// <summary>ดรอปอะไรออกมา Barrier กับ Falcon ไม่ได้เปลี่ยนปืนที่ถืออยู่</summary>
    [SerializeField]
    private WeaponPickup.PickupKind dropKind = WeaponPickup.PickupKind.Weapon;

    /// <summary>โอกาสสุ่มออกเป็นไอเทมยิงรัว 0 = ไม่สุ่มเลย 1 = ออกแน่นอน</summary>
    [Range(0f, 1f)]
    [SerializeField] private float rapidFireChance;

    [Header("Activation (ต้องเข้าใกล้ก่อนถึงยิงแตก)")]
    [SerializeField] private bool requirePlayerNearby = true;

    /// <summary>ผู้เล่นต้องเข้ามาใกล้กว่านี้ ป้อมถึงจะรับดาเมจ</summary>
    [SerializeField] private float activationRange = 7f;

    [SerializeField] private string playerTag = "Player";

    /// <summary>สีตอนยังยิงไม่เข้า หม่นๆ ให้เห็นว่ายังไม่พร้อม</summary>
    [SerializeField] private Color asleepColor = new Color(0.35f, 0.33f, 0.28f);

    [Header("Look")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    /// <summary>กะพริบช้าๆ ให้ผู้เล่นสังเกตเห็นว่ายิงได้</summary>
    [SerializeField] private bool pulse = true;
    [SerializeField] private Color baseColor = new Color(0.9f, 0.75f, 0.2f);
    [SerializeField] private Color pulseColor = new Color(1f, 1f, 0.8f);
    [SerializeField] private float pulseSpeed = 3f;

    private EnemyHealth health;
    private Transform player;
    private bool isActive;

    private void Awake()
    {
        health = GetComponent<EnemyHealth>();

        if (health != null)
        {
            health.OnDied += DropWeapon;
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (weaponPickupPrefab == null)
        {
            Debug.LogError(
                "WeaponPod: ยังไม่ได้กำหนด Weapon Pickup Prefab"
            );
        }

        if (randomWeapon)
        {
            weaponType = PickRandomWeapon();
        }

        // เริ่มมาปิดไว้ก่อน รอผู้เล่นเข้ามาใกล้ถึงเปิด
        if (requirePlayerNearby && health != null)
        {
            health.SetInvulnerable(true);
        }
    }

    private void Start()
    {
        GameObject playerObject =
            GameObject.FindGameObjectWithTag(playerTag);

        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    /// <summary>
    /// ให้จุดวาง/จุดเกิดกำหนดอาวุธเฉพาะตัวได้ โดยไม่ต้องไปแก้ prefab กลาง
    /// เรียกหลัง Instantiate ได้เลย จะทับค่าสุ่มที่ Awake ตั้งไว้
    /// </summary>
    /// <summary>
    /// ให้จุดเกิดสั่งว่าตัวนี้ปล่อยไอเทม R แทนอาวุธ
    /// เรียกหลัง Instantiate ได้เลย จะทับค่าใน prefab
    /// </summary>
    public void SetDropRapidFire(bool value)
    {
        dropRapidFire = value;
        rapidFireChance = value ? 1f : 0f;

        dropKind = value
            ? WeaponPickup.PickupKind.RapidFire
            : WeaponPickup.PickupKind.Weapon;
    }

    public void SetWeapon(WeaponType type)
    {
        randomWeapon = false;
        weaponType = type;
    }

    /// <summary>สั่งว่าตัวนี้ดรอปอะไร รองรับ Barrier กับ Falcon ด้วย</summary>
    public void SetDropKind(WeaponPickup.PickupKind kind)
    {
        dropKind = kind;

        if (kind != WeaponPickup.PickupKind.Weapon)
        {
            rapidFireChance = 0f;
            dropRapidFire = kind == WeaponPickup.PickupKind.RapidFire;
        }
    }

    private void OnDestroy()
    {
        // ต้องถอนการดักฟังเสมอ ไม่งั้นอ้างถึงวัตถุที่ถูกทำลายไปแล้ว
        if (health != null)
        {
            health.OnDied -= DropWeapon;
        }
    }

    private void Update()
    {
        UpdateActivation();

        if (spriteRenderer == null)
            return;

        // ยังไม่ตื่น = สีหม่นนิ่งๆ ผู้เล่นจะได้รู้ว่ายังยิงไม่เข้า
        if (!isActive)
        {
            spriteRenderer.color = asleepColor;
            return;
        }

        if (!pulse)
        {
            spriteRenderer.color = baseColor;
            return;
        }

        float wave = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;

        spriteRenderer.color = Color.Lerp(baseColor, pulseColor, wave);
    }

    private void UpdateActivation()
    {
        if (!requirePlayerNearby)
        {
            isActive = true;
            return;
        }

        if (player == null || health == null || health.IsDead)
            return;

        float distance = Vector2.Distance(
            transform.position,
            player.position
        );

        bool nearby = distance <= activationRange;

        if (nearby == isActive)
            return;

        isActive = nearby;

        health.SetInvulnerable(!nearby);
    }

    private void DropWeapon()
    {
        if (weaponPickupPrefab == null)
            return;

        GameObject pickupObject = Instantiate(
            weaponPickupPrefab,
            (Vector2)transform.position +
                dropOffset * Mathf.Abs(transform.lossyScale.y),
            Quaternion.identity
        );

        WeaponPickup pickup =
            pickupObject.GetComponent<WeaponPickup>();

        if (pickup == null)
        {
            Debug.LogError(
                "WeaponPod: Weapon Pickup Prefab ไม่มี WeaponPickup.cs"
            );

            Destroy(pickupObject);
            return;
        }

        if (dropKind == WeaponPickup.PickupKind.Barrier ||
            dropKind == WeaponPickup.PickupKind.Falcon)
        {
            pickup.SetKind(dropKind);

            return;
        }

        if (dropRapidFire || Random.value < rapidFireChance)
        {
            pickup.SetRapidFire();
        }
        else
        {
            pickup.SetWeaponType(weaponType);
        }

        if (launchPickup)
        {
            pickup.Launch(GetLaunchVelocity());
        }
    }

    private Vector2 GetLaunchVelocity()
    {
        float side = launchRandomSide
            ? (Random.value < 0.5f ? -1f : 1f)
            : (launchSide >= 0f ? 1f : -1f);

        return new Vector2(
            side * launchSideForce,
            launchUpForce
        );
    }

    /// <summary>สุ่มเฉพาะอาวุธพิเศษ ไม่รวมปืนพื้นฐานที่มีอยู่แล้ว</summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, activationRange);
    }

    private static WeaponType PickRandomWeapon()
    {
        WeaponType[] choices =
        {
            WeaponType.MachineGun,
            WeaponType.SpreadGun,
            WeaponType.Laser,
            WeaponType.FireBall
        };

        return choices[Random.Range(0, choices.Length)];
    }
}
