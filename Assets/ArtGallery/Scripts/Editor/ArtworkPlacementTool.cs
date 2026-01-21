using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Utilities;

/// <summary>
/// Editor tool for easily placing artworks in the scene.
/// Access via Tools > Art Gallery > Place Artwork
/// </summary>
public class ArtworkPlacementTool : EditorWindow
{
    private Vector2 scrollPosition = Vector2.zero;

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

    // Edge-to-edge distance calculator (for frames of different sizes)
    private ArtworkFrame distanceCurrentFrame;
    private ArtworkFrame distanceNextFrame;
    private float edgeHorizontalGutterInches = 0f;

    // Optional helper for "two small squares beside a big square" layout
    private bool showTripleLayoutHelper = false;
    private float tripleVerticalGutterInches = 3f;
    private float tripleTopBottomMarginInches = 0f;
    
    [MenuItem("Tools/Art Gallery/Place Artwork")]
    public static void ShowWindow()
    {
        GetWindow<ArtworkPlacementTool>("Artwork Placement Tool");
    }
    
    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

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

        DrawEdgeToEdgeDistanceCalculator();
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Tip: Use Scene View to position your cursor, then click 'Place Artwork at Scene View Cursor'",
            MessageType.Info
        );

        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// Simple calculator that takes two ArtworkFrame instances and tells you how far (in inches and meters)
    /// to move the NEXT frame's center in X or Y so it sits exactly edge-to-edge with the CURRENT frame,
    /// even if their sizes are different.
    /// </summary>
    private void DrawEdgeToEdgeDistanceCalculator()
    {
        GUILayout.Label("Edge-to-Edge Distance Calculator", EditorStyles.boldLabel);

        // Try to auto-populate from current selection if fields are empty
        // If you select one ArtworkFrame in the Hierarchy, it becomes the CURRENT frame.
        // If you select two ArtworkFrames, the first becomes CURRENT and the second NEXT.
        if ((distanceCurrentFrame == null || distanceNextFrame == null) && Selection.gameObjects != null)
        {
            ArtworkFrame first = null;
            ArtworkFrame second = null;
            int found = 0;

            foreach (var go in Selection.gameObjects)
            {
                if (go == null) continue;
                var frame = go.GetComponent<ArtworkFrame>();
                if (frame == null) continue;

                if (found == 0)
                {
                    first = frame;
                    found = 1;
                }
                else if (found == 1)
                {
                    second = frame;
                    found = 2;
                    break;
                }
            }

            if (distanceCurrentFrame == null && first != null)
            {
                distanceCurrentFrame = first;
            }
            if (distanceNextFrame == null && second != null)
            {
                distanceNextFrame = second;
            }
        }

        distanceCurrentFrame = (ArtworkFrame)EditorGUILayout.ObjectField(
            "Current Frame",
            distanceCurrentFrame,
            typeof(ArtworkFrame),
            true
        );

        distanceNextFrame = (ArtworkFrame)EditorGUILayout.ObjectField(
            "Next Frame",
            distanceNextFrame,
            typeof(ArtworkFrame),
            true
        );

        EditorGUILayout.HelpBox(
            "You can drag frames from the Hierarchy into these fields, or simply select 1-2 ArtworkFrame objects and reopen/refresh this window to auto-fill.",
            MessageType.None
        );

        if (distanceCurrentFrame == null || distanceNextFrame == null)
        {
            EditorGUILayout.HelpBox(
                "Assign both a CURRENT frame and a NEXT frame to calculate distances.",
                MessageType.Info
            );
            return;
        }

        // Get outer sizes (including frame + bleed) in inches
        Vector2 currentOuterInches = distanceCurrentFrame.GetOuterSizeInches();
        Vector2 nextOuterInches = distanceNextFrame.GetOuterSizeInches();

        // Center-to-center offset for perfect edge-to-edge placement (no gutter)
        float deltaXInches = 0.5f * (currentOuterInches.x + nextOuterInches.x);
        float deltaYInches = 0.5f * (currentOuterInches.y + nextOuterInches.y);

        // Additional horizontal gutter between the two frames (in inches)
        edgeHorizontalGutterInches = EditorGUILayout.FloatField(
            "Horizontal gutter between frames (in)",
            edgeHorizontalGutterInches
        );

        float moveRightWithGutterInches = deltaXInches + Mathf.Max(0f, edgeHorizontalGutterInches);

        // Convert to Unity units (meters)
        float deltaXUnits = deltaXInches.FromInches();
        float deltaYUnits = deltaYInches.FromInches();
        float moveRightWithGutterUnits = moveRightWithGutterInches.FromInches();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Current outer size (in)",
            $"{currentOuterInches.x:F3} in x {currentOuterInches.y:F3} in");
        EditorGUILayout.LabelField("Next outer size (in)",
            $"{nextOuterInches.x:F3} in x {nextOuterInches.y:F3} in");

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Move NEXT frame's center for edge-to-edge:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(
            "Offset in inches (no gutter)",
            $"{deltaXInches:F3} in in X, {deltaYInches:F3} in in Y"
        );
        EditorGUILayout.LabelField(
            "Offset in Unity units (meters, no gutter)",
            $"{deltaXUnits:F3} m in X, {deltaYUnits:F3} m in Y"
        );

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Move NEXT frame to the RIGHT (with gutter):", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(
            "Right-move in inches",
            $"{moveRightWithGutterInches:F3} in in +X"
        );
        EditorGUILayout.LabelField(
            "Right-move in Unity units (meters)",
            $"{moveRightWithGutterUnits:F3} m in +X"
        );

        EditorGUILayout.HelpBox(
            "Base formula (no gutter): 94 = (current outer size + next outer size) / 2. " +
            "Right-move with gutter adds the specified horizontal gutter to 94 in +X. Assumes centered pivots.",
            MessageType.None
        );

        // Optional helper for the specific layout: two smaller squares stacked next to a bigger square
        showTripleLayoutHelper = EditorGUILayout.Foldout(showTripleLayoutHelper, "Two-small + big layout helper (size suggestion)");
        if (showTripleLayoutHelper)
        {
            EditorGUI.indentLevel++;

            // Use the CURRENT frame's outer height as the big square size
            float bigHeightInches = currentOuterInches.y;
            EditorGUILayout.LabelField("Big frame outer height (in)", bigHeightInches.ToString("F3"));

            tripleVerticalGutterInches = EditorGUILayout.FloatField(
                "Vertical gutter between the two small frames (in)",
                tripleVerticalGutterInches
            );

            tripleTopBottomMarginInches = EditorGUILayout.FloatField(
                "Top/Bottom margin above/below small pair (in)",
                tripleTopBottomMarginInches
            );

            // Solve: 2*S + Gv + 2*M = BigHeight  =>  S = (BigHeight - Gv - 2*M) / 2
            float smallSquareSizeInches =
                (bigHeightInches - tripleVerticalGutterInches - 2f * tripleTopBottomMarginInches) / 2f;

            EditorGUILayout.LabelField(
                "Recommended small square size",
                $"{smallSquareSizeInches:F3} in x {smallSquareSizeInches:F3} in"
            );

            float checkTotalHeight = 2f * smallSquareSizeInches + tripleVerticalGutterInches + 2f * tripleTopBottomMarginInches;
            EditorGUILayout.LabelField(
                "Check total vertical span",
                $"{checkTotalHeight:F3} in (should equal big height {bigHeightInches:F3} in)"
            );

            EditorGUILayout.HelpBox(
                "This assumes two equal small squares stacked vertically to the LEFT of the big frame. " +
                "Set gutter = desired gap between the two small frames. " +
                "Set top/bottom margin if you want extra space above and below the small pair inside the big frame's height. " +
                "For your example: big 60in, gutter 3in, margin 2in -> small ≈ 26.5in.",
                MessageType.Info
            );

            EditorGUI.indentLevel--;
        }
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

