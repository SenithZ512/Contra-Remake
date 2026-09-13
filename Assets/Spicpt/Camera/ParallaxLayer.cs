using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    [Header("Parallax")]
    [Range(0f, 1f)]
    [SerializeField] private float parallaxFactor = 0.5f;
    [SerializeField] private bool followVertical = false;

    [Header("Infinite Scroll")]
    [SerializeField] private bool infiniteHorizontal = true;

    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;

    private Vector3 lastCameraPosition;
    private float tileWidth;

    private void Start()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (cameraTransform == null)
        {
            Debug.LogError(
                "ParallaxLayer: ไม่พบกล้อง (ต้องมีกล้องที่ติด Tag MainCamera)"
            );

            return;
        }

        lastCameraPosition = cameraTransform.position;

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            // ความกว้างของลาย 1 รอบ ใช้ตอนวาร์ปกลับ จะได้ต่อเนียนไม่มีรอยต่อ
            tileWidth =
                spriteRenderer.sprite.texture.width /
                spriteRenderer.sprite.pixelsPerUnit;
        }
    }

    private void LateUpdate()
    {
        if (cameraTransform == null)
            return;

        Vector3 cameraDelta =
            cameraTransform.position - lastCameraPosition;

        lastCameraPosition = cameraTransform.position;

        // factor 1 = เลื่อนตามกล้องเป๊ะ (ดูไกลสุด), 0 = อยู่นิ่งกับที่ (ดูใกล้สุด)
        float moveY = followVertical
            ? cameraDelta.y * parallaxFactor
            : 0f;

        transform.position += new Vector3(
            cameraDelta.x * parallaxFactor,
            moveY,
            0f
        );

        if (infiniteHorizontal)
        {
            WrapHorizontally();
        }
    }

    private void WrapHorizontally()
    {
        if (tileWidth <= 0f)
            return;

        float distance =
            cameraTransform.position.x - transform.position.x;

        if (Mathf.Abs(distance) < tileWidth)
            return;

        // เลื่อนไปทีละ 1 รอบลาย ภาพที่เห็นจึงเหมือนเดิมทุกประการ
        float offset = distance % tileWidth;

        transform.position = new Vector3(
            cameraTransform.position.x - offset,
            transform.position.y,
            transform.position.z
        );
    }
}
