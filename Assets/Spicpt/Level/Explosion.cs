using UnityEngine;

public class Explosion : MonoBehaviour
{
    [Header("Explosion")]
    [SerializeField] private float duration = 0.35f;
    [SerializeField] private float startScale = 0.4f;
    [SerializeField] private float endScale = 1.8f;

    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Color startColor;
    private float elapsed;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer != null)
        {
            startColor = spriteRenderer.color;
        }

        transform.localScale = Vector3.one * startScale;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;

        float progress = duration <= 0f
            ? 1f
            : Mathf.Clamp01(elapsed / duration);

        // ระเบิดพองออกแล้วจางหายไป
        transform.localScale =
            Vector3.one * Mathf.Lerp(startScale, endScale, progress);

        if (spriteRenderer != null)
        {
            Color color = startColor;
            color.a = Mathf.Lerp(startColor.a, 0f, progress);

            spriteRenderer.color = color;
        }

        if (progress >= 1f)
        {
            Destroy(gameObject);
        }
    }
}
