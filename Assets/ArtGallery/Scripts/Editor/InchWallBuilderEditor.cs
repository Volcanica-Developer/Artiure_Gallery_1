using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom inspector for InchWallBuilder to provide Build/Clear buttons like GalleryBuilder.
/// </summary>
[CustomEditor(typeof(InchWallBuilder))]
public class InchWallBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        InchWallBuilder builder = (InchWallBuilder)target;

        if (GUILayout.Button("Build Inch Wall"))
        {
            builder.BuildWall();
        }

        if (GUILayout.Button("Clear Inch Wall"))
        {
            builder.ClearWall();
        }
    }
}
