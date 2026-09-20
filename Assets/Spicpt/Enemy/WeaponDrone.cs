using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public class WeaponDrone : MonoBehaviour
{
    [Header("Weapon Carried")]
    [SerializeField] private WeaponType weaponType = WeaponType.SpreadGun;
    [SerializeField] private bool randomWeapon = true;

    [Header("Drop")]
    [SerializeField] private GameObject weaponPickupPrefab;
    [SerializeField] private Vector2 dropOffset = Vector2.zero;

    [Header("Flight")]
    [SerializeField] private float moveSpeed = 3f;
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
    public void SetWeapon(WeaponType type)
    {
        randomWeapon = false;
        weaponType = type;
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

        // บินตรงไปข้างหน้าพร้อมส่ายขึ้นลงเป็นคลื่น
        origin += new Vector2(
            direction * moveSpeed * Time.deltaTime,
            0f
        );

        transform.position = origin + new Vector2(
            0f,
            Mathf.Sin(elapsed * bobSpeed) * bobAmplitude
        );
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

        pickup.SetWeaponType(weaponType);
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
