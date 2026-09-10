using System.Collections;
using UnityEngine;

public class BridgeSegment : MonoBehaviour
{
    [Header("Shake (สั่นเตือนก่อนพัง)")]
    [SerializeField] private float shakeDuration = 0.25f;
    [SerializeField] private float shakeDistance = 0.06f;

    [Header("Fall")]
    [SerializeField] private float fallGravityScale = 2.5f;
    [SerializeField] private float torque = 40f;
    [SerializeField] private float destroyDelay = 2f;

    private Rigidbody2D rb;
    private Collider2D bodyCollider;
    private CollapsingBridge bridge;
    private bool isCollapsing;

    public bool IsCollapsing => isCollapsing;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();

        if (rb == null)
        {
            Debug.LogError(
                "BridgeSegment: ต้องมี Rigidbody2D (ตั้งเป็น Static ไว้)"
            );
        }

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

        // ปิด collider ก่อน ผู้เล่นที่ยืนอยู่จะได้ร่วงลงทันที
        if (bodyCollider != null)
        {
            bodyCollider.enabled = false;
        }

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = fallGravityScale;
            rb.AddTorque(Random.Range(-torque, torque));
        }

        Destroy(gameObject, destroyDelay);
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
