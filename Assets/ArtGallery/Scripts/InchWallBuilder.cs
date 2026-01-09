using UnityEngine;
using Utilities;

/// <summary>
/// Builds a wall as a cube with thickness in Unity units, but width/height specified in inches.
/// Also creates a visible 1x1 inch grid (mesh) on the front face of the wall.
/// </summary>
public class InchWallBuilder : MonoBehaviour
{
    [Header("Wall Size (inches)")]
    [Tooltip("Total width of the wall in inches.")]
    [SerializeField] private float widthInches = 96f;

    [Tooltip("Total height of the wall in inches.")]
    [SerializeField] private float heightInches = 96f;

    [Header("Wall Settings (Unity units)")]
    [Tooltip("Wall thickness in Unity units (meters). Default is 1.")]
    [SerializeField] private float thickness = 1f;

    [SerializeField] private Material wallMaterial;
    [SerializeField] private Material gridMaterial;

    [Header("Grid Points Data")]
    [Tooltip("If true, computes and stores grid cell center positions in InchWallGridData.")]
    [SerializeField] private bool createGridPoints = true;

    [Tooltip("Name of the generated wall root object.")]
    [SerializeField] private string wallRootName = "Inch Wall";

    private GameObject wallRoot;

    /// <summary>
    /// Builds the wall cube and 1x1 inch grid mesh on its front face.
    /// </summary>
    [ContextMenu("Build Inch Wall")]
    public void BuildWall()
    {
        ClearWall();

        wallRoot = new GameObject(string.IsNullOrEmpty(wallRootName) ? "Inch Wall" : wallRootName);
        wallRoot.transform.SetParent(transform, false);

        // Convert dimensions from inches to Unity units (meters)
        float widthUnits = Mathf.Max(0.01f, widthInches.FromInches());
        float heightUnits = Mathf.Max(0.01f, heightInches.FromInches());
        float depthUnits = Mathf.Max(0.01f, thickness);

        // Attach data container for grid positions to the wall root
        InchWallGridData gridData = wallRoot.AddComponent<InchWallGridData>();

        // Position the wall so that its bottom sits on the builder's origin (surface)
        // Instead of being centered half below and half above.
        wallRoot.transform.localPosition = new Vector3(0f, heightUnits / 2f, 0f);

        // Create the main wall body as a cube
        GameObject wallBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wallBody.name = "Wall Body";
        wallBody.transform.SetParent(wallRoot.transform, false);
        wallBody.transform.localScale = new Vector3(widthUnits, heightUnits, depthUnits);

        Renderer wallRenderer = wallBody.GetComponent<Renderer>();
        if (wallRenderer != null && wallMaterial != null)
        {
            wallRenderer.sharedMaterial = wallMaterial;
        }

        // Optional: keep collider on the body for interactions

        // Create the front grid mesh, positioned slightly in front of the wall's front face
        // We treat the "front" as the -Z face (towards the camera in your setup).
        GameObject gridObject = new GameObject("Inch Grid");
        gridObject.transform.SetParent(wallRoot.transform, false);
        gridObject.transform.localPosition = new Vector3(0f, 0f, -depthUnits / 2f - 0.001f);

        MeshFilter meshFilter = gridObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = gridObject.AddComponent<MeshRenderer>();

        if (gridMaterial != null)
        {
            meshRenderer.sharedMaterial = gridMaterial;
        }

        meshFilter.sharedMesh = BuildGridMesh(widthInches, heightInches);

        // Optionally compute grid cell center positions on the wall surface and store them as data
        if (createGridPoints)
        {
            // Reuse the grid data component already attached to the wall root
            InchWallGridData wallGridData = wallRoot.GetComponent<InchWallGridData>();
            if (wallGridData != null)
            {
                CreateGridPoints(widthInches, heightInches, depthUnits, wallGridData);
            }
        }
    }

    /// <summary>
    /// Configures parametric grid data on the wall so positions can be computed on demand
    /// without allocating large arrays. For width/height of 10x10 inches you will get
    /// a 10x10 logical grid of cells.
    /// </summary>
    private void CreateGridPoints(float widthInches, float heightInches, float depthUnits, InchWallGridData gridData)
    {
        int segmentsX = Mathf.Max(1, Mathf.RoundToInt(widthInches));
        int segmentsY = Mathf.Max(1, Mathf.RoundToInt(heightInches));

        float widthUnits = Mathf.Max(0.01f, widthInches.FromInches());
        float heightUnits = Mathf.Max(0.01f, heightInches.FromInches());

        // Z offset for the grid cell centers on the wall's front surface (same as grid lines)
        // Front is the -Z face.
        float z = -depthUnits / 2f - 0.0015f;

        // Configure parametric grid: we don't store each position, only dimensions and size
        gridData.Configure(segmentsX, segmentsY, widthUnits, heightUnits, z, flipX: true);
    }

    /// <summary>
    /// Builds a line grid mesh where each cell is approximately 1x1 inch in size.
    /// The grid covers the full wall: if width/height are 10 x 10 inches,
    /// you will get an 10 x 10 cell grid (11 vertical + 11 horizontal lines).
    /// </summary>
    private Mesh BuildGridMesh(float widthInches, float heightInches)
    {
        // Number of 1-inch cells; at least 1 in each direction
        int segmentsX = Mathf.Max(1, Mathf.RoundToInt(widthInches));
        int segmentsY = Mathf.Max(1, Mathf.RoundToInt(heightInches));

        float widthUnits = Mathf.Max(0.01f, widthInches.FromInches());
        float heightUnits = Mathf.Max(0.01f, heightInches.FromInches());

        float stepX = widthUnits / segmentsX;
        float stepY = heightUnits / segmentsY;

        // Center the grid around (0,0) in local space so it matches the cube front face
        float halfWidth = widthUnits * 0.5f;
        float halfHeight = heightUnits * 0.5f;

        // We will build a pure line mesh (MeshTopology.Lines)
        // Vertical lines: segmentsX + 1
        // Horizontal lines: segmentsY + 1
        int verticalLineCount = segmentsX + 1;
        int horizontalLineCount = segmentsY + 1;
        int totalLineCount = verticalLineCount + horizontalLineCount;

        // Two vertices per line
        Vector3[] vertices = new Vector3[totalLineCount * 2];
        int[] indices = new int[vertices.Length];
        Vector2[] uvs = new Vector2[vertices.Length];

        int v = 0;

        // Vertical lines
        for (int x = 0; x <= segmentsX; x++)
        {
            float px = -halfWidth + x * stepX;

            vertices[v] = new Vector3(px, -halfHeight, 0f);
            uvs[v] = new Vector2((float)x / segmentsX, 0f);
            indices[v] = v;
            v++;

            vertices[v] = new Vector3(px, halfHeight, 0f);
            uvs[v] = new Vector2((float)x / segmentsX, 1f);
            indices[v] = v;
            v++;
        }

        // Horizontal lines
        for (int y = 0; y <= segmentsY; y++)
        {
            float py = -halfHeight + y * stepY;

            vertices[v] = new Vector3(-halfWidth, py, 0f);
            uvs[v] = new Vector2(0f, (float)y / segmentsY);
            indices[v] = v;
            v++;

            vertices[v] = new Vector3(halfWidth, py, 0f);
            uvs[v] = new Vector2(1f, (float)y / segmentsY);
            indices[v] = v;
            v++;
        }

        Mesh mesh = new Mesh();
        mesh.name = "InchGridMesh";
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.SetIndices(indices, MeshTopology.Lines, 0);
        mesh.RecalculateBounds();

        return mesh;
    }

    /// <summary>
    /// Clears the previously built wall.
    /// </summary>
    [ContextMenu("Clear Inch Wall")]
    public void ClearWall()
    {
        if (wallRoot != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(wallRoot);
            }
            else
#endif
            {
                Destroy(wallRoot);
            }

            wallRoot = null;
        }
    }
}
