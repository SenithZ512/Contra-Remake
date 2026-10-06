using System.Collections;
using UnityEngine;

/// <summary>
/// บอสประตูเหล็กท้ายด่าน แบบด่าน 1 ของ Contra
/// ต้องยิงป้อมปืนให้พังทุกตัวก่อน แกนกลางถึงจะรับดาเมจได้
/// พอแกนแตก ประตูจะเปิดออกให้ผ่านไปได้
/// </summary>
public class BossGate : MonoBehaviour
{
    [Header("Parts")]
    [SerializeField] private EnemyHealth[] turrets;
    [SerializeField] private EnemyHealth core;

    [Header("Core Look")]
    [SerializeField] private SpriteRenderer coreRenderer;
    [SerializeField] private Color lockedColor = new Color(0.3f, 0.3f, 0.35f);
    [SerializeField] private Color exposedColor = new Color(1f, 0.3f, 0.25f);

    [Header("Gate")]
    [SerializeField] private Transform door;
    [SerializeField] private float doorOpenDistance = 4f;
    [SerializeField] private float doorOpenSpeed = 2f;

    [Header("Defeat Explosions")]
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private int explosionCount = 6;
    [SerializeField] private float explosionInterval = 0.2f;
    [SerializeField] private float explosionRadius = 2f;

    /// <summary>ยิงตอนบอสตาย เอาไว้ต่อกับระบบจบด่านทีหลัง</summary>
    public event System.Action OnDefeated;

    private int turretsDestroyed;
    private bool coreExposed;
    private bool defeated;

    public bool CoreExposed => coreExposed;
    public bool Defeated => defeated;

    private void Awake()
    {
        if (core == null)
        {
            Debug.LogError("BossGate: ยังไม่ได้กำหนด Core");
            return;
        }

        if (coreRenderer == null)
        {
            coreRenderer = core.GetComponent<SpriteRenderer>();
        }

        // แกนกลางยิงไม่เข้าจนกว่าป้อมจะพังหมด
        core.SetInvulnerable(true);
        core.OnDied += HandleCoreDied;

        ApplyCoreColor();

        if (turrets == null || turrets.Length == 0)
        {
            // ไม่ได้ใส่ป้อมไว้เลย ก็เปิดแกนให้ยิงได้ทันที
            ExposeCore();
            return;
        }

        foreach (EnemyHealth turret in turrets)
        {
            if (turret != null)
            {
                turret.OnDied += HandleTurretDied;
            }
        }
    }

    private void OnDestroy()
    {
        if (core != null)
        {
            core.OnDied -= HandleCoreDied;
        }

        if (turrets == null)
            return;

        foreach (EnemyHealth turret in turrets)
        {
            if (turret != null)
            {
                turret.OnDied -= HandleTurretDied;
            }
        }
    }

    private void HandleTurretDied()
    {
        turretsDestroyed++;

        Debug.Log(
            "ป้อมปืนพังแล้ว " + turretsDestroyed + "/" + turrets.Length
        );

        if (turretsDestroyed < turrets.Length)
            return;

        ExposeCore();
    }

    private void ExposeCore()
    {
        if (coreExposed || core == null)
            return;

        coreExposed = true;

        core.SetInvulnerable(false);

        ApplyCoreColor();

        Debug.Log("แกนกลางเปิดแล้ว ยิงได้!");
    }

    private void ApplyCoreColor()
    {
        if (coreRenderer == null)
            return;

        coreRenderer.color = coreExposed ? exposedColor : lockedColor;
    }

    private void HandleCoreDied()
    {
        if (defeated)
            return;

        defeated = true;

        Debug.Log("บอสพ่ายแพ้ ประตูกำลังเปิด");

        StartCoroutine(DefeatRoutine());
    }

    private IEnumerator DefeatRoutine()
    {
        yield return StartCoroutine(ExplosionBurst());

        yield return StartCoroutine(OpenDoor());

        if (OnDefeated != null)
        {
            OnDefeated();
        }
    }

    private IEnumerator ExplosionBurst()
    {
        if (explosionPrefab == null)
            yield break;

        for (int i = 0; i < explosionCount; i++)
        {
            Vector2 offset = Random.insideUnitCircle * explosionRadius;

            Instantiate(
                explosionPrefab,
                (Vector2)transform.position + offset,
                Quaternion.identity
            );

            yield return new WaitForSeconds(explosionInterval);
        }
    }

    private IEnumerator OpenDoor()
    {
        if (door == null)
            yield break;

        Vector3 start = door.position;
        Vector3 target = start + Vector3.up * doorOpenDistance;

        float travelled = 0f;

        while (travelled < 1f)
        {
            travelled += Time.deltaTime * doorOpenSpeed;

            door.position = Vector3.Lerp(
                start,
                target,
                Mathf.Clamp01(travelled)
            );

            yield return null;
        }

        door.position = target;
    }
}
