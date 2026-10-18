using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public class WeaponDrone : MonoBehaviour
{
    /// <summary>
    /// บินแนวไหน
    ///
    /// ต้นฉบับเลือกจาก LEVEL_SCROLLING_TYPE
    ///   ด่านแนวนอน  บินข้าง  ความเร็ว X = 1.5 พิกเซลต่อเฟรม
    ///   ด่านแนวตั้ง  บินขึ้น  ความเร็ว Y = -1.5 พิกเซลต่อเฟรม
    ///
    /// ใช้ Vertical กับด่าน WaterFall แคปซูลจะลอยขึ้นมาจากข้างล่างแบบต้นฉบับ
    /// </summary>
    public enum FlyAxis
    {
        Horizontal,
        Vertical
    }

    /// <summary>
    /// สั่งทิศบินจากภายนอกหลังเกิด
    /// จุดเกิดแต่ละจุดจะได้ตั้งต่างกันได้ ทั้งที่ใช้พรีแฟบตัวเดียวกัน
    /// </summary>
    public void SetFlight(FlyAxis axis, float direction)
    {
        flyAxis = axis;

        flyDirection = direction >= 0f ? 1f : -1f;
    }

    [Header("Weapon Carried")]
    [SerializeField] private WeaponType weaponType = WeaponType.SpreadGun;
    [SerializeField] private bool randomWeapon = true;

    [Header("Drop")]
    [SerializeField] private GameObject weaponPickupPrefab;
    [SerializeField] private Vector2 dropOffset = Vector2.zero;

    [Header("Rapid Fire (ไอเทม R)")]
    /// <summary>บังคับให้ชิ้นนี้เป็นไอเทมยิงรัวแทนอาวุธ</summary>
    [SerializeField] private bool dropRapidFire;

    /// <summary>
    /// ดรอปอะไรออกมา
    ///
    /// Weapon กับ RapidFire ยังใช้ค่าเดิมข้างบนได้ ไม่กระทบของที่ตั้งไว้แล้ว
    /// Barrier กับ Falcon เป็นของใหม่ ไม่ได้เปลี่ยนปืนที่ผู้เล่นถืออยู่
    /// </summary>
    [SerializeField]
    private WeaponPickup.PickupKind dropKind = WeaponPickup.PickupKind.Weapon;

    /// <summary>โอกาสสุ่มออกเป็นไอเทมยิงรัว 0 = ไม่สุ่มเลย 1 = ออกแน่นอน</summary>
    [Range(0f, 1f)]
    [SerializeField] private float rapidFireChance;

    [Header("Flight")]
    [SerializeField] private FlyAxis flyAxis = FlyAxis.Horizontal;

    /// <summary>1.5 พิกเซลต่อเฟรมของต้นฉบับ = 3.75 หน่วยต่อวินาที</summary>
    [SerializeField] private float moveSpeed = 3.75f;

    /// <summary>
    /// แนวนอน: -1 บินซ้าย, +1 บินขวา
    /// แนวตั้ง: +1 บินขึ้น, -1 บินลง
    /// </summary>
    [SerializeField] private float flyDirection = -1f;
    [SerializeField] private float bobAmplitude = 0.5f;
    [SerializeField] private float bobSpeed = 2f;

    [Header("Lifetime")]
    [SerializeField] private float lifetime = 20f;

    private EnemyHealth health;
    private Vector2 origin;
    private float elapsed;

    private void Awake()
    {
        health = GetComponent<EnemyHealth>();

        if (health != null)
        {
            health.OnDied += DropWeapon;
        }

        if (weaponPickupPrefab == null)
        {
            Debug.LogError(
                "WeaponDrone: ยังไม่ได้กำหนด Weapon Pickup Prefab"
            );
        }

        if (randomWeapon)
        {
            weaponType = PickRandomWeapon();
        }

        origin = transform.position;
    }

    /// <summary>
    /// ให้จุดเกิดกำหนดอาวุธเฉพาะตัวได้ โดยไม่ต้องไปแก้ prefab กลาง
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

        dropKind = WeaponPickup.PickupKind.Weapon;
    }

    /// <summary>
    /// สั่งว่าตัวนี้ดรอปอะไร รองรับ Barrier กับ Falcon ด้วย
    /// เรียกหลัง Instantiate ได้เลย จะทับค่าใน prefab
    /// </summary>
    public void SetDropKind(WeaponPickup.PickupKind kind)
    {
        dropKind = kind;

        // สองอันนี้ไม่ได้เปลี่ยนปืน จึงต้องปิดการสุ่มอาวุธทิ้ง
        if (kind != WeaponPickup.PickupKind.Weapon)
        {
            randomWeapon = false;
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

    private void Start()
    {
        if (lifetime > 0f)
        {
            Destroy(gameObject, lifetime);
        }
    }

    private void Update()
    {
        elapsed += Time.deltaTime;

        float direction = flyDirection >= 0f ? 1f : -1f;

        float step = direction * moveSpeed * Time.deltaTime;

        float wave = Mathf.Sin(elapsed * bobSpeed) * bobAmplitude;

        // คลื่นต้องตั้งฉากกับทางที่บินเสมอ ตามที่ต้นฉบับแยก
        // set_flying_capsule_y_vel กับ set_flying_capsule_x_vel ไว้
        if (flyAxis == FlyAxis.Vertical)
        {
            // บินขึ้น ส่ายซ้ายขวา
            origin += new Vector2(0f, step);

            transform.position = origin + new Vector2(wave, 0f);

            return;
        }

        // บินข้าง ส่ายขึ้นลง
        origin += new Vector2(step, 0f);

        transform.position = origin + new Vector2(0f, wave);
    }

    private void DropWeapon()
    {
        if (weaponPickupPrefab == null)
            return;

        GameObject pickupObject = Instantiate(
            weaponPickupPrefab,
            (Vector2)transform.position + dropOffset,
            Quaternion.identity
        );

        WeaponPickup pickup =
            pickupObject.GetComponent<WeaponPickup>();

        if (pickup == null)
        {
            Debug.LogError(
                "WeaponDrone: Weapon Pickup Prefab ไม่มี WeaponPickup.cs"
            );

            Destroy(pickupObject);
            return;
        }

        // ถูกสั่งมาเป็น Barrier หรือ Falcon ก็จบตรงนี้ ไม่ต้องไปยุ่งกับอาวุธ
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
    }

    /// <summary>สุ่มเฉพาะอาวุธพิเศษ ไม่รวมปืนพื้นฐานที่มีอยู่แล้ว</summary>
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
