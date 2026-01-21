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
            EditorGUILayout.LabelField(
                "Required move for its center",
                $"{customOffset.x:F3} in in X, {customOffset.y:F3} in in Y"
            );
            EditorGUILayout.LabelField(
                "Required move in Unity units (meters)",
                $"{customOffsetUnits.x:F3} m in X, {customOffsetUnits.y:F3} m in Y"
            );
        }

        EditorGUILayout.HelpBox(
            "Values above assume both this frame and the next one use centered pivots. " +
            "Move the NEXT artwork's center by the shown inches in X or Y to place it exactly edge-to-edge.",
            MessageType.Info
        );
    }
}
