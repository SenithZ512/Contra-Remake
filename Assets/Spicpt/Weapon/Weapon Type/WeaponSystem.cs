using UnityEngine;

public class WeaponSystem : MonoBehaviour
{
    [Header("Current Weapon")]
    [SerializeField] private WeaponType currentWeapon = WeaponType.Normal;

    [Header("Fire Rate (วินาทีต่อนัด ยิ่งมากยิ่งยิงช้า)")]
    /// <summary>
    /// ต้นฉบับแทบไม่ได้ใช้ตัวจับเวลาคุมปืนที่กดทีละนัด
    /// ตัวที่คุมจริงคือโควตากระสุนบนจอ (ดู PlayerCombat)
    /// ค่าพวกนี้เลยตั้งต่ำไว้เป็นแค่กันกดรัวจนเกมสะดุด
    /// </summary>
    [SerializeField] private float normalFireRate = 0.1f;

    /// <summary>ต้นฉบับยิงทุก 8 เฟรม ตอนกดค้าง</summary>
    [SerializeField] private float machineGunFireRate = 0.133f;

    [SerializeField] private float spreadGunFireRate = 0.12f;
    [SerializeField] private float laserFireRate = 0.41f;
    [SerializeField] private float fireBallFireRate = 0.2f;

    /// <summary>กันตั้งเป็น 0 แล้วยิงรัวจนเกมค้าง</summary>
    [SerializeField] private float minimumFireRate = 0.03f;

    [Header("Rapid Fire (ไอเทมเพิ่มความเร็วยิง)")]
    /// <summary>คูณช่วงเวลาระหว่างนัด 0.5 = ยิงเร็วขึ้นเท่าตัว</summary>
    [SerializeField] private float rapidFireMultiplier = 0.5f;

    private bool rapidFire;

    public bool RapidFire => rapidFire;

    public WeaponType CurrentWeapon => currentWeapon;

    public float CurrentFireRate
    {
        get
        {
            float rate;

            switch (currentWeapon)
            {
                case WeaponType.MachineGun:
                    rate = machineGunFireRate;
                    break;

                case WeaponType.SpreadGun:
                    rate = spreadGunFireRate;
                    break;

                case WeaponType.Laser:
                    rate = laserFireRate;
                    break;

                case WeaponType.FireBall:
                    rate = fireBallFireRate;
                    break;

                default:
                    rate = normalFireRate;
                    break;
            }

            if (rapidFire)
            {
                rate *= rapidFireMultiplier;
            }

            return Mathf.Max(minimumFireRate, rate);
        }
    }

    private void Update()
    {
        HandleDebugWeaponSwitch();
    }

    private void HandleDebugWeaponSwitch()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SetWeapon(WeaponType.Normal);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SetWeapon(WeaponType.MachineGun);
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SetWeapon(WeaponType.SpreadGun);
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            SetWeapon(WeaponType.Laser);
        }

        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            SetWeapon(WeaponType.FireBall);
        }
    }

    /// <summary>ติดไอเทม R แล้วยิงเร็วขึ้น ติดค้างข้ามการเปลี่ยนอาวุธ</summary>
    public void EnableRapidFire()
    {
        rapidFire = true;

        Debug.Log("Rapid Fire ON");
    }

    /// <summary>หลุดตอนตาย เหมือนต้นฉบับ</summary>
    public void DisableRapidFire()
    {
        rapidFire = false;
    }

    public void SetWeapon(WeaponType newWeapon)
    {
        currentWeapon = newWeapon;

        Debug.Log(
            "Weapon changed to: " + currentWeapon
        );
    }
}