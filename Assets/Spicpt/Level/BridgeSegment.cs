using System.Collections;
using UnityEngine;

public class BridgeSegment : MonoBehaviour
{
    [Header("Shake (สั่นเตือนก่อนระเบิด)")]
    [SerializeField] private float shakeDuration = 0.15f;
    [SerializeField] private float shakeDistance = 0.06f;

    [Header("Explosion")]
    [SerializeField] private GameObject explosionPrefab;

    private Collider2D bodyCollider;
    private CollapsingBridge bridge;
    private bool isCollapsing;

    public bool IsCollapsing => isCollapsing;

    private void Awake()
    {
        bodyCollider = GetComponent<Collider2D>();

        if (bodyCollider == null)
        {
            Debug.LogError(
                "BridgeSegment: ต้องมี Collider2D"
            );
        }
    }

    public void Initialize(CollapsingBridge owner)
    {
        bridge = owner;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (bridge == null)
            return;

        // บอกสะพานว่ามีอะไรมาเหยียบ แล้วให้สะพานตัดสินใจว่าเริ่มพังไหม
        bridge.OnSegmentTouched(collision.collider);
    }

    public void Collapse()
    {
        if (isCollapsing)
            return;

        isCollapsing = true;

        StartCoroutine(CollapseRoutine());
    }

    private IEnumerator CollapseRoutine()
    {
        yield return StartCoroutine(ShakeRoutine());

        // ระเบิดต้องไม่ถูกทำลายไปพร้อมท่อนสะพาน จึงไม่ผูกเป็นลูก
        if (explosionPrefab != null)
        {
            Instantiate(
                explosionPrefab,
                transform.position,
                Quaternion.identity
            );
        }

        // ท่อนหายทันที ผู้เล่นที่ยืนอยู่จะร่วงลงตรงนั้นเลย
        Destroy(gameObject);
    }

    private IEnumerator ShakeRoutine()
    {
        if (shakeDuration <= 0f)
            yield break;

        Vector3 originalPosition = transform.localPosition;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            transform.localPosition =
                originalPosition +
                new Vector3(
                    Random.Range(-shakeDistance, shakeDistance),
                    Random.Range(-shakeDistance, shakeDistance),
                    0f
                );

            elapsed += Time.deltaTime;

            yield return null;
        }

        transform.localPosition = originalPosition;
    }
}
