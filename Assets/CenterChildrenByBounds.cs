using UnityEngine;

[ExecuteInEditMode]   // so it works in the editor without Play mode
public class CenterChildrenByBounds : MonoBehaviour
{
    [ContextMenu("Center Children Around Parent")]
    public void CenterChildrenAroundParent()
    {
        // Collect all renderers in this hierarchy (including this object)
        var renderers = GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            Debug.LogWarning("CenterChildrenByBounds: No Renderers found under this object.", this);
            return;
        }

        // Build a combined bounds in WORLD space
        Bounds combined = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            combined.Encapsulate(renderers[i].bounds);
        }

        // How much to move children so the visual center sits at the parent position
        Vector3 offset = transform.position - combined.center;

        // Move only direct children; descendants follow via hierarchy
        foreach (Transform child in transform)
        {
            child.position += offset;
        }

        Debug.Log("CenterChildrenByBounds: Children recentered around parent.", this);
    }
}
