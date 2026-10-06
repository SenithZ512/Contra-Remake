using UnityEngine;

public class WeaponSystem : MonoBehaviour
{
    [Header("Current Weapon")]
    [SerializeField] private WeaponType currentWeapon = WeaponType.Normal;

    [Header("Fire Rate (วินาทีต่อนัด ยิ่งมากยิ่งยิงช้า)")]
    [SerializeField] private float normalFireRate = 0.55f;
    [SerializeField] private float machineGunFireRate = 0.14f;
    [SerializeField] private float spreadGunFireRate = 0.34f;
    [SerializeField] private float laserFireRate = 0.41f;
    [SerializeField] private float fireBallFireRate = 0.28f;

    /// <summary>กันตั้งเป็น 0 แล้วยิงรัวจนเกมค้าง</summary>
    [SerializeField] private float minimumFireRate = 0.03f;

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

    public void SetWeapon(WeaponType newWeapon)
    {
        currentWeapon = newWeapon;

        Debug.Log(
            "Weapon changed to: " + currentWeapon
        );
    }
}