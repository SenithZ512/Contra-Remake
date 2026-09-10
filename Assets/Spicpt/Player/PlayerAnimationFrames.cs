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

        index = Mathf.Clamp(index, 0, frames.Length - 1);

        spriteRenderer.sprite = frames[index];
    }
}
