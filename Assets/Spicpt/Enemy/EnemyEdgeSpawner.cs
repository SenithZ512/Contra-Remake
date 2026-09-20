using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ปล่อยศัตรูจากนอกจอฝั่งขวาเข้ามาเป็นระลอก แบบทหารวิ่งใน Contra
/// ผู้เล่นจะไม่เห็นมันโผล่มาเฉยๆ กลางจอ เพราะเกิดนอกขอบจอเสมอ
/// </summary>
public class EnemyEdgeSpawner : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private float spawnInterval = 3f;
    [SerializeField] private float firstSpawnDelay = 2f;
    [SerializeField] private float spawnMargin = 2f;

    [Header("Spawn Height")]
    [SerializeField] private bool useOwnHeight = true;
    [SerializeField] private float spawnY = 0f;

    [Header("Limit")]
    [SerializeField] private int maxAlive = 3;

    [Header("Active Range (ช่วงที่กล้องต้องอยู่ถึงจะปล่อย)")]
    [SerializeField] private float activeFromX = -999f;
    [SerializeField] private float activeToX = 999f;

    private Camera mainCamera;
    private readonly List<GameObject> spawned = new List<GameObject>();
    private float nextSpawnTime;

    private void Awake()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError(
                "EnemyEdgeSpawner: ยังไม่ได้กำหนด Enemy Prefab"
            );
        }
    }

    private void Start()
    {
        mainCamera = Camera.main;

        nextSpawnTime = Time.time + firstSpawnDelay;
    }

    private void Update()
    {
        if (mainCamera == null || enemyPrefab == null)
            return;

        float cameraX = mainCamera.transform.position.x;

        if (cameraX < activeFromX || cameraX > activeToX)
            return;

        if (Time.time < nextSpawnTime)
            return;

        if (CountAlive() >= maxAlive)
            return;

        nextSpawnTime = Time.time + spawnInterval;

        Spawn(cameraX);
    }

    private void Spawn(float cameraX)
    {
        float halfWidth =
            mainCamera.orthographicSize * mainCamera.aspect;

        float x = cameraX + halfWidth + spawnMargin;
        float y = useOwnHeight ? transform.position.y : spawnY;

        GameObject enemy = Instantiate(
            enemyPrefab,
            new Vector3(x, y, 0f),
            Quaternion.identity
        );

        spawned.Add(enemy);
    }

    private int CountAlive()
    {
        spawned.RemoveAll(enemy => enemy == null);

        return spawned.Count;
    }

    private void OnDrawGizmosSelected()
    {
        // เส้นระดับความสูงที่ศัตรูจะเกิด
        float y = useOwnHeight ? transform.position.y : spawnY;

        Gizmos.DrawLine(
            new Vector3(activeFromX, y, 0f),
            new Vector3(activeToX, y, 0f)
        );
    }
}
