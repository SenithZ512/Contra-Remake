using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollapsingBridge : MonoBehaviour
{
    [Header("Segments")]
    [SerializeField] private List<BridgeSegment> segments = new List<BridgeSegment>();
    [SerializeField] private bool autoCollectChildren = true;

    [Header("Collapse")]
    [SerializeField] private float startDelay = 0.3f;
    [SerializeField] private float collapseInterval = 0.35f;
    [SerializeField] private bool collapseFromLeft = true;

    [Header("Trigger")]
    [SerializeField] private bool startWhenPlayerSteps = true;
    [SerializeField] private string playerTag = "Player";

    private bool hasStarted;

    public bool HasStarted => hasStarted;

    private void Awake()
    {
        if (autoCollectChildren)
        {
            segments.Clear();
            segments.AddRange(GetComponentsInChildren<BridgeSegment>());
        }

        SortSegments();

        foreach (BridgeSegment segment in segments)
        {
            if (segment != null)
            {
                segment.Initialize(this);
            }
        }
    }

    private void SortSegments()
    {
        segments.RemoveAll(segment => segment == null);

        // เรียงตามแกน X เพื่อให้พังไล่จากปลายด้านหนึ่งไปอีกด้านอย่างเป็นระเบียบ
        segments.Sort((a, b) =>
            a.transform.position.x.CompareTo(b.transform.position.x)
        );

        if (!collapseFromLeft)
        {
            segments.Reverse();
        }
    }

    public void OnSegmentTouched(Collider2D other)
    {
        if (!startWhenPlayerSteps || hasStarted)
            return;

        if (other == null || !other.CompareTag(playerTag))
            return;

        BeginCollapse();
    }

    public void BeginCollapse()
    {
        if (hasStarted)
            return;

        hasStarted = true;

        StartCoroutine(CollapseRoutine());
    }

    private IEnumerator CollapseRoutine()
    {
        if (startDelay > 0f)
        {
            yield return new WaitForSeconds(startDelay);
        }

        foreach (BridgeSegment segment in segments)
        {
            if (segment != null)
            {
                segment.Collapse();
            }

            yield return new WaitForSeconds(collapseInterval);
        }
    }
}
