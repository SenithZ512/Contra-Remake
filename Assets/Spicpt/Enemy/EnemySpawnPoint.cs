using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// จุดเกิดศัตรูที่ผูกกับระยะเลื่อนของกล้อง แบบเดียวกับ Contra ต้นฉบับ
/// พอกล้องเลื่อนมาใกล้ถึงระยะที่ตั้งไว้ จะปล่อยศัตรูออกมา ทำงานครั้งเดียวไม่ซ้ำ
/// </summary>
public class EnemySpawnPoint : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Vector2 spawnOffset = Vector2.zero;
    [SerializeField] private int spawnCount = 1;
    [SerializeField] private float spawnInterval = 1.5f;

    /// <summary>
    /// กล้องของด่านนี้เลื่อนไปทางไหน
    ///
    /// ด่านแนวนอนอย่าง Forest กล้องไปทางขวา เช็คแกน X
    /// ด่านแนวตั้งอย่าง WaterFall กล้องไต่ขึ้น ต้องเช็คแกน Y แทน
    /// ถ้าใช้ผิดแกน จุดเกิดจะไม่ทำงานเลยหรือทำงานทันทีตั้งแต่เริ่มด่าน
    /// </summary>
    public enum TriggerAxis
    {
        CameraMovesRight,
        CameraMovesUp
    }

    [Header("Trigger")]
    [SerializeField] private TriggerAxis triggerAxis =
        TriggerAxis.CameraMovesRight;

    [SerializeField] private float triggerDistance = 12f;

    [Header("Limit")]
    [SerializeField] private int maxAlive = 4;

    [Header("Drop (ใช้กับ WeaponDrone และ WeaponPod)")]
    [SerializeField] private bool overrideDroneWeapon = false;
    [SerializeField] private WeaponType droneWeapon = WeaponType.SpreadGun;

    /// <summary>ให้จุดนี้ปล่อยไอเทมเพิ่มความเร็วยิงแทนอาวุธ</summary>
    [SerializeField] private bool dropRapidFire = false;

    /// <summary>
    /// ดรอปของพิเศษแทนอาวุธ
    ///
    /// Barrier = เกราะอมตะราว 17 วินาที (19.2 ในด่าน Hangar) ไม่เปลี่ยนปืนที่ถืออยู่
    /// Falcon  = ล้างศัตรูทั้งจอทันทีที่แตะ
    ///
    /// แยกสวิตช์จาก Override Drone Weapon เพราะสองอันนั้นเป็นเรื่องอาวุธ
    /// ส่วนอันนี้ไม่ได้ยุ่งกับปืนเลย
    /// </summary>
    [SerializeField] private bool overrideDropKind = false;

    [SerializeField] private WeaponPickup.PickupKind dropKind =
        WeaponPickup.PickupKind.Barrier;

    [Header("Drone Flight (สั่งทิศบินเฉพาะจุดนี้)")]
    /// <summary>
    /// สั่งทับทิศบินของโดรนที่เกิดจากจุดนี้
    ///
    /// ต้นฉบับเลือกจากชนิดการเลื่อนของด่าน แคปซูลในด่านแนวตั้งลอยขึ้น
    /// ของเราใช้พรีแฟบโดรนตัวเดียวทุกด่าน เลยสั่งทับที่จุดเกิดแทน
    /// </summary>
    [SerializeField] private bool overrideDroneFlight = false;

    [SerializeField] private WeaponDrone.FlyAxis droneFlyAxis =
        WeaponDrone.FlyAxis.Vertical;

    /// <summary>แนวตั้ง: 1 = ลอยขึ้น / แนวนอน: 1 = บินขวา, -1 = บินซ้าย</summary>
    [SerializeField] private float droneFlyDirection = 1f;

    [Header("Runner Behaviour (ใช้กับ EnemyRunner)")]
    /// <summary>สั่งทับพฤติกรรมให้จุดเกิดนี้ ต่างจาก prefab กลางได้</summary>
    [SerializeField] private bool overrideRunnerBehaviour = false;
    [SerializeField] private EnemyRunner.RunMode runMode =
        EnemyRunner.RunMode.LockOnSpawn;
    [SerializeField] private EnemyRunner.EdgeBehaviour edgeBehaviour =
        EnemyRunner.EdgeBehaviour.TurnAround;

    [Header("Shooting Behaviour (ใช้กับ EnemyStopAndShoot)")]
    /// <summary>
    /// สั่งทับว่าทหารจากจุดนี้หยุดยิงหรือวิ่งผ่านเฉยๆ
    ///
    /// ต้นฉบับเก็บไว้ในบิต 2 ของ ENEMY_ATTRIBUTES ของทหารแต่ละตัว
    /// ทหารตัวเดียวกัน แต่จุดเกิดต่างกันทำให้พฤติกรรมต่างกันได้
    /// </summary>
    [SerializeField] private bool overrideShooting = false;

    /// <summary>ปิดแล้วจะวิ่งผ่านอย่างเดียว ไม่หยุดยิงเลย</summary>
    [SerializeField] private bool canShoot = true;

    /// <summary>ยิงกี่นัดต่อชุด x = ต่ำสุด y = สูงสุด</summary>
    [SerializeField] private Vector2Int burstBullets = new Vector2Int(1, 3);

    /// <summary>วิ่งนานแค่ไหนก่อนหยุดยิงรอบถัดไป 0 = ใช้ค่าใน prefab</summary>
    [SerializeField] private float runTimeBetweenBursts = 0f;

    private Transform cameraTransform;
    private readonly List<GameObject> spawned = new List<GameObject>();
    private bool hasTriggered;

    private void Awake()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError(
                "EnemySpawnPoint: ยังไม่ได้กำหนด Enemy Prefab"
            );
        }
    }

    private void Start()
    {
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void Update()
    {
        if (hasTriggered || cameraTransform == null)
            return;

        if (!HasCameraArrived())
            return;

        hasTriggered = true;

        StartCoroutine(SpawnRoutine());
    }

    /// <summary>กล้องเลื่อนมาถึงระยะที่ตั้งไว้หรือยัง เช็คตามแกนที่เลือก</summary>
    private bool HasCameraArrived()
    {
        if (triggerAxis == TriggerAxis.CameraMovesUp)
        {
            return cameraTransform.position.y + triggerDistance >=
                transform.position.y;
        }

        return cameraTransform.position.x + triggerDistance >=
            transform.position.x;
    }

    private IEnumerator SpawnRoutine()
    {
        for (int i = 0; i < Mathf.Max(1, spawnCount); i++)
        {
            // เต็มโควตาแล้วรอก่อน แบบเดียวกับที่ต้นฉบับจำกัดจำนวนศัตรูพร้อมกัน
            while (CountAlive() >= maxAlive)
            {
                yield return null;
            }

            Spawn();

            if (spawnInterval > 0f)
            {
                yield return new WaitForSeconds(spawnInterval);
            }
        }
    }

    private void Spawn()
    {
        if (enemyPrefab == null)
            return;

        GameObject enemy = Instantiate(
            enemyPrefab,
            (Vector2)transform.position + spawnOffset,
            Quaternion.identity
        );

        // สั่งทับค่าเฉพาะตัวหลังเกิด ทำให้จุดเกิดแต่ละจุดต่างกันได้ทั้งที่ใช้ prefab ตัวเดียวกัน
        ApplyDropOverride(enemy);
        ApplyDroneFlightOverride(enemy);
        ApplyRunnerOverride(enemy);
        ApplyShootingOverride(enemy);

        spawned.Add(enemy);
    }

    /// <summary>รองรับทั้งโดรนและป้อมอาวุธ ตัวไหนไม่มีก็ข้ามไป</summary>
    private void ApplyDropOverride(GameObject enemy)
    {
        // ของพิเศษมาก่อน เพราะไม่เกี่ยวกับอาวุธเลย
        if (overrideDropKind)
        {
            WeaponDrone specialDrone = enemy.GetComponent<WeaponDrone>();

            if (specialDrone != null)
            {
                specialDrone.SetDropKind(dropKind);
            }

            WeaponPod specialPod = enemy.GetComponent<WeaponPod>();

            if (specialPod != null)
            {
                specialPod.SetDropKind(dropKind);
            }

            return;
        }

        if (!overrideDroneWeapon && !dropRapidFire)
            return;

        WeaponDrone drone = enemy.GetComponent<WeaponDrone>();

        if (drone != null)
        {
            if (dropRapidFire)
            {
                drone.SetDropRapidFire(true);
            }
            else
            {
                drone.SetWeapon(droneWeapon);
            }
        }

        WeaponPod pod = enemy.GetComponent<WeaponPod>();

        if (pod != null)
        {
            if (dropRapidFire)
            {
                pod.SetDropRapidFire(true);
            }
            else
            {
                pod.SetWeapon(droneWeapon);
            }
        }
    }

    /// <summary>สั่งทิศบินให้โดรน ตัวที่ไม่ใช่โดรนก็ข้ามไป</summary>
    private void ApplyDroneFlightOverride(GameObject enemy)
    {
        if (!overrideDroneFlight)
            return;

        WeaponDrone drone = enemy.GetComponent<WeaponDrone>();

        if (drone == null)
            return;

        drone.SetFlight(droneFlyAxis, droneFlyDirection);
    }

    private void ApplyRunnerOverride(GameObject enemy)
    {
        if (!overrideRunnerBehaviour)
            return;

        EnemyRunner runner = enemy.GetComponent<EnemyRunner>();

        if (runner == null)
            return;

        runner.SetRunMode(runMode);
        runner.SetEdgeBehaviour(edgeBehaviour);
    }

    /// <summary>ตัวที่วิ่งไม่ได้หรือยิงไม่ได้ ไม่มีคอมโพเนนต์นี้ ก็ข้ามไป</summary>
    private void ApplyShootingOverride(GameObject enemy)
    {
        if (!overrideShooting)
            return;

        EnemyStopAndShoot gunner = enemy.GetComponent<EnemyStopAndShoot>();

        if (gunner == null)
            return;

        gunner.Configure(
            canShoot,
            burstBullets.x,
            burstBullets.y,
            runTimeBetweenBursts
        );
    }

    private int CountAlive()
    {
        spawned.RemoveAll(enemy => enemy == null);

        return spawned.Count;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 spawnPosition =
            transform.position + (Vector3)spawnOffset;

        Gizmos.DrawWireSphere(spawnPosition, 0.4f);

        // เส้นบอกว่ากล้องต้องมาถึงตรงไหนถึงจะเริ่มปล่อย วาดตามแกนที่เลือก
        if (triggerAxis == TriggerAxis.CameraMovesUp)
        {
            Vector3 line =
                transform.position + Vector3.down * triggerDistance;

            Gizmos.DrawLine(
                line + Vector3.left * 4f,
                line + Vector3.right * 4f
            );

            return;
        }

        Vector3 triggerLine =
            transform.position + Vector3.left * triggerDistance;

        Gizmos.DrawLine(
            triggerLine + Vector3.up * 3f,
            triggerLine + Vector3.down * 3f
        );
    }
}
