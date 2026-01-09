using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor tool for easily placing artworks in the scene.
/// Access via Tools > Art Gallery > Place Artwork
/// </summary>
public class ArtworkPlacementTool : EditorWindow
{
    private ArtworkData selectedArtwork;
    private Vector3 placementPosition = Vector3.zero;
    private Vector3 placementRotation = Vector3.zero;
    private GameObject artworkFramePrefab;
    private bool snapToWall = true;
    private float snapDistance = 0.5f;
    private int wallLayerIndex = 6; // Layer index (0-31), will be converted to LayerMask

    // Optional InchWall-based placement
    private bool useInchWallCenter = false;
    private InchWallGridData inchWallGridData;
    
    [MenuItem("Tools/Art Gallery/Place Artwork")]
    public static void ShowWindow()
    {
        GetWindow<ArtworkPlacementTool>("Artwork Placement Tool");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Artwork Placement Tool", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // Artwork selection
        selectedArtwork = (ArtworkData)EditorGUILayout.ObjectField(
            "Artwork Data", 
            selectedArtwork, 
            typeof(ArtworkData), 
            false
        );
        
        EditorGUILayout.Space();
        
        // Prefab selection
        artworkFramePrefab = (GameObject)EditorGUILayout.ObjectField(
            "Artwork Frame Prefab", 
            artworkFramePrefab, 
            typeof(GameObject), 
            false
        );
        
        EditorGUILayout.Space();
        
        // Placement settings
        EditorGUILayout.LabelField("Placement Settings", EditorStyles.boldLabel);
        snapToWall = EditorGUILayout.Toggle("Snap to Wall", snapToWall);
        snapDistance = EditorGUILayout.FloatField("Snap Distance", snapDistance);
        wallLayerIndex = EditorGUILayout.LayerField("Wall Layer", wallLayerIndex);
        
        EditorGUILayout.Space();

        // Optional InchWall placement
        EditorGUILayout.LabelField("Inch Wall Placement (Optional)", EditorStyles.boldLabel);
        useInchWallCenter = EditorGUILayout.Toggle("Use InchWall Center", useInchWallCenter);
        inchWallGridData = (InchWallGridData)EditorGUILayout.ObjectField(
            "InchWall Grid Data",
            inchWallGridData,
            typeof(InchWallGridData),
            true
        );
        
        EditorGUILayout.Space();
        
        // Position and rotation
        placementPosition = EditorGUILayout.Vector3Field("Position", placementPosition);
        placementRotation = EditorGUILayout.Vector3Field("Rotation", placementRotation);
        
        EditorGUILayout.Space();
        
        // Buttons
        EditorGUI.BeginDisabledGroup(selectedArtwork == null);
        
        if (GUILayout.Button("Place Artwork at Position"))
        {
            PlaceArtwork();
        }
        
        if (GUILayout.Button("Place Artwork at Scene View Cursor"))
        {
            PlaceArtworkAtSceneView();
        }

        if (useInchWallCenter && GUILayout.Button("Place Artwork on InchWall Center"))
        {
            PlaceArtworkOnInchWallCenter();
        }

        if (useInchWallCenter && GUILayout.Button("Place Second Artwork Side-by-Side on InchWall"))
        {
            PlaceSecondArtworkOnInchWallSideBySide();
        }
        
        if (GUILayout.Button("Place All Artworks from Manager"))
        {
            PlaceAllArtworks();
        }
        
        EditorGUILayout.Space();
        
        // Custom transforms placement
        EditorGUILayout.LabelField("Custom Transform Placement", EditorStyles.boldLabel);
        ArtworkManager manager = FindObjectOfType<ArtworkManager>();
        if (manager != null)
        {
            if (GUILayout.Button("Place Artworks at Custom Transforms"))
            {
                manager.PlaceArtworksAtTransforms();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No ArtworkManager found in scene.", MessageType.Warning);
        }
        
        EditorGUI.EndDisabledGroup();
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Tip: Use Scene View to position your cursor, then click 'Place Artwork at Scene View Cursor'",
            MessageType.Info
        );
    }
    
    private void PlaceArtwork()
    {
        if (selectedArtwork == null) return;
        
        GameObject frameObject;
        if (artworkFramePrefab != null)
        {
            frameObject = PrefabUtility.InstantiatePrefab(artworkFramePrefab) as GameObject;
        }
        else
        {
            frameObject = new GameObject($"ArtworkFrame_{selectedArtwork.title}");
            frameObject.AddComponent<ArtworkFrame>();
        }
        
        frameObject.transform.position = placementPosition;
        frameObject.transform.rotation = Quaternion.Euler(placementRotation);
        
        ArtworkFrame frame = frameObject.GetComponent<ArtworkFrame>();
        if (frame != null)
        {
            frame.SetArtwork(selectedArtwork);
        }
        
        // Snap to wall if enabled
        if (snapToWall)
        {
            SnapToWall(frameObject.transform);
        }
        
        Selection.activeGameObject = frameObject;
        Undo.RegisterCreatedObjectUndo(frameObject, "Place Artwork");
    }
    
    private void PlaceArtworkAtSceneView()
    {
        // Get scene view camera
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView == null) return;
        
        // Convert layer index to LayerMask
        LayerMask wallLayer = 1 << wallLayerIndex;
        
        // Raycast from scene view camera
        Ray ray = sceneView.camera.ScreenPointToRay(new Vector3(
            sceneView.camera.pixelWidth / 2f,
            sceneView.camera.pixelHeight / 2f,
            0f
        ));
        
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100f, wallLayer))
        {
            placementPosition = hit.point;
            placementRotation = Quaternion.LookRotation(-hit.normal).eulerAngles;
        }
        else
        {
            // Place at camera forward position
            placementPosition = sceneView.camera.transform.position + sceneView.camera.transform.forward * 5f;
            placementRotation = (Quaternion.LookRotation(-sceneView.camera.transform.forward)).eulerAngles;
        }
        
        PlaceArtwork();
    }
    
    private void PlaceAllArtworks()
    {
        ArtworkManager manager = FindObjectOfType<ArtworkManager>();
        if (manager == null)
        {
            EditorUtility.DisplayDialog("Error", "No ArtworkManager found in scene!", "OK");
            return;
        }
        
        List<ArtworkData> artworks = manager.GetAllArtworks();
        if (artworks.Count == 0)
        {
            EditorUtility.DisplayDialog("Info", "No artworks in ArtworkManager database!", "OK");
            return;
        }
        
        // Auto-place on walls
        manager.AutoPlaceArtworksOnWalls();
    }
    
    private void PlaceArtworkOnInchWallCenter()
    {
        if (selectedArtwork == null)
        {
            Debug.LogWarning("Artwork Placement Tool: No artwork selected.");
            return;
        }

        if (inchWallGridData == null)
        {
            Debug.LogWarning("Artwork Placement Tool: InchWall Grid Data is not assigned.");
            return;
        }

        ArtworkManager manager = FindObjectOfType<ArtworkManager>();
        if (manager == null)
        {
            Debug.LogWarning("Artwork Placement Tool: No ArtworkManager found in scene.");
            return;
        }

        ArtworkFrame frame = manager.PlaceArtworkOnInchWallCenter(selectedArtwork, inchWallGridData);
        if (frame != null)
        {
            Selection.activeGameObject = frame.gameObject;
            Undo.RegisterCreatedObjectUndo(frame.gameObject, "Place Artwork on InchWall Center");
        }
    }

    private void PlaceSecondArtworkOnInchWallSideBySide()
    {
        if (selectedArtwork == null)
        {
            Debug.LogWarning("Artwork Placement Tool: No artwork selected for second placement.");
            return;
        }

        if (inchWallGridData == null)
        {
            Debug.LogWarning("Artwork Placement Tool: InchWall Grid Data is not assigned.");
            return;
        }

        ArtworkManager manager = FindObjectOfType<ArtworkManager>();
        if (manager == null)
        {
            Debug.LogWarning("Artwork Placement Tool: No ArtworkManager found in scene.");
            return;
        }

        bool success = manager.TryPlaceSecondArtworkOnInchWallSideBySide(selectedArtwork, inchWallGridData);
        if (!success)
        {
            // Error is already logged by ArtworkManager; we just stop here.
            return;
        }

        // If successful, select the grid root (or leave selection as-is).
        Selection.activeGameObject = inchWallGridData.gameObject;
    }
    
    private void SnapToWall(Transform frameTransform)
    {
        // Convert layer index to LayerMask
        LayerMask wallLayer = 1 << wallLayerIndex;
        
        // Use raycasting to find the wall the artwork is facing
        Vector3 rayOrigin = frameTransform.position;
        Vector3 rayDirection = -frameTransform.forward; // Raycast in the direction the artwork is facing
        
        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, rayDirection, out hit, snapDistance * 2f, wallLayer))
        {
            // Found a wall - snap to it
            float offsetFromWall = 0.1f;
            frameTransform.position = hit.point + hit.normal * offsetFromWall;
            frameTransform.rotation = Quaternion.LookRotation(-hit.normal);
        }
        else
        {
            // Try reverse direction
            if (Physics.Raycast(rayOrigin, -rayDirection, out hit, snapDistance * 2f, wallLayer))
            {
                float offsetFromWall = 0.1f;
                frameTransform.position = hit.point + hit.normal * offsetFromWall;
                frameTransform.rotation = Quaternion.LookRotation(-hit.normal);
            }
            else
            {
                // Fallback: use overlap sphere method
                Collider[] nearbyColliders = Physics.OverlapSphere(
                    frameTransform.position, 
                    snapDistance, 
                    wallLayer
                );
                
                if (nearbyColliders.Length > 0)
                {
                    // Find closest wall
                    Collider closestWall = null;
                    float closestDistance = float.MaxValue;
                    
                    foreach (Collider col in nearbyColliders)
                    {
                        float distance = Vector3.Distance(frameTransform.position, col.ClosestPoint(frameTransform.position));
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            closestWall = col;
                        }
                    }
                    
                    if (closestWall != null)
                    {
                        // Snap to wall surface
                        Vector3 wallPoint = closestWall.ClosestPoint(frameTransform.position);
                        Vector3 wallNormal = (frameTransform.position - wallPoint).normalized;
                        
                        frameTransform.position = wallPoint + wallNormal * 0.1f; // Offset slightly from wall
                        frameTransform.rotation = Quaternion.LookRotation(-wallNormal);
                    }
                }
            }
        }
    }
}

