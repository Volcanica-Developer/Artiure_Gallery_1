using UnityEngine;

public class ChildSpawner : MonoBehaviour
{
    [Min(1)]
    public int count = 3;

    [Tooltip("Optional prefab to spawn. If null, empty GameObjects will be created.")]
    public GameObject childPrefab;

    [Tooltip("Clear existing children before spawning new ones.")]
    public bool clearExistingChildren = true;

    /// <summary>
    /// Creates child objects spaced evenly along this object's length in local X.
    /// Prefers the Mesh bounds (local space) for accuracy, falls back to Renderer bounds.
    /// </summary>
    public void CreateChildren()
    {
        if (count <= 0)
        {
            Debug.LogWarning("ChildSpawner: count must be > 0.");
            return;
        }

        var renderer = GetComponent<Renderer>();
        if (renderer == null)
        {
            Debug.LogError("ChildSpawner: requires a Renderer on the same GameObject to measure length.");
            return;
        }

        // Clear existing children if requested
        if (clearExistingChildren)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(child.gameObject);
                else
                    Destroy(child.gameObject);
#else
                Destroy(child.gameObject);
#endif
            }
        }

        // Try to use Mesh bounds in LOCAL space (handles rotation better)
        float minXLocal, maxXLocal;
        float centerYLocal, centerZLocal;

        var meshFilter = GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            var meshBounds = meshFilter.sharedMesh.bounds; // local space bounds
            minXLocal = meshBounds.min.x;
            maxXLocal = meshBounds.max.x;
            centerYLocal = meshBounds.center.y;
            centerZLocal = meshBounds.center.z;
        }
        else
        {
            // Fallback: use renderer's world-space bounds and convert to local.
            // This may be slightly less accurate for rotated objects but is still usable.
            var worldBounds = renderer.bounds;
            Vector3 minWorld = worldBounds.min;
            Vector3 maxWorld = worldBounds.max;

            Vector3 minLocalV3 = transform.InverseTransformPoint(minWorld);
            Vector3 maxLocalV3 = transform.InverseTransformPoint(maxWorld);

            minXLocal = Mathf.Min(minLocalV3.x, maxLocalV3.x);
            maxXLocal = Mathf.Max(minLocalV3.x, maxLocalV3.x);

            // Use the renderer center converted to local for Y/Z so children stay centered
            Vector3 centerLocalV3 = transform.InverseTransformPoint(worldBounds.center);
            centerYLocal = centerLocalV3.y;
            centerZLocal = centerLocalV3.z;
        }

        float lengthX = maxXLocal - minXLocal;
        if (lengthX <= 0f)
        {
            Debug.LogWarning("ChildSpawner: computed length is zero or negative; cannot place children.");
            return;
        }

        float segmentLength = lengthX / count;

        for (int i = 0; i < count; i++)
        {
            // Position at center of each segment along local X:
            //   x = leftEdge + segmentLength * (i + 0.5)
            float localX = minXLocal + segmentLength * (i + 0.5f);
            Vector3 localPos = new Vector3(localX, centerYLocal, centerZLocal);

            GameObject child;
            if (childPrefab != null)
            {
                child = Instantiate(childPrefab, transform);
                child.name = childPrefab.name + "_" + i;
            }
            else
            {
                child = new GameObject("Child_" + i);
                child.transform.SetParent(transform, false);
            }

            child.transform.localPosition = localPos;
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = Vector3.one;
        }
    }
}
