using UnityEngine;

/// <summary>
/// ลบวัตถุที่หลุดออกหลังจอหรือตกลงไปใต้ด่าน
/// จำเป็นเพราะกล้องเกมนี้ไม่มีวันถอยกลับ ของที่ผ่านไปแล้วจะค้างสะสมเปล่าๆ
/// </summary>
public class DespawnBehindCamera : MonoBehaviour
{
    [Header("Behind Camera")]
    [SerializeField] private float margin = 4f;

    [Header("Fell Off Level")]
    [SerializeField] private bool despawnWhenFalling = true;
    [SerializeField] private float minY = -20f;

    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (despawnWhenFalling && transform.position.y < minY)
        {
            Destroy(gameObject);
            return;
        }

        if (mainCamera == null)
            return;

        float halfWidth =
            mainCamera.orthographicSize * mainCamera.aspect;

        float leftEdge =
            mainCamera.transform.position.x - halfWidth;

        if (transform.position.x < leftEdge - margin)
        {
            Destroy(gameObject);
        }
    }
}
