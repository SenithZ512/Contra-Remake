using UnityEngine;

/// <summary>
/// ไอเท็มอาวุธที่ทหารแดงทำหล่น
///
/// ในด่านฐานไม่มีแคปซูลหรือ Sensor ให้ยิง ทหารแดงคือแหล่งอาวุธแหล่งเดียว
/// ต้นฉบับให้ไอเท็มบินจากผนังหลังเข้ามาหาเราช้าๆ ไม่ได้โผล่ตรงหน้า
///
/// ข้อควรระวังที่ต้นฉบับก็มี: มันไม่หายไปเอง ถ้าอาวุธที่ได้ไม่ถูกใจ
/// มันจะลอยเกะกะกินพื้นที่หลบอยู่อย่างนั้นจนกว่าจะเปลี่ยนห้อง
/// </summary>
public class BaseWeaponItem : MonoBehaviour
{
    [Header("Weapon")]
    [SerializeField] private WeaponType weaponType = WeaponType.MachineGun;

    /// <summary>เป็นไอเท็มเพิ่มอัตราการยิงแทนที่จะเป็นปืน</summary>
    [SerializeField] private bool rapidFire;

    [Header("Flight")]
    /// <summary>ลอยเข้าหาหน้าจอเร็วแค่ไหน</summary>
    [SerializeField] private float approachSpeed = 2.2f;

    /// <summary>ส่ายซ้ายขวาระหว่างลอยมา</summary>
    [SerializeField] private float swayAmplitude = 0.5f;

    [SerializeField] private float swaySpeed = 2f;

    [Header("Rules")]
    /// <summary>ลอยถึงแนวพื้นแล้วค้างอยู่ตรงนั้น ไม่หายไปเอง เหมือนต้นฉบับ</summary>
    [SerializeField] private bool stopAtFloor = true;

    [Header("Sprites")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float blinkSpeed = 5f;

    private float startX;
    private float floorY;

    public void Setup(WeaponType type, bool isRapidFire)
    {
        weaponType = type;
        rapidFire = isRapidFire;
    }

    private void Start()
    {
        startX = transform.position.x;
        floorY = BaseStageGeometry.FloorY;

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }

    private void Update()
    {
        float y = transform.position.y;

        if (!stopAtFloor || y > floorY)
        {
            y -= approachSpeed * Time.deltaTime;
        }

        transform.position = new Vector3(
            startX + Mathf.Sin(Time.time * swaySpeed) * swayAmplitude,
            Mathf.Max(y, floorY),
            transform.position.z
        );

        // ขยายตามระยะเหมือนของอื่นในด่าน จะได้รู้ว่ามันลอยเข้ามาใกล้แล้ว
        float scale = BaseStageGeometry.ScaleAt(transform.position.y);

        transform.localScale = new Vector3(scale, scale, 1f);

        Blink();
    }

    private void Blink()
    {
        if (spriteRenderer == null)
            return;

        float wave = (Mathf.Sin(Time.time * blinkSpeed) + 1f) * 0.5f;

        spriteRenderer.color = Color.Lerp(Color.white, Color.yellow, wave);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        BasePlayer player = other.GetComponentInParent<BasePlayer>();

        if (player == null)
            return;

        WeaponSystem weapons = player.GetComponent<WeaponSystem>();

        if (weapons != null)
        {
            if (rapidFire)
            {
                weapons.EnableRapidFire();
            }
            else
            {
                weapons.SetWeapon(weaponType);
            }
        }

        Destroy(gameObject);
    }
}
