using UnityEditor;
using UnityEngine;
using Utilities;

/// <summary>
/// Custom inspector for ArtworkFrame that displays:
/// - Outer size in inches (including frame and bleeding)
/// - Total area in square inches
/// - How far (in inches) the NEXT artwork's center needs to move in X/Y
///   to sit exactly edge-to-edge with this frame.
///
/// This uses ArtworkFrame.GetOuterSizeInches() and GetEdgeToEdgeOffsetInches().
/// </summary>
[CustomEditor(typeof(ArtworkFrame))]
public class ArtworkFrameEditor : Editor
{
    // Editor-only gutter fields (not stored on ArtworkFrame)
    private static float horizontalGutterInches = 0f;
    private static float verticalGutterInches = 0f;
    public override void OnInspectorGUI()
    {
        // Draw all the normal fields first
        base.OnInspectorGUI();

        // Only show the helper info when exactly one frame is selected
        if (targets == null || targets.Length != 1)
        {
            return;
        }

        var frame = (ArtworkFrame)target;

        EditorGUILayout.Space();

        if (GUILayout.Button("Rebuild Only Frame (Adjust Top/Right/Left/Bottom)"))
        {
            Undo.RecordObject(frame, "Rebuild Frame From Artwork");
            frame.RebuildFrameFromArtwork();
            EditorUtility.SetDirty(frame);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Rebuild Whole Artwork", EditorStyles.boldLabel);

        // Show current outer size for reference
        Vector2 currentOuterForRebuild = frame.GetOuterSizeInches();
        EditorGUILayout.LabelField(
            "Current outer size",
            $"{currentOuterForRebuild.x:F3} in x {currentOuterForRebuild.y:F3} in"
        );

        // Optional aspect ratio
        bool useAspect = EditorGUILayout.Toggle("Use aspect ratio (W:H)", frame.EditorUseAspectRatio);
        if (useAspect != frame.EditorUseAspectRatio)
        {
            Undo.RecordObject(frame, "Toggle outer aspect ratio usage");
            frame.EditorUseAspectRatio = useAspect;
            EditorUtility.SetDirty(frame);
        }

        if (frame.EditorUseAspectRatio)
        {
            float aspectW = EditorGUILayout.FloatField("Aspect width (W)", frame.EditorAspectWidth);
            float aspectH = EditorGUILayout.FloatField("Aspect height (H)", frame.EditorAspectHeight);

            if (!Mathf.Approximately(aspectW, frame.EditorAspectWidth) ||
                !Mathf.Approximately(aspectH, frame.EditorAspectHeight))
            {
                Undo.RecordObject(frame, "Change outer aspect ratio");
                frame.EditorAspectWidth = aspectW;
                frame.EditorAspectHeight = aspectH;
                EditorUtility.SetDirty(frame);
            }

            if (GUILayout.Button("Set aspect from current outer size"))
            {
                Undo.RecordObject(frame, "Set outer aspect from current size");
                float w = Mathf.Max(0.0001f, currentOuterForRebuild.x);
                float h = Mathf.Max(0.0001f, currentOuterForRebuild.y);
                frame.EditorAspectWidth = w;
                frame.EditorAspectHeight = h;
                EditorUtility.SetDirty(frame);
            }
        }

        // Capture previous targets to detect which field user changed
        float prevOuterW = frame.EditorTargetOuterWidthInches;
        float prevOuterH = frame.EditorTargetOuterHeightInches;

        float targetOuterW = EditorGUILayout.FloatField(
            "Target outer width (in)",
            frame.EditorTargetOuterWidthInches
        );
        float targetOuterH = EditorGUILayout.FloatField(
            "Target outer height (in)",
            frame.EditorTargetOuterHeightInches
        );

        if (frame.EditorUseAspectRatio)
        {
            float aw = Mathf.Max(0.0001f, frame.EditorAspectWidth);
            float ah = Mathf.Max(0.0001f, frame.EditorAspectHeight);
            float ratioWH = aw / ah; // width / height

            bool widthChanged = !Mathf.Approximately(targetOuterW, prevOuterW);
            bool heightChanged = !Mathf.Approximately(targetOuterH, prevOuterH);

            if (widthChanged && !heightChanged)
            {
                // User edited width -> compute height from ratio
                targetOuterH = targetOuterW / ratioWH;
            }
            else if (heightChanged && !widthChanged)
            {
                // User edited height -> compute width from ratio
                targetOuterW = targetOuterH * ratioWH;
            }
            else if (widthChanged && heightChanged)
            {
                // Both changed: favor width as the source of truth
                targetOuterH = targetOuterW / ratioWH;
            }
        }

        if (!Mathf.Approximately(targetOuterW, frame.EditorTargetOuterWidthInches) ||
            !Mathf.Approximately(targetOuterH, frame.EditorTargetOuterHeightInches))
        {
            Undo.RecordObject(frame, "Change target outer size (frame editor)");
            frame.EditorTargetOuterWidthInches = targetOuterW;
            frame.EditorTargetOuterHeightInches = targetOuterH;
            EditorUtility.SetDirty(frame);
        }

        if (GUILayout.Button("Rebuild Whole Artwork"))
        {
            Undo.RecordObject(frame, "Rebuild Frame To Outer Size");
            frame.RebuildFrameToOuterSize(frame.EditorTargetOuterWidthInches, frame.EditorTargetOuterHeightInches);
            EditorUtility.SetDirty(frame);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Artwork Coverage (Inches)", EditorStyles.boldLabel);

        Vector2 outerSizeInches = frame.GetOuterSizeInches();
        float areaSqInches = outerSizeInches.x * outerSizeInches.y;

        // Convert to Unity units (meters) for precise control using UnitConversionExtensions
        Vector2 outerSizeUnits = new Vector2(
            outerSizeInches.x.FromInches(),
            outerSizeInches.y.FromInches()
        );
        float areaSqUnits = outerSizeUnits.x * outerSizeUnits.y;

        EditorGUILayout.LabelField(
            "Outer Size (incl. frame + bleed)",
            $"{outerSizeInches.x:F3} in x {outerSizeInches.y:F3} in"
        );

        EditorGUILayout.LabelField(
            "Outer Size (Unity units / meters)",
            $"{outerSizeUnits.x:F3} m x {outerSizeUnits.y:F3} m"
        );

        EditorGUILayout.LabelField(
            "Area Covered",
            $"{areaSqInches:F3} in^2"
        );

        EditorGUILayout.LabelField(
            "Area Covered (Unity units / m^2)",
            $"{areaSqUnits:F3} m^2"
        );

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Edge-to-Edge Placement Helper", EditorStyles.boldLabel);

        // Offset assuming same outer size as this frame
        Vector2 sameSizeOffset = outerSizeInches;
        Vector2 sameSizeOffsetUnits = new Vector2(
            sameSizeOffset.x.FromInches(),
            sameSizeOffset.y.FromInches()
        );
        EditorGUILayout.LabelField(
            "Next artwork SAME outer size:",
            $"Move center by {sameSizeOffset.x:F3} in in X, or {sameSizeOffset.y:F3} in in Y"
        );
        EditorGUILayout.LabelField(
            "Same-size move in Unity units (meters):",
            $"{sameSizeOffsetUnits.x:F3} m in X, {sameSizeOffsetUnits.y:F3} m in Y"
        );

        // If the user has configured a custom next artwork outer size, show the exact offset
        if (frame.UseCustomNextArtworkSize)
        {
            Vector2 customNextOuter = frame.CustomNextArtworkOuterSizeInches;
            Vector2 customOffset = frame.GetEdgeToEdgeOffsetInches();
            Vector2 customOffsetUnits = new Vector2(
                customOffset.x.FromInches(),
                customOffset.y.FromInches()
            );

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Next artwork CUSTOM outer size:");
            EditorGUILayout.LabelField(
                "Configured next outer size",
                $"{customNextOuter.x:F3} in x {customNextOuter.y:F3} in"
            );

            // Standard center-to-center offsets (edge to edge, no gutter)
            EditorGUILayout.LabelField(
                "Required move for its center (no gutter)",
                $"{customOffset.x:F3} in in X, {customOffset.y:F3} in in Y"
            );
            EditorGUILayout.LabelField(
                "Required move in Unity units (no gutter)",
                $"{customOffsetUnits.x:F3} m in X, {customOffsetUnits.y:F3} m in Y"
            );

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Alternative: Edge-matching (half diffs)", EditorStyles.miniBoldLabel);

            // Half-diff formula for matching edges when centers are aligned:
            //   To match LEFT edges:  move next in X by (currentW/2 - nextW/2)
            //   To match TOP edges:   move next in Y by (currentH/2 - nextH/2)
            float halfCurrentW = outerSizeUnits.x * 0.5f;
            float halfCurrentH = outerSizeUnits.y * 0.5f;
            float halfNextW = customNextOuter.x.FromInches() * 0.5f;
            float halfNextH = customNextOuter.y.FromInches() * 0.5f;

            float edgeMatchHorizontalUnits = halfCurrentW - halfNextW; // move next in X by this amount
            float edgeMatchVerticalUnits = halfCurrentH - halfNextH;   // move next in Y by this amount

            EditorGUILayout.LabelField(
                "Horizontal edge match (same center, match LEFT edges)",
                $"{edgeMatchHorizontalUnits:F3} m (ΔX)"
            );
            EditorGUILayout.LabelField(
                "Vertical edge match (same center, match TOP edges)",
                $"{edgeMatchVerticalUnits:F3} m (ΔY)"
            );

            // Edge-to-edge side placement (Right/Up) from this frame to the next
            float edgeToEdgeRightUnits = halfCurrentW + halfNextW; // centers offset for RIGHT placement (no gutter)
            float edgeToEdgeUpUnits = halfCurrentH + halfNextH;    // centers offset for UP placement (no gutter)

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Side placement (no gutter)", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField(
                "Move NEXT to RIGHT (edge-to-edge)",
                $"{edgeToEdgeRightUnits:F3} m in +X"
            );
            EditorGUILayout.LabelField(
                "Move NEXT UP (edge-to-edge)",
                $"{edgeToEdgeUpUnits:F3} m in +Y"
            );

            // Optional gutter fields for horizontal (Right) and vertical (Up) movements
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("With Gutter", EditorStyles.miniBoldLabel);

            horizontalGutterInches = EditorGUILayout.FloatField(
                "Horizontal gutter (in)",
                horizontalGutterInches
            );
            verticalGutterInches = EditorGUILayout.FloatField(
                "Vertical gutter (in)",
                verticalGutterInches
            );

            float gutterUnitsH = Mathf.Max(0f, horizontalGutterInches).FromInches();
            float gutterUnitsV = Mathf.Max(0f, verticalGutterInches).FromInches();

            float moveRightWithGutterUnits = edgeToEdgeRightUnits + gutterUnitsH;
            float moveUpWithGutterUnits = edgeToEdgeUpUnits + gutterUnitsV;

            EditorGUILayout.LabelField(
                "Move NEXT to RIGHT (with gutter)",
                $"{moveRightWithGutterUnits:F3} m in +X"
            );
            EditorGUILayout.LabelField(
                "Move NEXT UP (with gutter)",
                $"{moveUpWithGutterUnits:F3} m in +Y"
            );
        }

        EditorGUILayout.HelpBox(
            "Values above assume both this frame and the next one use centered pivots. " +
            "Move the NEXT artwork's center by the shown meters in +X (Right) or +Y (Up) to place it exactly edge-to-edge.",
            MessageType.Info
        );
    }
}
