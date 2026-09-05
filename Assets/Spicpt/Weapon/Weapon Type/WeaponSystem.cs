using UnityEngine;

public class WeaponSystem : MonoBehaviour
{
    [Header("Current Weapon")]
    [SerializeField] private WeaponType currentWeapon = WeaponType.Normal;

    [Header("Fire Rate")]
    [SerializeField] private float normalFireRate = 0.20f;
    [SerializeField] private float machineGunFireRate = 0.08f;

    public WeaponType CurrentWeapon => currentWeapon;

    public float CurrentFireRate
    {
        get
        {
            switch (currentWeapon)
            {
                case WeaponType.MachineGun:
                    return machineGunFireRate;

                case WeaponType.SpreadGun:
                    return 0.25f;

                case WeaponType.Laser:
                    return 0.30f;

                case WeaponType.FireBall:
                    return 0.20f;

                default:
                    return normalFireRate;
            }
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