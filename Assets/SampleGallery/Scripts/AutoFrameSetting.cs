using UnityEngine;

public class AutoFrameSetting : MonoBehaviour
{
    [Header("Dimensions (in inches)")]
    [SerializeField] private float frameWidth;
    [SerializeField] private float frameDepth;
    [SerializeField] private float bleed;

    [Header("Frame Transforms")]
    [SerializeField] private Transform left;
    [SerializeField] private Transform right;
    [SerializeField] private Transform top;
    [SerializeField] private Transform bottom;

    private const float OneInch = 0.0254f;

    private Transform painting;
    private MeshRenderer paintingRenderer;

    private float frameWidthM;
    private float frameDepthM;
    private float bleedM;

    private void SetFrame()
    {
        painting = transform;
        paintingRenderer = GetComponent<MeshRenderer>();

        if (!paintingRenderer)
        {
            Debug.LogError("AutoFrameSetting: Painting has no MeshRenderer.", this);
            return;
        }

        frameWidthM = frameWidth * OneInch;
        frameDepthM = frameDepth * OneInch;
        bleedM = bleed * OneInch;

        SetPositions();
        SetScale();
    }

    // ----------------------------------------------------
    // POSITION (local-axis based, rotation safe)
    // ----------------------------------------------------
    private void SetPositions()
    {
        Bounds local = paintingRenderer.localBounds;

        float halfHeightWorld = local.extents.y * painting.lossyScale.y;
        float halfWidthWorld = local.extents.z * painting.lossyScale.z;

        Vector3 center = painting.position;

        left.position =
            center - painting.forward * (halfWidthWorld + bleedM);

        right.position =
            center + painting.forward * (halfWidthWorld + bleedM);

        bottom.position =
            center - painting.up * (halfHeightWorld + bleedM);

        top.position =
            center + painting.up * (halfHeightWorld + bleedM);
    }

    // ----------------------------------------------------
    // SCALE (Y only, world correct, no rotation changes)
    // ----------------------------------------------------
    private void SetScale()
    {
        Bounds local = paintingRenderer.localBounds;

        float heightWorld =
            local.size.y * painting.lossyScale.y + (bleedM * 2f) + frameWidthM;

        float widthWorld =
            local.size.z * painting.lossyScale.z + (bleedM * 2f);

        SetWorldYScale(left, heightWorld);
        SetWorldYScale(right, heightWorld);

        SetWorldYScale(top, widthWorld);
        SetWorldYScale(bottom, widthWorld);

        SetWorldXZ(left);
        SetWorldXZ(right);
        SetWorldXZ(top);
        SetWorldXZ(bottom);
    }

    // ----------------------------------------------------
    // WORLD SCALE HELPERS
    // ----------------------------------------------------
    private void SetWorldYScale(Transform t, float worldY)
    {
        Transform parent = t.parent;
        t.parent = null;

        Vector3 s = t.localScale;
        s.y = worldY;
        t.localScale = s;

        t.parent = parent;
    }

    private void SetWorldXZ(Transform t)
    {
        Transform parent = t.parent;
        t.parent = null;

        Vector3 s = t.localScale;
        s.x = frameWidthM;
        s.z = frameDepthM;
        t.localScale = s;

        t.parent = parent;
    }

#if UNITY_EDITOR
    [ContextMenu("Auto Assign Frame Transforms")]
    private void AutoAssignFrameTransforms()
    {
        left = right = top = bottom = null;

        Transform frames = transform.Find("Frames");
        if (!frames)
        {
            Debug.LogWarning("AutoFrameSetting: 'Frames' object not found.", this);
            return;
        }

        foreach (Transform child in frames)
        {
            switch (child.name)
            {
                case "Left": left = child; break;
                case "Right": right = child; break;
                case "Top": top = child; break;
                case "Bottom": bottom = child; break;
            }
        }

        if (!left) Debug.LogWarning("Left frame missing", this);
        if (!right) Debug.LogWarning("Right frame missing", this);
        if (!top) Debug.LogWarning("Top frame missing", this);
        if (!bottom) Debug.LogWarning("Bottom frame missing", this);

        UnityEditor.EditorUtility.SetDirty(this);
        SetFrame();
    }
#endif
}
