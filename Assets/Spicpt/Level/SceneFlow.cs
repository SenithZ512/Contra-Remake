using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ตัวพาไปด่านถัดไป ใช้ร่วมกันทุกด่าน
///
/// ลำดับด่านอ่านจาก Build Settings ตรงๆ ไม่ต้องไปใส่ชื่อฉากซ้ำในแต่ละที่
/// ตอนนี้เรียงเป็น Forest(0) -> Base-1(1) -> WaterFall(2)
///
/// ทำจอดำคั่นให้ด้วย โดยสร้างภาพสีดำขึ้นมาเองตอนรัน
/// ไม่ต้องเตรียมไฟล์ภาพหรือ Canvas ไว้ล่วงหน้า
/// </summary>
public static class SceneFlow
{
    private static bool loading;

    /// <summary>กำลังเปลี่ยนฉากอยู่หรือเปล่า กันสั่งซ้ำตอนเดินทับทริกเกอร์สองที</summary>
    public static bool IsLoading
    {
        get { return loading; }
    }

    /// <summary>
    /// ไปด่านถัดไปตาม Build Settings
    /// sceneName ใส่ไว้ก็ได้ถ้าอยากข้ามลำดับไปฉากที่ระบุ
    /// </summary>
    public static void GoToNext(MonoBehaviour host, float delay,
        float fadeDuration, string sceneName, bool loopAtEnd)
    {
        if (loading || host == null)
            return;

        loading = true;

        host.StartCoroutine(
            Run(delay, fadeDuration, sceneName, loopAtEnd)
        );
    }

    private static IEnumerator Run(float delay, float fadeDuration,
        string sceneName, bool loopAtEnd)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        SpriteRenderer veil = fadeDuration > 0f ? CreateVeil() : null;

        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;

            if (veil != null)
            {
                Color color = veil.color;
                color.a = Mathf.Clamp01(elapsed / fadeDuration);

                veil.color = color;
            }

            yield return null;
        }

        loading = false;

        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
            yield break;
        }

        int next = SceneManager.GetActiveScene().buildIndex + 1;

        if (next >= SceneManager.sceneCountInBuildSettings)
        {
            if (!loopAtEnd)
            {
                Debug.Log("ด่านสุดท้ายแล้ว ไม่มีฉากถัดไปใน Build Settings");
                yield break;
            }

            next = 0;
        }

        SceneManager.LoadScene(next);
    }

    /// <summary>สร้างแผ่นดำคลุมจอขึ้นมาเองตอนรัน ไม่ต้องมีไฟล์ภาพ</summary>
    private static SpriteRenderer CreateVeil()
    {
        Camera view = Camera.main;

        if (view == null)
            return null;

        Texture2D dot = new Texture2D(1, 1);
        dot.SetPixel(0, 0, Color.white);
        dot.Apply();

        Sprite sheet = Sprite.Create(
            dot, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f
        );

        GameObject host = new GameObject("Scene Fade");

        host.transform.SetParent(view.transform, false);
        host.transform.localPosition = new Vector3(0f, 0f, 1f);

        SpriteRenderer veil = host.AddComponent<SpriteRenderer>();

        veil.sprite = sheet;
        veil.color = new Color(0f, 0f, 0f, 0f);
        veil.sortingOrder = 999;

        // เผื่อให้คลุมเกินขอบจอทุกอัตราส่วน
        float height = view.orthographicSize * 2f;

        host.transform.localScale = new Vector3(
            height * view.aspect * 1.4f, height * 1.4f, 1f
        );

        return veil;
    }
}
