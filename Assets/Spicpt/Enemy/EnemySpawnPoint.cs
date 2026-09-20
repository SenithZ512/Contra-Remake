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

    [Header("Trigger")]
    [SerializeField] private float triggerDistance = 12f;

    [Header("Limit")]
    [SerializeField] private int maxAlive = 4;

    [Header("Drone Weapon (ใช้เฉพาะตอนปล่อยโดรน)")]
    [SerializeField] private bool overrideDroneWeapon = false;
    [SerializeField] private WeaponType droneWeapon = WeaponType.SpreadGun;

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

        // กล้องเลื่อนไปทางขวาอย่างเดียว จึงเช็คแค่ว่ามาถึงระยะหรือยัง
        if (cameraTransform.position.x + triggerDistance < transform.position.x)
            return;

        hasTriggered = true;

        StartCoroutine(SpawnRoutine());
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
        if (overrideDroneWeapon)
        {
            WeaponDrone drone = enemy.GetComponent<WeaponDrone>();

            if (drone != null)
            {
                drone.SetWeapon(droneWeapon);
            }
        }

        spawned.Add(enemy);
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

        // เส้นบอกว่ากล้องต้องมาถึงตรงไหนถึงจะเริ่มปล่อย
        Vector3 triggerLine =
            transform.position + Vector3.left * triggerDistance;

        Gizmos.DrawLine(
            triggerLine + Vector3.up * 3f,
            triggerLine + Vector3.down * 3f
        );
    }
}
