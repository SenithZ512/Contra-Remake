using UnityEngine;

public class PlayerAnimationFrames : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] frames;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    // เรียกจาก Animation Event ในคลิป Player_Run แต่ละเฟรม
    public void SetFrame(int index)
    {
        if (spriteRenderer == null)
            return;

        if (frames == null || frames.Length == 0)
            return;

        // ฟ้องให้เห็นชัด ไม่งั้นจะถูก clamp เงียบๆ แล้วโชว์ผิดท่าโดยไม่รู้ว่าพัง
        if (index < 0 || index >= frames.Length)
        {
            Debug.LogWarning(
                "PlayerAnimationFrames: ขอเฟรมที่ " + index +
                " แต่ในลิสต์ Frames มีแค่ " + frames.Length + " ช่อง"
            );

            index = Mathf.Clamp(index, 0, frames.Length - 1);
        }

        spriteRenderer.sprite = frames[index];
    }
}
